using HyperQuant.Domain.Contracts;
using HyperQuant.Domain.Model;
using HyperQuant.WPF.Common.Commands;
using HyperQuant.WPF.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HyperQuant.WPF.ViewModel
{
    internal class MainViewModel(ITestConnector testConnector, CancellationTokenSource cancellationTokenSource) : ViewModelBase
    {
        private readonly ITestConnector _testConnector = testConnector;
        private readonly CancellationTokenSource _cancellationTokenSource = cancellationTokenSource;

        private string _pair = "tBTCUSD";

        public string Pair
        {
            get => _pair;
            set => Set(ref _pair, value);
        }

        private int _count = 50;

        public int Count 
        {
            get => _count;
            set => Set(ref _count, value);
        }

        private int _periodInSec = 60;

        public int PeriodInSec
        {
            get => _periodInSec;
            set => Set(ref _periodInSec, value);
        }

        private ObservableCollection<Trade> _trades = [];

        public ReadOnlyObservableCollection<Trade> Trades => new(_trades);

        private ObservableCollection<Candle> _candles = [];

        public ReadOnlyObservableCollection<Candle> Candles => new(_candles);

        #region GetNewTradesCommand

        private LambdaCommand? _getNewTradesCommand;

        public ICommand GetNewTradesCommand => _getNewTradesCommand ??= new LambdaCommand(async () =>
        {
            await GetNewTradesExecutedAsync();

        }, GetNewTradesCanExecute);

        private async Task GetNewTradesExecutedAsync()
        {
            var trades = await _testConnector.GetNewTradesAsync(Pair, Count, _cancellationTokenSource.Token);
            _trades.Clear();
            foreach (var trade in trades)
            {
                _trades.Add(trade);
            }
        }

        private bool GetNewTradesCanExecute() => !string.IsNullOrEmpty(Pair) && Count > 0;

        #endregion

        #region GetCandleSeriesCommand

        private LambdaCommand? _getCandleSeriesCommand;

        public ICommand GetCandleSeriesCommand => _getCandleSeriesCommand ??= new LambdaCommand(async () =>
        {
            await GetCandleSeriesExecutedAsync();

        }, GetCandleSeriesExecutedCanExecute);

        private async Task GetCandleSeriesExecutedAsync()
        {
            var candles = await _testConnector.GetCandleSeriesAsync(Pair, PeriodInSec, stoppingToken: _cancellationTokenSource.Token);
            _candles.Clear();
            foreach (var candle in candles)
            {
                _candles.Add(candle);
            }
        }

        private bool GetCandleSeriesExecutedCanExecute() => !string.IsNullOrEmpty(Pair);

        #endregion
    }
}
