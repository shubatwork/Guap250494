using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Futures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompleteTradingBot
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

                try
                {
                    // 1. Get account balance
                    var balanceResult = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
                    if (!balanceResult.Success) continue;

                    // 2. Get all symbols
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    if (!tickerList.Success) continue;

                    // 3. Get current positions
                    var positionResult = await restClient.FuturesApi.Account.GetPositionsAsync();
                    var positions = positionResult.Success ? positionResult.Data : new List<KucoinPosition>();

                    foreach (var ticker in tickerList.Data)
                    {
                        var candles = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(
                            ticker.Symbol,
                            FuturesKlineInterval.OneHour,
                            DateTime.UtcNow.AddHours(-4),
                            DateTime.UtcNow);

                        if (!candles.Success || candles.Data.Count() < 2) continue;

                        var prevDay = new KucoinFuturesKline()
                        {
                            OpenPrice = candles.Data.Sum(x=>x.OpenPrice) / candles.Data.Count(),
                            ClosePrice = candles.Data.Sum(x => x.ClosePrice) / candles.Data.Count(),
                            HighPrice = candles.Data.Sum(x => x.HighPrice) / candles.Data.Count(),
                            LowPrice = candles.Data.Sum(x => x.LowPrice) / candles.Data.Count()
                        };
                        if (prevDay == null) continue;

                        var currentPrice = await GetCurrentPrice(ticker.Symbol);
                        if (currentPrice == null) continue;

                        // Calculate pivot levels
                        var levels = CalculatePivotLevels(
                            prevDay.HighPrice,
                            prevDay.LowPrice,
                            prevDay.ClosePrice
                        );

                        // ENTRY LOGIC: Nearest level orders
                        var (targetLevel, targetSide) = currentPrice > levels.pivot
                            ? (FindNearestSupport(currentPrice.Value, levels.supports), OrderSide.Buy)
                            : (FindNearestResistance(currentPrice.Value, levels.resistances), OrderSide.Sell);

                        if (targetLevel.HasValue)
                        {
                            var openOrders = await GetOpenOrders(ticker.Symbol);
                            await CancelNonNearestOrders(openOrders, targetLevel.Value, targetSide);
                            if (!OrderExistsAtLevel(openOrders, targetLevel.Value, targetSide))
                            {
                                await PlaceLimitOrder(ticker.Symbol, targetSide, targetLevel.Value, 1);
                            }
                        }

                        // EXIT LOGIC: Position management
                        var position = positions.FirstOrDefault(p => p.Symbol == ticker.Symbol);
                        if (position != null && position.CurrentQuantity != 0)
                        {
                            await ManagePositionExit(position, levels.supports, levels.resistances, currentPrice.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                await Task.Delay(60000);
            }
        }

        private static async Task ManagePositionExit(KucoinPosition position, decimal[] supports,
            decimal[] resistances, decimal currentPrice)
        {
            var isLong = position.CurrentQuantity > 0;
            var exitConditionMet = false;

            if (isLong)
            {
                // Long exit conditions
                var nearestResistance = resistances.OrderBy(r => r)
                    .FirstOrDefault(r => r > position.AverageEntryPrice);
                var nearestSupport = supports.OrderByDescending(s => s)
                    .FirstOrDefault(s => s < currentPrice);

                exitConditionMet = currentPrice >= nearestResistance || currentPrice <= nearestSupport;
            }
            else
            {
                // Short exit conditions
                var nearestSupport = supports.OrderByDescending(s => s)
                    .FirstOrDefault(s => s < position.AverageEntryPrice);
                var nearestResistance = resistances.OrderBy(r => r)
                    .FirstOrDefault(r => r > currentPrice);

                exitConditionMet = currentPrice <= nearestSupport || currentPrice >= nearestResistance;
            }

            if (exitConditionMet)
            {
                await ClosePosition(position.Symbol, position.CurrentQuantity);
                Console.WriteLine($"Closed {position.Symbol} position at {currentPrice}");
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
                var result = await restClient.FuturesApi.Trading.CancelOrderAsync(order.Id);
                if (result.Success)
                {
                    Console.WriteLine($"Cancelled {order.Side} order at {order.Price}");
                }
            }
        }

        private static bool OrderExistsAtLevel(IEnumerable<KucoinFuturesOrder> orders, decimal level, OrderSide side)
        {
            return orders.Any(o =>
                Math.Abs((decimal)(o.Price - level)) < 0.001m &&
                o.Side == side);
        }

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
            var result = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: side,
                type: NewOrderType.Limit,
                leverage: 20,
                quantityInQuoteAsset: (int)quantity,
                price: Math.Round(price, 3),
                timeInForce: TimeInForce.GoodTillCanceled,
                marginMode: FuturesMarginMode.Cross
            );
            if (result.Success)
            {
                Console.WriteLine($"Placed {side} order at {price}");
            }
        }

        private static async Task ClosePosition(string symbol, decimal quantity)
        {
            var side = quantity > 0 ? OrderSide.Sell : OrderSide.Buy;
            var result = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                symbol: symbol,
                side: side,
                type: NewOrderType.Market,
                leverage: 20,
                quantityInQuoteAsset: Math.Abs((int)quantity),
                marginMode: FuturesMarginMode.Cross,
                closeOrder: true
            );

            if (!result.Success)
            {
                Console.WriteLine($"Failed to close position: {result.Error?.Message}");
            }
        }
    }
}