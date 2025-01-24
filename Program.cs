using Kucoin.Net.Clients;
using System;

namespace Guap250494
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            bool canBuy = true;
            while (true)
            {
                Console.Clear();
                Task.Delay(1000 * 10).Wait();
                var restClient = new KucoinRestClient();
                restClient.SetApiCredentials(new Kucoin.Net.Objects.KucoinApiCredentials
                    ("6792c43bc0a1b1000135cb65", "25ab9c72-17e6-4951-b7a8-6e2fce9c3026", "test1234"));

                var symbolList = await restClient.FuturesApi.Account.GetPositionsAsync();
                foreach (var symbol in symbolList.Data.Select(x => x.Symbol))
                {
                    var getPositions = await restClient.FuturesApi.Account.GetPositionAsync(symbol);
                    if (getPositions != null && getPositions.Data.IsOpen && getPositions.Data.UnrealizedPnl > .003M)
                    {
                        var z = await restClient.FuturesApi.Trading.PlaceOrderAsync
                        (symbol, Kucoin.Net.Enums.OrderSide.Buy, Kucoin.Net.Enums.NewOrderType.Market, 0, closeOrder: true, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);
                        Console.WriteLine("Closed " + symbol + " - " + getPositions.Data.UnrealizedPnl);
                        continue;
                    }
                }
                if (canBuy)
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
                                (randomSymbol, Kucoin.Net.Enums.OrderSide.Buy, Kucoin.Net.Enums.NewOrderType.Market, 25, quantityInQuoteAsset: 1, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);

                            if (!result.Success)
                            {
                                result = await restClient.FuturesApi.Trading.PlaceOrderAsync
                                                    (randomSymbol, Kucoin.Net.Enums.OrderSide.Buy, Kucoin.Net.Enums.NewOrderType.Market, 25, quantityInQuoteAsset: 2, marginMode: Kucoin.Net.Enums.FuturesMarginMode.Cross);
                            }

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
