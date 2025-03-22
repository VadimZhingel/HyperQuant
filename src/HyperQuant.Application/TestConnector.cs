using HyperQuant.Domain.Contracts;
using HyperQuant.Domain.Model;
using RestSharp;
using System.Text.Json;

namespace HyperQuant.Application
{
    public class TestConnector : ITestConnector
    {
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount, CancellationToken stoppingToken = default)
        {
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/trades/{pair}/hist");
            using var client = new RestClient(options);
            var request = new RestRequest();
            request.AddHeader("accept", "application/json");
            request.AddParameter("limit", maxCount);
            request.AddParameter("sort", -1);

            var response = await client.ExecuteAsync<List<JsonDocument>>(request, stoppingToken);

            if (response.IsSuccessful && response.Data != null)
            {
                var trades = new List<Trade>(response.Data.Count);

                foreach (var data in response.Data)
                {
                    JsonElement root = data.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        long firstValue = root[0].GetInt64();
                        long secondValue = root[1].GetInt64();
                        double thirdValue = root[2].GetDouble();
                        double fourthValue = root[3].GetDouble();

                        var trade = new Trade() 
                        {
                            Id = firstValue.ToString(),
                            Time = DateTimeOffset.FromUnixTimeMilliseconds(secondValue),
                            Amount = Convert.ToDecimal(thirdValue),
                            Price = Convert.ToDecimal(fourthValue),
                            Pair = pair,
                        };

                        trades.Add(trade);
                    }
                }
               
                return trades;
            }
            else
            {
                // TODO add logs
                return [];
            }
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            throw new NotImplementedException();
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeCandles(string pair)
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeTrades(string pair)
        {
            throw new NotImplementedException();
        }
    }
}
