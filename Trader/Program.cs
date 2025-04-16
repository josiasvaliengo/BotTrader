using Trader.Binance;
using Trader.Strategies;

class Program
{
    static async Task Main(string[] args)
    {
        var strategy = new Strategies();
        var historicalPrices = await strategy.FetchLastCandlesAsync(30);
        foreach (var price in historicalPrices)
        {
            strategy.OnNewPrice(price);
        }
        _ = StartLoopAsync(strategy);

        await Task.Delay(Timeout.Infinite);
    }

    static async Task StartLoopAsync(Strategies strategy)
    {
        var api = new BinanceApi();
        while (true)
        {
            var serverTime = await api.GetServerTime();
            var delay = 5000 - (serverTime % 5000); // tempo restante até o próximo candle de 1min
            await Task.Delay((int)delay + 1000); // espera até o próximo fechamento + 1s de segurança

            var btcPrice = await api.GetBitcoinPrice();
            strategy.OnNewPrice((decimal)btcPrice!);
        }
    }
}
