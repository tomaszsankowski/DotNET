using Xunit;
using System.Text.Json;

namespace Ztm.Tests
{
    public class ZtmTests
    {
        [Fact]
        public void ParseDepartureJson_ShouldReturnCorrectRouteShortName()
        {
            string json = @"{
                ""lastUpdate"": ""2025-10-07T18:24:18Z"",
                ""departures"": [
                    {
                        ""routeShortName"": ""9"",
                        ""headsign"": ""Strzyża PKM"",
                        ""estimatedTime"": ""2025-10-07T18:27:29Z"",
                        ""delayInSeconds"": 60
                    }
                ]
            }";

            var result = JsonSerializer.Deserialize<ZtmResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(result);
            Assert.Single(result.Departures);
            Assert.Equal("9", result.Departures[0].RouteShortName);
        }
    }
}
