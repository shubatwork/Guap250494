using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Futures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplifiedTradingBot
{
    internal class Program
    {
        private static KucoinRestClient restClient;
        private const int MaxPositions = 100;

        static async Task Main(string[] args)
        {
            restClient = new KucoinRestClient();
            restClient.SetApiCredentials(new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

            while (true)
            {
                await Task.Delay(5000);

                try
                {
                    // 1. Get account balance
                    var balanceResult = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
                    if (!balanceResult.Success) continue;

                    // 2. Get all symbols and calculate levels
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    if (!tickerList.Success) continue;

                    // 3. Process each symbol
                    foreach (var ticker in tickerList.Data)
                    {
                        var candles = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(
                            ticker.Symbol,
                            FuturesKlineInterval.OneDay,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow);

                        if (!candles.Success || candles.Data.Count() < 2) continue;

                        var prevDay = candles.Data.FirstOrDefault(x =>
                            x.OpenTime.Date == DateTime.UtcNow.AddDays(-1).Date);
                        if (prevDay == null) continue;

                        var currentPrice = await GetCurrentPrice(ticker.Symbol);
                        if (currentPrice == null) continue;

                        var levels = CalculatePivotLevels(
                            prevDay.HighPrice,
                            prevDay.LowPrice,
                            prevDay.ClosePrice
                        );

                        // Determine nearest level
                        var (targetLevel, targetSide) = currentPrice > levels.pivot
                            ? (FindNearestSupport(currentPrice.Value, levels.supports), OrderSide.Buy)
                            : (FindNearestResistance(currentPrice.Value, levels.resistances), OrderSide.Sell);

                        if (!targetLevel.HasValue) continue;

                        // Order management
                        var openOrders = await GetOpenOrders(ticker.Symbol);
                        await CancelNonNearestOrders(openOrders, targetLevel.Value, targetSide);

                        if (!OrderExistsAtLevel(openOrders, targetLevel.Value, targetSide))
                        {
                            await PlaceLimitOrder(ticker.Symbol, targetSide, targetLevel.Value, 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static decimal? FindNearestSupport(decimal currentPrice, decimal[] supports)
        {
            return supports.Where(s => s < currentPrice)
                          .OrderByDescending(s => s)
                          .FirstOrDefault();
        }

        private static decimal? FindNearestResistance(decimal currentPrice, decimal[] resistances)
        {
            return resistances.Where(r => r > currentPrice)
                             .OrderBy(r => r)
                             .FirstOrDefault();
        }

        private static async Task CancelNonNearestOrders(IEnumerable<KucoinFuturesOrder> orders, decimal targetLevel, OrderSide targetSide)
        {
            foreach (var order in orders.Where(o =>
                o.Price != targetLevel ||
                o.Side != targetSide))
            {
                await restClient.FuturesApi.Trading.CancelOrderAsync(order.Id);
            }
        }

        private static bool OrderExistsAtLevel(IEnumerable<KucoinFuturesOrder> orders, decimal level, OrderSide side)
        {
            return orders.Any(o =>
                Math.Abs((decimal)(o.Price - level)) < 0.001m &&
                o.Side == side);
        }

        // Keep these methods unchanged from original code:
        private static (decimal pivot, decimal[] supports, decimal[] resistances) CalculatePivotLevels(
            decimal high, decimal low, decimal close)
        {
            decimal pivot = (high + low + close) / 3;
            decimal range = high - low;
            return (
                pivot,
                new[] { pivot - range * 0.382m, pivot - range * 0.618m, pivot - range * 1.0m },
                new[] { pivot + range * 0.382m, pivot + range * 0.618m, pivot + range * 1.0m }
            );
        }

        private static async Task<decimal?> GetCurrentPrice(string symbol)
        {
            var priceResult = await restClient.FuturesApi.ExchangeData.GetCurrentMarkPriceAsync(symbol);
            return priceResult.Success ? priceResult.Data?.IndexPrice : null;
        }

        private static async Task<IEnumerable<KucoinFuturesOrder>> GetOpenOrders(string symbol)
        {
            var ordersResult = await restClient.FuturesApi.Trading.GetOrdersAsync(symbol: symbol);
            return ordersResult.Success ? ordersResult.Data.Items : Enumerable.Empty<KucoinFuturesOrder>();
        }

        private static async Task PlaceLimitOrder(string symbol, OrderSide side, decimal price, decimal quantity)
        {
            await restClient.FuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: side,
                type: NewOrderType.Limit,
                leverage: 20,
                quantity: (int)quantity,
                price: price,
                timeInForce: TimeInForce.GoodTillCanceled,
                marginMode: FuturesMarginMode.Cross
            );
        }
    }
}