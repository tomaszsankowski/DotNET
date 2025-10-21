using Autofac;
using Autofac.Configuration;
using Autofac.Features.Metadata;
using Microsoft.Extensions.Configuration;

public class Program
{
    static void Main()
    {
        var container = BuildContainer(false);

        var w1 = container.Resolve<Worker>();
        Console.WriteLine(w1.Work("123", "123"));

        var w2 = container.Resolve<Worker2>();
        Console.WriteLine(w2.Work("123", "123"));

        var w3 = container.Resolve<Worker3>();
        Console.WriteLine(w3.Work("123", "123"));

        var w4 = container.ResolveNamed<Worker>("state");
        Console.WriteLine(w4.Work("123", "123"));

        var w5 = container.ResolveNamed<Worker2>("state");
        Console.WriteLine(w5.Work("123", "123"));

        var w6 = container.ResolveNamed<Worker3>("state");
        Console.WriteLine(w6.Work("123", "123"));

        IUnitOfWork uowFromScope1;
        IUnitOfWork uowFromScope2;

        // Tworzymy pierwszy, odizolowany zasięg
        using (var scope1 = container.BeginLifetimeScope())
        {
            uowFromScope1 = scope1.ResolveNamed<IUnitOfWork>("scoped");
            var uowAnotherFromScope1 = scope1.ResolveNamed<IUnitOfWork>("scoped");
            Console.WriteLine($"\nCzy instancje wewnątrz scope1 są takie same? " +
                              $"{uowFromScope1.Id == uowAnotherFromScope1.Id} " +
                              $"(ID: {uowFromScope1.Id})");
        }

        // Tworzymy drugi, zupełnie nowy zasięg
        using (var scope2 = container.BeginLifetimeScope())
        {
            uowFromScope2 = scope2.ResolveNamed<IUnitOfWork>("scoped");
            Console.WriteLine($" -> ID instancji wewnątrz scope2: {uowFromScope2.Id}");
        }
        Console.WriteLine($" -> Czy instancja ze scope1 jest taka sama jak ze scope2?" +
                          $" {uowFromScope1.Id == uowFromScope2.Id}");

        var processor = container.Resolve<TransactionProcessor>();
        Console.WriteLine("\nUruchamianie pierwszej transakcji...");
        processor.ProcessTransaction();
        Console.WriteLine("\nUruchamianie drugiej, niezależnej transakcji...");
        processor.ProcessTransaction();
    }

    public static IContainer BuildContainer(bool declarative)
    {
        var builder = new ContainerBuilder();

        if (declarative)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            builder.RegisterModule(new ConfigurationModule(config.GetSection("autofac")));

            builder.RegisterType<Worker>();

            builder.RegisterType<Worker2>()
                .OnActivated(e =>
                {
                    var calculators = e.Context.Resolve<IEnumerable<Meta<ICalculator>>>();
                    var plusCalc = calculators
                        .Where(c => c.Metadata.ContainsKey("name") && (string)c.Metadata["name"] == "plus_calc")
                        .Select(c => c.Value)
                        .FirstOrDefault();
                    e.Instance.SetCalculator(plusCalc);
                });

            builder.RegisterType<Worker3>()
                .OnActivated(e => e.Instance.m_calc = e.Context.Resolve<ICalculator>());

            builder.Register(c =>
            {
                var calculators = c.Resolve<IEnumerable<Meta<ICalculator>>>();
                var stateCalc = calculators
                    .Where(c => c.Metadata.ContainsKey("name") && (string)c.Metadata["name"] == "state_calc")
                    .Select(c => c.Value)
                    .FirstOrDefault();
                return new Worker(stateCalc);
            }).Named<Worker>("state");

            builder.RegisterType<Worker2>()
                .Named<Worker2>("state")
                .OnActivated(e =>
                {
                    var calculators = e.Context.Resolve<IEnumerable<Meta<ICalculator>>>();
                    var stateCalc = calculators
                        .Where(c => c.Metadata.ContainsKey("name") && (string)c.Metadata["name"] == "state_calc")
                        .Select(c => c.Value)
                        .FirstOrDefault();
                    e.Instance.SetCalculator(stateCalc);
                });

            builder.RegisterType<Worker3>()
                .Named<Worker3>("state")
                .OnActivated(e =>
                {
                    var calculators = e.Context.Resolve<IEnumerable<Meta<ICalculator>>>();
                    var stateCalc = calculators
                        .Where(c => c.Metadata.ContainsKey("name") && (string)c.Metadata["name"] == "state_calc")
                        .Select(c => c.Value)
                        .FirstOrDefault();
                    e.Instance.m_calc = stateCalc;
                });
        }
        else
        {
            builder.RegisterType<CatCalc>().As<ICalculator>();
            builder.RegisterType<PlusCalc>().Named<ICalculator>("plus_calc");
            builder.RegisterType<StateCalc>().Named<ICalculator>("state_calc")
                .WithParameter("i", 17)
                .SingleInstance();

            builder.RegisterType<Worker>();

            builder.RegisterType<Worker2>()
                .OnActivated(e => e.Instance.SetCalculator(e.Context.ResolveNamed<ICalculator>("plus_calc")));

            builder.RegisterType<Worker3>()
                .OnActivated(e => e.Instance.m_calc = e.Context.Resolve<ICalculator>());

            builder.RegisterType<Worker>()
                .Named<Worker>("state")
                .WithParameter(
                    (pi, _) => pi.ParameterType == typeof(ICalculator),
                    (_, ctx) => ctx.ResolveNamed<ICalculator>("state_calc")
                );

            builder.RegisterType<Worker2>()
                .Named<Worker2>("state")
                .OnActivated(e => e.Instance.SetCalculator(e.Context.ResolveNamed<ICalculator>("state_calc")));

            builder.RegisterType<Worker3>()
                .Named<Worker3>("state")
                .OnActivated(e => e.Instance.m_calc = e.Context.ResolveNamed<ICalculator>("state_calc"));
        }

        builder.RegisterType<UnitOfWork>().Named<IUnitOfWork>("scoped")
            .InstancePerLifetimeScope();

        builder.RegisterType<TransactionContext>().As<ITransactionContext>()
            .InstancePerMatchingLifetimeScope("transaction");

        builder.RegisterType<StepOneService>();
        builder.RegisterType<StepTwoService>();
        builder.RegisterType<TransactionProcessor>();

        return builder.Build();
    }
}


public interface ICalculator
{
    string Eval(string a, string b);
}

public class Worker // wstrzykiwanie przez konstruktor
{
    public Worker(ICalculator calc) { m_calc = calc; }
    public string Work(string a, string b)
    {
        return m_calc.Eval(a, b);
    }
    private ICalculator m_calc;
}

public class CatCalc : ICalculator
{
    public string Eval(string a, string b)
    {
        return $"{a}{b}";
    }
}

public class PlusCalc : ICalculator
{
    public string Eval(string a, string b)
    {
        return (int.Parse(a) + int.Parse(b)).ToString();
    }
}

public class StateCalc : ICalculator
{
    public StateCalc(int i) { this.i = i; }
    public string Eval(string a, string b)
    {
        return $"{a}{b}{i++}";
    }

    private int i;
}

public class Worker2 // wstrzykiwanie przez setter
{
    public string Work(string a, string b)
    {
        return m_calc.Eval(a, b);
    }

    public void SetCalculator(ICalculator calc) { m_calc = calc; }
    private ICalculator m_calc;
}

public class Worker3 // wstrzykiwanie przez publiczne pole
{
    public string Work(string a, string b)
    {
        return m_calc.Eval(a, b);
    }

    public ICalculator m_calc;
}

public interface IUnitOfWork
{
    Guid Id { get; }
}

public class UnitOfWork : IUnitOfWork
{
    public Guid Id { get; } = Guid.NewGuid();
}

// Interfejs reprezentujący współdzielony stan/kontekst
public interface ITransactionContext
{
    Guid TransactionId { get; }
}

// Implementacja kontekstu, każda instancja ma unikalne ID
public class TransactionContext : ITransactionContext
{
    public Guid TransactionId { get; } = Guid.NewGuid();
    public TransactionContext()
    {
        Console.WriteLine($"UTWORZONO NOWY KONTEKST TRANSAKCJI: {TransactionId}");
    }
}

// Pierwsza usługa biorąca udział w transakcji
public class StepOneService
{
    private readonly ITransactionContext _context;
    public StepOneService(ITransactionContext context) => _context = context;
    public void Execute() => Console.WriteLine($"Krok 1: Przetwarzanie w ramach transakcji {_context.TransactionId}");
}

// Druga usługa, która musi współdzielić ten sam kontekst
public class StepTwoService
{
    private readonly ITransactionContext _context;
    public StepTwoService(ITransactionContext context) => _context = context;
    public void Execute() => Console.WriteLine($"Krok 2: Zapisywanie w ramach transakcji {_context.TransactionId}");
}

// Główna klasa orkiestrująca całą operację
public class TransactionProcessor
{
    private readonly ILifetimeScope _scope;
    public TransactionProcessor(ILifetimeScope scope) => _scope = scope;
    public void ProcessTransaction()
    {
        // Tworzymy NOWY, OTAGOWANY zasięg czasu życia tylko dla tej transakcji
        using (var transactionScope = _scope.BeginLifetimeScope("transaction"))
        {
            Console.WriteLine(" -> Rozpoczęto nową transakcję...");
            // Rozwiązujemy zależności Z WNĘTRZA otagowanego zasięgu
            var step1 = transactionScope.Resolve<StepOneService>();
            var step2 = transactionScope.Resolve<StepTwoService>();
            step1.Execute();
            step2.Execute();
            Console.WriteLine(" -> Transakcja zakończona.");
        }
    }
}
