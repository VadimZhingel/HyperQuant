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

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0, CancellationToken stoppingToken = default)
        {
            string timeFrame = $"{periodInSec}m";
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/candles/trade:{timeFrame}:{pair}/hist");
            using var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("accept", "application/json");
            if (from.HasValue)
            {
                request.AddParameter("start", ((DateTimeOffset)from).ToUnixTimeMilliseconds());
            }
            if (to.HasValue)
            {
                request.AddParameter("end", ((DateTimeOffset)to).ToUnixTimeMilliseconds());
            }
            if (count.HasValue && count > 0)
            {
                request.AddParameter("limit", Convert.ToInt32(count));
            }

            var response = await client.ExecuteAsync<List<JsonDocument>>(request, stoppingToken);

            if (response.IsSuccessful && response.Data != null)
            {
                var candles = new List<Candle>(response.Data.Count);

                foreach (var data in response.Data)
                {
                    JsonElement root = data.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        long mts = root[0].GetInt64();
                        long open = root[1].GetInt64();
                        long close = root[2].GetInt64();
                        long high = root[3].GetInt64();
                        long low = root[4].GetInt64();
                        double volume = root[5].GetDouble();

                        var candle = new Candle()
                        {
                            Pair = pair,
                            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                            OpenPrice = Convert.ToDecimal(open),
                            ClosePrice = Convert.ToDecimal(close),
                            HighPrice = Convert.ToDecimal(high),
                            LowPrice = Convert.ToDecimal(low)
                        };

                        candles.Add(candle);
                    }
                }

                return candles;
            }
            else
            {
                // TODO add logs
                return [];
            }
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
                        long id = root[0].GetInt64();
                        long mts = root[1].GetInt64();
                        double amount = root[2].GetDouble();
                        double price = root[3].GetDouble();

                        var trade = new Trade() 
                        {
                            Id = id.ToString(),
                            Time = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                            Amount = Convert.ToDecimal(amount),
                            Price = Convert.ToDecimal(price),
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
