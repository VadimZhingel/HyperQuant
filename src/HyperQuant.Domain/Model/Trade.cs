namespace HyperQuant.Domain.Model
{
    public class Trade
    {
        /// <summary>
        /// Id трейда
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// Валютная пара
        /// </summary>
        public string Pair { get; set; } = null!;

        /// <summary>
        /// Цена трейда
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Объем трейда
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Направление (buy/sell)
        /// </summary>
        public string Side => Amount > 0 ? "buy" : "sell";

        /// <summary>
        /// Время трейда
        /// </summary>
        public DateTimeOffset Time { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}; Time: {Time}; Amount: {Amount}; Price: {Price}; Pair: {Pair}; Side: {Side}";
        }
    }
}
