using CryptoExchange.Net.Interfaces;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Futures;

namespace Guap250494
{
    internal class Program
    {
        private static KucoinRestClient? restClient;

        static async Task Main(string[] args)
        {
            restClient = new KucoinRestClient();
            var mode = OrderSide.Buy;
            restClient.SetApiCredentials(new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));
            List<string> ErrorList = new List<string>();

            while (true)
            {
                var acountInfo = await restClient.FuturesApi.Account.GetAccountOverviewAsync("USDT");
                Console.WriteLine(acountInfo.Data.MarginBalance + " - " + acountInfo.Data.UnrealizedPnl + " - " + acountInfo.Data.RiskRatio);
                Thread.Sleep(1000);
                //continue;

                bool canCreate = acountInfo.Data.RiskRatio < .15M;

                var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
                if (!symbolList.Success)
                {
                    Console.WriteLine("Failed to get positions: " + symbolList.Error);
                    continue;
                }

                KucoinPosition? kucoinPosition = symbolList.Data.Where(x => x.UnrealizedPnl > 0.01M).MaxBy(x => x.UnrealizedPnl);

                if (kucoinPosition != null)
                {
                    var closeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                        kucoinPosition.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);

                    if (closeOrderResult.Success)
                    {
                        Console.WriteLine("Closed " + kucoinPosition.Symbol + " - " + kucoinPosition.UnrealizedPnl);
                    }
                    else
                    {
                        Console.WriteLine("Failed to close " + kucoinPosition.Symbol + ": " + closeOrderResult.Error);
                    }
                }

                if (canCreate)
                {
                    var symbolList1 = await restClient.FuturesApi.Account.GetPositionsAsync();
                    foreach (var symbol in symbolList1.Data)
                    {
                        if (symbol != null && symbol.CurrentQuantity < 0 && symbol.UnrealizedRoePercentage < -1m)
                        {
                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                                    symbol.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                        }

                    }

                    foreach (var symbol in symbolList1.Data)
                    {
                        if (symbol != null && symbol.CurrentQuantity > 0 && symbol.UnrealizedRoePercentage < -1m)
                        {
                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                                    symbol.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
                        }

                    }
                }

                if (canCreate)
                {
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    if (!tickerList.Success)
                    {
                        Console.WriteLine("Failed to get tickers: " + tickerList.Error);
                        continue;
                    }

                    foreach (var randomSymbol in tickerList.Data.Where(x => !ErrorList.Contains(x.Symbol)).OrderByDescending(x => x.Symbol))
                    {
                        if (symbolList.Data.Any(x => x.Symbol == randomSymbol.Symbol))
                        {
                            continue;
                        }

                        var ticker = await restClient.FuturesApi.ExchangeData.GetKlinesAsync(randomSymbol.Symbol, FuturesKlineInterval.OneHour, DateTime.UtcNow.AddHours(-1));
                        var current = ticker.Data.LastOrDefault();

                        if (current?.OpenPrice < current?.ClosePrice)
                        {
                            mode = OrderSide.Buy;
                        }

                        if (current?.OpenPrice > current?.ClosePrice)
                        {
                            mode = OrderSide.Sell;
                        }

                        var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                            randomSymbol.Symbol, mode, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);

                        if (placeOrderResult.Success)
                        {
                            Console.WriteLine("Opened position on " + randomSymbol.Symbol);
                            break;
                        }
                        else
                        {
                            placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                            randomSymbol.Symbol, mode, NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: FuturesMarginMode.Cross);
                            if (!placeOrderResult.Success)
                            {
                                placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                                randomSymbol.Symbol, mode, NewOrderType.Market, 25, quantityInQuoteAsset: 3, marginMode: FuturesMarginMode.Cross);
                                if (!placeOrderResult.Success)
                                {
                                    placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
                                    randomSymbol.Symbol, mode, NewOrderType.Market, 25, quantityInQuoteAsset: 4, marginMode: FuturesMarginMode.Cross);
                                    if (!placeOrderResult.Success)
                                    {
                                        ErrorList.Add(randomSymbol.Symbol);
                                        Console.WriteLine("Failed to open position on " + randomSymbol.Symbol + " : " + placeOrderResult.Error);
                                    }
                                }
                            }
                            if (placeOrderResult.Success)
                            {
                                Console.WriteLine("Opened position on " + randomSymbol.Symbol);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}



//var symbolList1 = await restClient.FuturesApi.Account.GetPositionsAsync();
//foreach (var symbol in symbolList1.Data)
//{
//    if (symbol != null && symbol.CurrentQuantity < 0 && symbol.UnrealizedRoePercentage < -2m)
//    {
//        var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                symbol.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
//        Console.WriteLine(placeOrderResult.Success + placeOrderResult?.Error?.Message);
//    }

//}