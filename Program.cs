using CryptoExchange.Net.CommonObjects;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using System;

namespace Guap250494
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var mode = OrderSide.Buy;
            while (true)
            {
                Task.Delay(1000 * 10).Wait();
                var restClient = new KucoinRestClient();
                restClient.SetApiCredentials(new Kucoin.Net.Objects.KucoinApiCredentials
                    ("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

                var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
                var loss = symbolList.Data.Sum(x => x.UnrealizedPnl);
                Console.WriteLine(loss);
                foreach (var symbol in symbolList.Data.Where(x => x.UnrealizedPnl > .02M ||x.UnrealizedPnl < - 0.01M))
                {
                    if (symbol != null && symbol.IsOpen)
                    {
                        var z = await restClient.FuturesApi.Trading.PlaceOrderAsync
                        (symbol.Symbol, Kucoin.Net.Enums.OrderSide.Buy, Kucoin.Net.Enums.NewOrderType.Market, 0, closeOrder: true, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);
                        Console.WriteLine("Closed " + symbol.Symbol + " - " + symbol.UnrealizedPnl);

                        if(symbol.UnrealizedPnl < -0.01M && symbol.CurrentQuantity > 0)
                        {
                            mode = OrderSide.Sell;
                        }
                        if (symbol.UnrealizedPnl < -0.01M && symbol.CurrentQuantity > 0)
                        {
                            mode = OrderSide.Sell;
                        }

                        continue;
                    }

                }

                foreach (var symbol in symbolList.Data.Where(x => x.UnrealizedPnlPercentage < -.1M))
                {
                    if (symbol != null && symbol.IsOpen)
                    {
                        var result = await restClient.FuturesApi.Trading.PlaceOrderAsync
                                (symbol.Symbol, mode, Kucoin.Net.Enums.NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);
                        continue;
                    }
                }

                if (symbolList.Data.Count() < 50)
                {
                    var tickerList = await restClient.FuturesApi.ExchangeData.GetTickersAsync();
                    {
                        var random = new Random();
                        int randomIndex = random.Next(tickerList.Data.Count());
                        var randomSymbol = tickerList.Data.ElementAt(randomIndex).Symbol;
                        {
                            var getPositions = await restClient.FuturesApi.Account.GetPositionAsync(randomSymbol);
                            if (getPositions != null && getPositions.Data.IsOpen)
                            {
                                continue;
                            }

                            var result = await restClient.FuturesApi.Trading.PlaceOrderAsync
                                (randomSymbol, mode, Kucoin.Net.Enums.NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);

                            if (result.Success)
                            {
                                Console.WriteLine(randomSymbol);
                            }

                        }
                    }
                }
            }
        }
    }
}
