using Autofac;
using Autofac.Features.Metadata;

namespace Tests
{
    [TestClass]
    public class UnitTestAutofac
    {
        private IContainer BuildContainer(bool declarative)
        {
            return Program.BuildContainer(declarative);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker_Default_Uses_CatCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.Resolve<Worker>();
            var result = w.Work("123", "123");
            Assert.AreEqual("123123", result);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker2_Default_Uses_PlusCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.Resolve<Worker2>();
            var result = w.Work("123", "123");
            Assert.AreEqual("246", result);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker3_Default_Uses_CatCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.Resolve<Worker3>();
            var result = w.Work("123", "123");
            Assert.AreEqual("123123", result);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker_State_Uses_StateCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.ResolveNamed<Worker>("state");
            var result1 = w.Work("123", "123");
            var result2 = w.Work("123", "123");
            Assert.AreEqual("12312317", result1);
            Assert.AreEqual("12312318", result2);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker2_State_Uses_StateCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.ResolveNamed<Worker2>("state");
            var result1 = w.Work("123", "123");
            var result2 = w.Work("123", "123");
            Assert.AreEqual("12312317", result1);
            Assert.AreEqual("12312318", result2);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Worker3_State_Uses_StateCalc(bool declarative)
        {
            var container = BuildContainer(declarative);
            var w = container.ResolveNamed<Worker3>("state");
            var result1 = w.Work("123", "123");
            var result2 = w.Work("123", "123");
            Assert.AreEqual("12312317", result1);
            Assert.AreEqual("12312318", result2);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void StateCalc_Is_Singleton(bool declarative)
        {
            var container = BuildContainer(declarative);

            ICalculator calc1;
            ICalculator calc2;

            if (declarative)
            {
                calc1 = container.Resolve<IEnumerable<Meta<ICalculator>>>()
                    .First(m => (string)m.Metadata["name"] == "state_calc").Value;
                calc2 = container.Resolve<IEnumerable<Meta<ICalculator>>>()
                    .First(m => (string)m.Metadata["name"] == "state_calc").Value;
            }
            else
            {
                calc1 = container.ResolveNamed<ICalculator>("state_calc");
                calc2 = container.ResolveNamed<ICalculator>("state_calc");
            }

            Assert.AreSame(calc1, calc2);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitOfWork_Is_PerLifetimeScope(bool declarative)
        {
            var container = BuildContainer(declarative);
            Guid id1, id2;

            using (var scope1 = container.BeginLifetimeScope())
            {
                var u1 = scope1.ResolveNamed<IUnitOfWork>("scoped");
                var u2 = scope1.ResolveNamed<IUnitOfWork>("scoped");
                Assert.AreEqual(u1.Id, u2.Id);
                id1 = u1.Id;
            }

            using (var scope2 = container.BeginLifetimeScope())
            {
                var u3 = scope2.ResolveNamed<IUnitOfWork>("scoped");
                id2 = u3.Id;
            }

            Assert.AreNotEqual(id1, id2);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TransactionContext_Is_PerMatchingLifetimeScope(bool declarative)
        {
            var container = BuildContainer(declarative);

            using (var tx = container.BeginLifetimeScope("transaction"))
            {
                var s1 = tx.Resolve<StepOneService>();
                var s2 = tx.Resolve<StepTwoService>();

                var field1 = typeof(StepOneService).GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var field2 = typeof(StepTwoService).GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var ctx1 = field1.GetValue(s1);
                var ctx2 = field2.GetValue(s2);

                Assert.AreSame(ctx1, ctx2);
            }
        }
    }
}
