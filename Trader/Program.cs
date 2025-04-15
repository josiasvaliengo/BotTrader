using System;
using System.Threading.Tasks;
using Trader.Binance;
using Trader.Strategies;

class Program
{
    static async Task Main(string[] args)
    {
        // Inicia o loop em segundo plano
        _ = StartLoopAsync();

        // Evita que a aplicação termine imediatamente
        await Task.Delay(Timeout.Infinite);
    }

    static async Task StartLoopAsync()
    {
        while (true)
        {
            new Strategies().OnNewPrice(0.0m);
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }
}
