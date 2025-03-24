using HyperQuant.Domain.Contracts;
using HyperQuant.WPF.Common.Commands;
using HyperQuant.WPF.ViewModel.Base;
using System.Windows.Input;

namespace HyperQuant.WPF.ViewModel
{
    internal class MainViewModel(ITestConnector testConnector, CancellationTokenSource cancellationTokenSource) : ViewModelBase
    {
        private readonly ITestConnector _testConnector = testConnector;
        private readonly CancellationTokenSource _cancellationTokenSource = cancellationTokenSource;

        private string _pair = string.Empty;

        public string Pair
        {
            get => _pair;
            set => Set(ref _pair, value);
        }

        private int _count;

        public int Count 
        {
            get => _count;
            set => Set(ref _count, value);
        }

        private int _periodInSec;

        public int PeriodInSec
        {
            get => _periodInSec;
            set => Set(ref _periodInSec, value);
        }

        #region GetNewTradesCommand

        private LambdaCommand? _getNewTradesCommand;

        public ICommand GetNewTradesCommand => _getNewTradesCommand ??= new LambdaCommand(async () =>
        {
            await GetNewTradesExecutedAsync();

        }, GetNewTradesCanExecute);

        private async Task GetNewTradesExecutedAsync()
        {
            await _testConnector.GetNewTradesAsync(Pair, Count, _cancellationTokenSource.Token);
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
            await _testConnector.GetCandleSeriesAsync(Pair, PeriodInSec, stoppingToken: _cancellationTokenSource.Token);
        }

        private bool GetCandleSeriesExecutedCanExecute() => !string.IsNullOrEmpty(Pair);

        #endregion
    }
}
