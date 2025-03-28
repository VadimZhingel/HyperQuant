﻿using HyperQuant.Domain.Model;

namespace HyperQuant.Domain.Contracts
{
    public interface ITestConnector
    {
        #region Rest

        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount, CancellationToken stoppingToken = default);

        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0, CancellationToken stoppingToken = default);

        #endregion

        #region Socket

        event Action<Trade> NewBuyTrade;

        event Action<Trade> NewSellTrade;

        void SubscribeTrades(string pair, int maxCount = 100, CancellationToken stoppingToken = default);

        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;

        void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0, CancellationToken stoppingToken = default);

        void UnsubscribeCandles(string pair);

        #endregion
    }
}
