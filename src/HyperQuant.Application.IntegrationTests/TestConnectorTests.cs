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
            Assert.True(result.Any(), "Не было получено ни одной сделки.");
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
        [InlineData("tBTCUSD", 1)]
        public async Task GetCandleSeriesAsync_ReturnsCandles_WhenCalledWithValidPair(string pair, int periodInSec)
        {
            // Arrange
            long count = 10;
            var dateTime = new DateTime(2025, 1, 1);

            // Act
            var result = await _testConnector.GetCandleSeriesAsync(pair, periodInSec, new DateTimeOffset(dateTime), DateTimeOffset.Now, count, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Any(), "Не был получен ни один график цен.");
        }
    }
}