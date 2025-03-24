namespace HyperQuant.Domain.Model
{
    public class CryptoBalance
    {
        public string Currency { get; set; } = null!;
        public decimal USDT { get; set; }
        public decimal BTC { get; set; }
        public decimal XRP { get; set; }
        public decimal XMR { get; set; }
        public decimal DASH { get; set; }
    }
}
