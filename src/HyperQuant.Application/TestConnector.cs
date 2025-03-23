using System.Text;
using System.Text.Json;
using System.Net.WebSockets;

using RestSharp;

using HyperQuant.Domain.Contracts;
using HyperQuant.Domain.Model;

namespace HyperQuant.Application
{
    public sealed class TestConnector : ITestConnector, IDisposable
    {
        private readonly static HashSet<int> _validPeriods =
        [
            60,      // 1m
            300,     // 5m
            900,     // 15m
            1800,    // 30m
            3600,    // 1h
            10800,   // 3h
            21600,   // 6h
            43200,   // 12h
            86400,   // 1D
            604800,  // 1W
            1209600, // 14D
            2592000  // 1M
        ];

        private readonly Dictionary<string, ClientWebSocket> _tradeSubscriptions;
        private readonly Dictionary<string, ClientWebSocket> _candleSubscriptions;

        public TestConnector()
        {
            _tradeSubscriptions = [];
            _candleSubscriptions = [];
        }

        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0, CancellationToken stoppingToken = default)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }
            if (!_validPeriods.Contains(periodInSec))
            {
                throw new ArgumentException("Неверный период в секундах. Допустимые значения: 60, 300, 900, 1800, 3600, 10800, 21600, 43200, 86400, 604800, 1209600, 2592000.", nameof(periodInSec));
            }

            string filter = GetCandleFilter(pair, periodInSec, from, to);

            string timeFrame = GetTimeFrame(periodInSec);
            var options = new RestClientOptions($"https://api-pub.bitfinex.com/v2/candles/{filter}/hist");
            using var client = new RestClient(options);

            var request = new RestRequest();
            request.AddHeader("accept", "application/json");

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
                // TODO logs
                return [];
            }
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount, CancellationToken stoppingToken = default)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }
            if (maxCount < 0)
            {
                throw new ArgumentException("Значение максимального количества трейдов не может быть меньше нуля", nameof(maxCount));
            }

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
                // TODO logs
                return [];
            }
        }

        public void SubscribeTrades(string pair, int maxCount = 100, CancellationToken stoppingToken = default)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }
            if (maxCount < 0)
            {
                throw new ArgumentException("Значение максимального количества трейдов не может быть меньше нуля", nameof(maxCount));
            }

            if (_tradeSubscriptions.ContainsKey(pair))
            {
                if (_tradeSubscriptions.TryGetValue(pair, out ClientWebSocket? webSocket) && webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    throw new InvalidOperationException("Соединение все еще активно, для установления нового сначало вызовете метод UnsubscribeTrades.");
                }
            }

            var clientWebSocket = new ClientWebSocket();
            _tradeSubscriptions[pair] = clientWebSocket;

            SubscribeTradesAsync(clientWebSocket, pair, maxCount, stoppingToken).GetAwaiter().GetResult();
        }
       
        public void UnsubscribeTrades(string pair)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }

            if (_tradeSubscriptions.ContainsKey(pair))
            {
                if (_tradeSubscriptions.TryGetValue(pair, out ClientWebSocket? webSocket) && webSocket != null)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        var message = new
                        {
                            @event = "unsubscribe",
                            channel = "trades",
                            symbol = pair
                        };

                        SendMessageAsync(webSocket, JsonSerializer.Serialize(message)).GetAwaiter().GetResult();

                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
                    }
                   
                    webSocket.Dispose();
                }

                _tradeSubscriptions.Remove(pair);
            }
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0, CancellationToken stoppingToken = default)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }
            if (!_validPeriods.Contains(periodInSec))
            {
                throw new ArgumentException("Неверный период в секундах. Допустимые значения: 60, 300, 900, 1800, 3600, 10800, 21600, 43200, 86400, 604800, 1209600, 2592000.", nameof(periodInSec));
            }

            if (_candleSubscriptions.ContainsKey(pair))
            {
                if (_candleSubscriptions.TryGetValue(pair, out ClientWebSocket? webSocket) && webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    throw new InvalidOperationException("Соединение все еще активно, для установления нового сначало вызовете метод UnsubscribeCandles.");
                }
            }

            var clientWebSocket = new ClientWebSocket();
            _candleSubscriptions[pair] = clientWebSocket;

            string candleFilter = GetCandleFilter(pair, periodInSec, from, to, count);

            SubscribeCandlesAsync(clientWebSocket, pair, candleFilter, count, stoppingToken).GetAwaiter().GetResult();
        }
        
        public void UnsubscribeCandles(string pair)
        {
            if (string.IsNullOrEmpty(pair))
            {
                throw new ArgumentNullException(nameof(pair), "Значение валютной пары не может быть пустым");
            }

            if (_candleSubscriptions.ContainsKey(pair))
            {
                if (_candleSubscriptions.TryGetValue(pair, out ClientWebSocket? webSocket) && webSocket != null)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        var message = new
                        {
                            @event = "unsubscribe",
                            channel = "candles",
                            symbol = pair
                        };

                        SendMessageAsync(webSocket, JsonSerializer.Serialize(message)).GetAwaiter().GetResult();

                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
                    }

                    webSocket.Dispose();
                }

                _candleSubscriptions.Remove(pair);
            }
        }

        private async Task SubscribeTradesAsync(ClientWebSocket clientWebSocket, string pair, int maxCount = 100, CancellationToken stoppingToken = default)
        {
            await clientWebSocket.ConnectAsync(new Uri("wss://api-pub.bitfinex.com/ws/2"), stoppingToken);

            var messageSubscribe = new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = pair
            };

            await SendMessageAsync(clientWebSocket, JsonSerializer.Serialize(messageSubscribe), stoppingToken);

            var buffer = new byte[2048];
            int currentCount = 0;

            while (clientWebSocket.State == WebSocketState.Open && currentCount < maxCount && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    currentCount = ProcessTradeMessage(message, pair, currentCount, maxCount);
                }
                catch (TaskCanceledException)
                {
                    // TODO logs
                }
            }
        }
     
        private async Task SubscribeCandlesAsync(ClientWebSocket clientWebSocket, string pair, string candleFilter, long? count = 0, CancellationToken stoppingToken = default)
        {
            await clientWebSocket.ConnectAsync(new Uri("wss://api-pub.bitfinex.com/ws/2"), stoppingToken);

            var messageSubscribe = new
            {
                @event = "subscribe",
                channel = "candles",
                key = candleFilter
            };

            await SendMessageAsync(clientWebSocket, JsonSerializer.Serialize(messageSubscribe), stoppingToken);

            var buffer = new byte[20480];
            int currentCount = 0;

            while (clientWebSocket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                if (count.HasValue && count > 0 && currentCount >= count)
                {
                    break;
                }

                try
                {
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    currentCount = ProcessCandleMessage(message, pair, currentCount, Convert.ToInt32(count));
                }
                catch (TaskCanceledException)
                {
                    // TODO logs
                }
            }
        }

        private string GetCandleFilter(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            var stringBuilder = new StringBuilder($"trade:{GetTimeFrame(periodInSec)}:{pair}");

            if (count.HasValue && count > 0 && count != null)
            {
                stringBuilder.Append($":a{count}");
            }

            if (from.HasValue && to.HasValue)
            {
                // Вычисляем количество периодов
                long fromPeriod = (long)((from - DateTimeOffset.UtcNow).Value.TotalSeconds / periodInSec);
                long toPeriod = (long)((to - DateTimeOffset.UtcNow).Value.TotalSeconds / periodInSec);

                stringBuilder.Append($":p{Math.Abs(fromPeriod)}");
                stringBuilder.Append($":p{Math.Abs(toPeriod)}");
            }

            return stringBuilder.ToString();
        }

        private static async Task SendMessageAsync(ClientWebSocket clientWebSocket, string message, CancellationToken stoppingToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, stoppingToken);
        }
       
        private int ProcessTradeMessage(string message, string pair, int currentCount, int maxCount)
        {
            var root = JsonDocument.Parse(message).RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                JsonElement trades = root[root.GetArrayLength() - 1];

                if (trades.ValueKind == JsonValueKind.Array)
                {
                    if (trades[0].ValueKind == JsonValueKind.Number)
                    {
                        RaiseTrade(trades);
                        currentCount++;
                    }
                    else
                    {
                        foreach (var tradeData in trades.EnumerateArray())
                        {
                            RaiseTrade(tradeData);

                            currentCount++;
                            if (currentCount == maxCount)
                            {
                                break;
                            }
                        }
                    }
                }

                void RaiseTrade(JsonElement tradeData)
                {
                    long id = tradeData[0].GetInt64();
                    long mts = tradeData[1].GetInt64();
                    double amount = tradeData[2].GetDouble();
                    double price = tradeData[3].GetDouble();

                    var trade = new Trade()
                    {
                        Id = id.ToString(),
                        Pair = pair,
                        Time = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                        Amount = Convert.ToDecimal(amount),
                        Price = Convert.ToDecimal(price)
                    };

                    if (trade.Amount > 0)
                    {
                        NewBuyTrade?.Invoke(trade);
                    }
                    else
                    {
                        NewSellTrade?.Invoke(trade);
                    }
                }
            }

            return currentCount;
        }

        private int ProcessCandleMessage(string message, string pair, int currentCount, int maxCount)
        {
            var root = JsonDocument.Parse(message).RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                JsonElement candles = root[root.GetArrayLength() - 1];

                if (candles.ValueKind == JsonValueKind.Array)
                {
                    if (candles.GetArrayLength() == 0)
                    {
                        return currentCount;
                    }

                    if (candles[0].ValueKind == JsonValueKind.Number)
                    {
                        RaiseCandle(candles);
                        currentCount++;
                    }
                    else
                    {
                        foreach (var candleData in candles.EnumerateArray())
                        {
                            RaiseCandle(candleData);

                            currentCount++;
                            if (currentCount == maxCount)
                            {
                                break;
                            }
                        }
                    }
                }

                void RaiseCandle(JsonElement candleData) 
                {
                    long mts = candleData[0].GetInt64();
                    double open = candleData[1].GetDouble();
                    double close = candleData[2].GetDouble();
                    double high = candleData[3].GetDouble();
                    double low = candleData[4].GetDouble();
                    double volume = candleData[5].GetDouble();

                    var candle = new Candle()
                    {
                        Pair = pair,
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(mts),
                        OpenPrice = Convert.ToDecimal(open),
                        ClosePrice = Convert.ToDecimal(close),
                        HighPrice = Convert.ToDecimal(high),
                        LowPrice = Convert.ToDecimal(low)
                    };

                    CandleSeriesProcessing?.Invoke(candle);
                }
            }

            return currentCount;
        }

        private static string GetTimeFrame(int periodInSec) => periodInSec switch
        {
            60 => "1m",
            300 => "5m",
            900 => "15m",
            1800 => "30m",
            3600 => "1h",
            10800 => "3h",
            21600 => "6h",
            43200 => "12h",
            86400 => "1D",
            604800 => "1W",
            1209600 => "14D",
            2592000 => "1M",
            _ => throw new ArgumentException("Неверный период в секундах."),
        };

        public void Dispose()
        {
            foreach (var webSocketTrade in _tradeSubscriptions.Values)
            {
                if (webSocketTrade.State == WebSocketState.Open)
                {
                    webSocketTrade.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
                }
                webSocketTrade.Dispose();
            }

            foreach (var webSocketCandle in _candleSubscriptions.Values)
            {
                if (webSocketCandle.State == WebSocketState.Open)
                {
                    webSocketCandle.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
                }
                webSocketCandle.Dispose();
            }
        }
    }
}
