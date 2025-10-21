using System.Net.Http.Json;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Użycie: ztmcli {id_przystanku}");
            return;
        }

        string stopId = args[0];
        string departuresUrl = $"https://ckan2.multimediagdansk.pl/departures?stopId={stopId}";
        string stopsUrl = "https://ckan.multimediagdansk.pl/dataset/c24aa637-3619-4dc2-a171-a23eec8f2172/resource/4c4025f0-01bf-41f7-a39f-d156d201b82b/download/stops.json";

        using var client = new HttpClient();

        try
        {
            var allStops = await client.GetFromJsonAsync<Dictionary<string, StopsData>>(stopsUrl);

            if (allStops == null || allStops.Count == 0)
            {
                Console.WriteLine("Nie udało się pobrać listy przystanków.");
                return;
            }

            var latestDate = allStops.Keys.Max();
            var stopsData = allStops[latestDate];

            var stop = stopsData.Stops.FirstOrDefault(s => s.StopId.ToString() == stopId);

            if (stop == null)
            {
                Console.WriteLine("Nie znaleziono przystanku o podanym stopId.");
                return;
            }

            Console.WriteLine($"Przystanek: {stop.StopName} ({stop.Type})");
            Console.WriteLine("--------------------------------------------------------------");

            var data = await client.GetFromJsonAsync<ZtmResponse>(departuresUrl);

            if (data == null || data.Departures == null || data.Departures.Count == 0)
            {
                Console.WriteLine("Brak danych dla tego przystanku — możliwe, że podano błędny numer.");
                return;
            }

            foreach (var dep in data.Departures.Take(10))
            {
                double delayMin = (dep.DelayInSeconds ?? 0) / 60.0;
                string delayStr = dep.DelayInSeconds == null || delayMin == 0 ? "planowo" :
                                  delayMin > 0 ? $"+{delayMin:F1} min" :
                                  $"{delayMin:F1} min";

                Console.WriteLine($"{dep.RouteShortName,3} -> {dep.Headsign,-30} {DateTime.Parse(dep.EstimatedTime):HH:mm} ({delayStr})");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Nie znaleziono przystanku lub wystąpił błąd połączenia z serwerem ZTM.");
        }
    }
}

public class StopsData
{
    public List<Stop> Stops { get; set; }
}

public class Stop
{
    public int StopId { get; set; }
    public string StopName { get; set; }
    public string StopDesc { get; set; }
    public string Type { get; set; }
}

public class ZtmResponse
{
    public string LastUpdate { get; set; }
    public List<Departure> Departures { get; set; }
}

public class Departure
{
    public string RouteShortName { get; set; }
    public string Headsign { get; set; }
    public string EstimatedTime { get; set; }
    public int? DelayInSeconds { get; set; }
}
