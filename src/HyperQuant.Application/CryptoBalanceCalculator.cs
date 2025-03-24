using HyperQuant.Domain.Model;
using System.Text.Json;

namespace HyperQuant.Application
{
    public class CryptoBalanceCalculator
    {
        private const string BTC = "BTC";
        private const string XRP = "XRP";
        private const string XMR = "XMR";
        private const string DASH = "DASH";

        private readonly Dictionary<string, decimal> _balances = new()
        {
           { BTC, 1 },
           { XRP, 15000 },
           { XMR, 50 },
           { DASH, 30 }
        };

        public async Task<IEnumerable<CryptoBalance>> GetPortfolioBalancesAsync()
        {
            var cryptoBalance = new CryptoBalance() { Currency = "1BTC+15000XRP+50XMR+30DASH" };

            var balances = new CryptoBalance[]
            {
                cryptoBalance
            };

            var prices = await GetCryptoPricesAsync();

            decimal totalPriceInUSDT = _balances[BTC] * prices[BTC] + _balances[XRP] * prices[XRP] + _balances[XMR] * prices[XMR] + _balances[DASH] * prices[DASH];
            cryptoBalance.USDT = totalPriceInUSDT;

            // Конвертируем цены в BTC
            decimal btcPriceInUsd = prices[BTC];
            decimal totalPriceInBTC = GetTotalPrice(btcPrice: 1, ConvertPrice(prices[XRP], btcPriceInUsd), ConvertPrice(prices[XMR], btcPriceInUsd), ConvertPrice(prices[DASH], btcPriceInUsd));
            cryptoBalance.BTC = GetRoundPrice(totalPriceInBTC);

            // Конвертируем цены в XRP
            decimal xrpPriceInUsd = prices[XRP];
            decimal totalPriceInXRP = GetTotalPrice(ConvertPrice(prices[BTC], xrpPriceInUsd), xrpPrice: 1, ConvertPrice(prices[XMR], xrpPriceInUsd), ConvertPrice(prices[DASH], xrpPriceInUsd));
            cryptoBalance.XRP = GetRoundPrice(totalPriceInXRP);

            // Конвертируем цены в XMR
            decimal xmrPriceInUsd = prices[XMR];
            decimal totalPriceInXMR = GetTotalPrice(ConvertPrice(prices[BTC], xmrPriceInUsd), ConvertPrice(prices[XRP], xmrPriceInUsd), xmrPrice: 1, ConvertPrice(prices[DASH], xmrPriceInUsd));
            cryptoBalance.XMR = GetRoundPrice(totalPriceInXMR);

            // Конвертируем цены в DASH
            decimal dashPriceInUsd = prices[DASH];
            decimal totalPriceInDASH = GetTotalPrice(ConvertPrice(prices[BTC], dashPriceInUsd), ConvertPrice(prices[XRP], dashPriceInUsd), ConvertPrice(prices[XMR], dashPriceInUsd), dashPrice: 1);
            cryptoBalance.DASH = GetRoundPrice(totalPriceInDASH);

            return balances;
        }

        private decimal GetTotalPrice(decimal btcPrice, decimal xrpPrice, decimal xmrPrice, decimal dashPrice)
        {
            return _balances[BTC] * btcPrice + _balances[XRP] * xrpPrice + _balances[XMR] * xmrPrice + _balances[DASH] * dashPrice;
        }

        private static decimal GetRoundPrice(decimal price) => Math.Round(price, 2);

        private static decimal ConvertPrice(decimal priceInUsdFrom, decimal priceInUsdTo) => priceInUsdFrom / priceInUsdTo;

        private static async Task<Dictionary<string, decimal>> GetCryptoPricesAsync()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ripple,monero,dash&vs_currencies=usd");
            var prices = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(response)!;

            // Получаем цены в USD
            var result = new Dictionary<string, decimal>
            {
                { BTC, prices["bitcoin"]["usd"] }, // Цена BTC в USD
                { XRP, prices["ripple"]["usd"] }, // Цена XRP в USD
                { XMR, prices["monero"]["usd"] }, // Цена XMR в USD
                { DASH, prices["dash"]["usd"] } // Цена DASH в USD
            };

            return result;
        }
    }
}
