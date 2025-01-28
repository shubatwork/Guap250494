using CryptoExchange.Net.CommonObjects;
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
            restClient.SetApiCredentials(new KucoinApiCredentials("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

            var mode = OrderSide.Buy;
            while (true)
            {
                await Task.Delay(1000);

                var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
                if (!symbolList.Success)
                {
                    Console.WriteLine("Failed to get positions: " + symbolList.Error);
                    continue;
                }
                
                KucoinPosition? kucoinPosition = null;

                var pnl = symbolList.Data.Sum(x => x.UnrealizedPnl);
                if (pnl > 1M)
                {
                    kucoinPosition = symbolList.Data.MaxBy(x => x.UnrealizedPnl);
                }
                if(pnl < -2M)
                {
                    kucoinPosition = symbolList.Data.MinBy(x => x.UnrealizedPnl);
                }

                if(kucoinPosition != null)
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
                
                var count = 100;
                if (symbolList.Data.Count(x => x.UnrealizedPnl > -0.01M) < count)
                {
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    if (!tickerList.Success)
                    {
                        Console.WriteLine("Failed to get tickers: " + tickerList.Error);
                        continue;
                    }
                     
                    foreach (var randomSymbol in tickerList.Data.OrderByDescending(x => x.Symbol))
                    {
                        if(symbolList.Data.Any(x => x.Symbol == randomSymbol.Symbol))
                        {
                            continue;
                        }
                        var getPositionResult = await restClient.FuturesApi.Account.GetPositionAsync(randomSymbol.Symbol);
                        if (getPositionResult.Success && getPositionResult.Data.IsOpen)
                        {
                            continue;
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
                            Console.WriteLine("Failed to open position on " + randomSymbol.Symbol + ": " + placeOrderResult.Error);
                        }
                    }
                }
            }
        }
    }
}

