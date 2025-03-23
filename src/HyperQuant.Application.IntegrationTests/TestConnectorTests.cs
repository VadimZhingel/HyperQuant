using HyperQuant.Domain.Contracts;

namespace HyperQuant.Application.IntegrationTests
{
    public class TestConnectorTests
    {
        private readonly ITestConnector _testConnector;

        public TestConnectorTests()
        {
            _testConnector = new TestConnector();
        }

        #region Rest

        [Theory]
        [InlineData("tBTCUSD")]
        [InlineData("tETHUSD")]
        [InlineData("fUSD")]
        [InlineData("fBTC")]
        public async Task GetNewTradesAsync_ReturnsTrades_WhenCalledWithValidPair(string pair)
        {
            // Arrange
            var maxCount = 10;

            // Act
            var result = await _testConnector.GetNewTradesAsync(pair, maxCount, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Any(), "�� ���� �������� �� ����� ������.");
        }

        [Fact]
        public async Task GetNewTradesAsync_ReturnsEmpty_WhenCalledWithInvalidPair()
        {
            // Arrange
            string pair = "Fake";
            var maxCount = 10;

            // Act
            var result = await _testConnector.GetNewTradesAsync(pair, maxCount, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(!result.Any());
        }

        [Theory]
        [InlineData("tBTCUSD", 60)]
        public async Task GetCandleSeriesAsync_ReturnsCandles_WhenCalledWithValidParams(string pair, int periodInSec)
        {
            // Act
            var result = await _testConnector.GetCandleSeriesAsync(pair, periodInSec, stoppingToken: CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Any(), "�� ��� ������� �� ���� ������ ���.");
        }

        [Theory]
        [InlineData("tBTCUSD", 3600)]
        public async Task GetCandleSeriesAsync_ReturnsCandles_WhenCalledWithValidAggregateParams(string pair, int periodInSec)
        {
            // Arrange
            int count = 30;
            DateTimeOffset currentTime = DateTimeOffset.UtcNow; // ������� �����
            DateTimeOffset from = currentTime.AddHours(-2); // 2 ���� �����
            DateTimeOffset to = currentTime.AddHours(-30); // 30 ����� �����

            // Act
            var result = await _testConnector.GetCandleSeriesAsync(pair, periodInSec, from, to, count, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Any(), "�� ��� ������� �� ���� ������ ���.");
        }

        #endregion

        #region Socket

        [Theory]
        [InlineData("tBTCUSD", 20)]
        [InlineData("tBTCUSD", 10)]
        [InlineData("tBTCUSD", 40)]
        public void SubscribeTrades_ReturnsTrades_WhenCalledWithValidParam(string pair, int maxCount)
        {
            // Arrange
            int tradesCount = 0;

            _testConnector.NewBuyTrade += trade =>
            {
                tradesCount++;
            };

            _testConnector.NewSellTrade += trade =>
            {
                tradesCount++;
            };

            // Act
            _testConnector.SubscribeTrades(pair, maxCount, CancellationToken.None);
            _testConnector.UnsubscribeTrades(pair);

            // Assert
            Assert.True(tradesCount == maxCount, "�������� �������� ���������� ������.");
        }

        [Fact]
        public async Task SubscribeTrades_ShouldThrowInvalidOperationException_WhenAlreadySubscribed()
        {
            // Arrange
            string pair = "tBTCUSD";

            // Act
            _ = Task.Run(() => { _testConnector.SubscribeTrades(pair); });
            await Task.Delay(1000);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _testConnector.SubscribeTrades(pair));
            Assert.Equal("���������� ��� ��� �������, ��� ������������ ������ ������� �������� ����� UnsubscribeTrades.", exception.Message);
        }

        [Theory]
        [InlineData("tBTCUSD", 60)]
        public void SubscribeCandles_ReturnsCandles_WhenCalledWithValidParams(string pair, int periodInSec)
        {
            // Arrange
            int candlesCount = 0;

            _testConnector.CandleSeriesProcessing += candle =>
            {
                candlesCount++;
            };

            // Act
            Task.Delay(2000).ContinueWith(t => _testConnector.UnsubscribeCandles(pair));

            _testConnector.SubscribeCandles(pair, periodInSec);
            // Assert
            Assert.True(candlesCount > 0, "�������� �������� ���������� �������� ���.");
        }

        [Theory]
        [InlineData("fUSD", 3600)]
        public void SubscribeCandles_ReturnsCandles_WhenCalledWithValidAggregateParams(string pair, int periodInSec)
        {
            // Arrange
            int candlesCount = 0;
            int count = 30;
            DateTimeOffset currentTime = DateTimeOffset.UtcNow; // ������� �����
            DateTimeOffset from = currentTime.AddHours(-2); // 2 ���� �����
            DateTimeOffset to = currentTime.AddHours(-30); // 30 ����� �����
            var cts = new CancellationTokenSource();

            _testConnector.CandleSeriesProcessing += candle =>
            {
                candlesCount++;
            };

            // Act
            Task.Delay(2000).ContinueWith(t => cts.Cancel());

            _testConnector.SubscribeCandles(pair, periodInSec, from, to, count, cts.Token);
            _testConnector.UnsubscribeCandles(pair);
            // Assert
            Assert.True(candlesCount > 0, "�������� �������� ���������� �������� ���.");
        }

        #endregion
    }
}