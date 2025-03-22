namespace HyperQuant.Domain.Model
{
    public class Candle
    {
        /// <summary>
        /// Валютная пара
        /// </summary>
        public string Pair { get; set; } = null!;

        /// <summary>
        /// Цена открытия
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// Максимальная цена
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Минимальная цена
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Цена закрытия
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// Partial (Общая сумма сделок)
        /// </summary>
        public decimal TotalPrice { get; private set; }

        /// <summary>
        /// Partial (Общий объем)
        /// </summary>
        public decimal TotalVolume { get; private set; }

        /// <summary>
        /// Время
        /// </summary>
        public DateTimeOffset OpenTime { get; set; }

        public void CalculateTotals(IEnumerable<Trade> trades)
        {
            if (trades == null)
            {
                throw new ArgumentNullException(nameof(trades));
            }

            var relevantTrades = trades.Where(t => t.Pair == Pair && t.Time < OpenTime);

            TotalPrice = relevantTrades.Sum(t => t.Price * t.Amount);
            TotalVolume = relevantTrades.Sum(t => t.Amount);
        }
    }
}
