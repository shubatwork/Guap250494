//using CryptoExchange.Net.CommonObjects;
//using Kucoin.Net.Clients;
//using Kucoin.Net.Enums;
//using Kucoin.Net.Objects;
//using Kucoin.Net.Objects.Models.Futures;

//namespace Guap250494
//{
//    internal class Program
//    {
//        //private static KucoinRestClient? restClient;

//        static async Task Main(string[] args)
//        {
//            restClient = new KucoinRestClient();
//            restClient.SetApiCredentials(new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

//            while (true)
//            {
//                await Task.Delay(1000);

//                try
//                {
//                    var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
//                    if (!symbolList.Success)
//                    {
//                        Console.WriteLine("Failed to get positions: " + symbolList.Error);
//                        continue;
//                    }

//                    KucoinPosition? kucoinPosition = null;

//                    var pnl = symbolList.Data.Sum(x => x.UnrealizedPnl);

//                    kucoinPosition = symbolList.Data.Where(x => x.UnrealizedPnl > 0.02M || x.UnrealizedPnl < -0.1M).MaxBy(x => x.UnrealizedPnl);

//                    if (kucoinPosition != null)
//                    {
//                        var closeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                            kucoinPosition.Symbol, OrderSide.Buy, NewOrderType.Market, 0, closeOrder: true, marginMode: FuturesMarginMode.Cross);

//                        if (closeOrderResult.Success)
//                        {
//                            Console.WriteLine("Closed " + kucoinPosition.Symbol + " - " + kucoinPosition.UnrealizedPnl);
//                        }
//                        else
//                        {
//                            Console.WriteLine("Failed to close " + kucoinPosition.Symbol + ": " + closeOrderResult.Error);
//                        }
//                    }

//                    var increasePos = symbolList.Data.Where(x => x.UnrealizedRoePercentage < -2M).MinBy(x => x.UnrealizedRoePercentage);
//                    if (increasePos != null)
//                    {
//                        if (increasePos.CurrentQuantity > 0)
//                        {
//                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                                increasePos.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
//                        }
//                        else
//                        {
//                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                                increasePos.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);
//                        }
//                    }
//                    var sunLong = symbolList.Data.Where(x => x.CurrentQuantity > 0).Sum(x => x.UnrealizedPnl);
//                    var sunShort = symbolList.Data.Where(x => x.CurrentQuantity < 0).Sum(x => x.UnrealizedPnl);


//                    var count = 50;
//                    if (symbolList.Data.Count(x => x.CurrentQuantity > 0) < count && sunLong >= sunShort)
//                    {
//                        var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
//                        if (!tickerList.Success)
//                        {
//                            Console.WriteLine("Failed to get tickers: " + tickerList.Error);
//                            continue;
//                        }

//                        foreach (var randomSymbol in tickerList.Data.OrderByDescending(x => x.Symbol))
//                        {
//                            if (symbolList.Data.Any(x => x.Symbol == randomSymbol.Symbol))
//                            {
//                                continue;
//                            }

//                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                                randomSymbol.Symbol, OrderSide.Buy, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);

//                            if (placeOrderResult.Success)
//                            {
//                                Console.WriteLine("Opened position on " + randomSymbol.Symbol);
//                                break;
//                            }
//                            else
//                            {
//                                Console.WriteLine("Failed to open position on " + randomSymbol.Symbol + ": " + placeOrderResult.Error);
//                            }
//                        }
//                    }

//                    if (symbolList.Data.Count(x => x.CurrentQuantity < 0) < count && sunShort > sunLong)
//                    {
//                        var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
//                        if (!tickerList.Success)
//                        {
//                            Console.WriteLine("Failed to get tickers: " + tickerList.Error);
//                            continue;
//                        }

//                        foreach (var randomSymbol in tickerList.Data.OrderBy(x => x.Symbol))
//                        {
//                            if (symbolList.Data.Any(x => x.Symbol == randomSymbol.Symbol))
//                            {
//                                continue;
//                            }

//                            var placeOrderResult = await restClient.FuturesApi.Trading.PlaceOrderAsync(
//                                randomSymbol.Symbol, OrderSide.Sell, NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: FuturesMarginMode.Cross);

//                            if (placeOrderResult.Success)
//                            {
//                                Console.WriteLine("Opened position on " + randomSymbol.Symbol);
//                                break;
//                            }
//                            else
//                            {
//                                Console.WriteLine("Failed to open position on " + randomSymbol.Symbol + ": " + placeOrderResult.Error);
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("An error occurred: " + ex.Message);
//                }
//            }
//        }
//    }
//}

