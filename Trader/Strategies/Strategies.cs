using System.Text.Json;

namespace Trader.Strategies;

public class Strategies
{
    private readonly int _movingAveragePeriod;
    private readonly decimal _threshold;
    private readonly Queue<decimal> _priceHistory;

    public Strategies(int movingAveragePeriod = 20, decimal threshold = 0.02m)
    {
        _movingAveragePeriod = movingAveragePeriod;
        _threshold = threshold;
        _priceHistory = new Queue<decimal>();
    }
    
    public void OnNewPrice(decimal currentPrice)
    {
        _priceHistory.Enqueue(currentPrice);
        if (_priceHistory.Count > _movingAveragePeriod)
            _priceHistory.Dequeue();

        if (_priceHistory.Count == _movingAveragePeriod)
        {
            var movingAverage = _priceHistory.Average();
            var deviation = (currentPrice - movingAverage) / movingAverage;

            if (deviation <= -_threshold)
            {
                Console.WriteLine($"[BUY] Price: {currentPrice}, Moving Average: {movingAverage}");
                // Lógica de compra
            }
            else if (deviation >= _threshold)
            {
                Console.WriteLine($"[SELL] Price: {currentPrice}, Moving Average: {movingAverage}");
                // Lógica de venda
            }
            else
            {
                Console.WriteLine($"[HOLD] Price: {currentPrice}, Moving Average: {movingAverage}");
                // Lógica de manter posição
            }
        }
        else
        {
            Console.WriteLine("Aguardando dados suficientes para calcular a média móvel.");
        }
    }
}