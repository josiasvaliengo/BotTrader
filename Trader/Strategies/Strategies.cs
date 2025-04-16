using Trader.Binance;
using Trader.Notifications;

namespace Trader.Strategies;

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Strategies
{
    private readonly int _emaShortPeriod = 9;
    private readonly int _emaLongPeriod = 21;
    private readonly int _rsiPeriod = 14;
    private readonly List<decimal> _priceHistory = new();
    private bool _positionOpen = false;
    private decimal _entryPrice = 0;
    private decimal _stopLossPrice = 0;
    private decimal _takeProfitPrice = 0;
    private decimal _trailingStopPercentage = 0.01m; // 1%

    public async Task OnNewPrice(decimal price)
    {
        _priceHistory.Add(price);
        if (_priceHistory.Count < _emaLongPeriod)
        {
            Console.WriteLine($"Coletando dados para EMAs... ({_priceHistory.Count}/{_emaLongPeriod})");
            return;
        }

        var emaShort = CalculateEMA(_priceHistory, _emaShortPeriod);
        var emaLong = CalculateEMA(_priceHistory, _emaLongPeriod);
        var rsi = CalculateRSI(_priceHistory, _rsiPeriod);

        Console.WriteLine($"EMA9: {emaShort:F2}, EMA21: {emaLong:F2}, RSI: {rsi:F2}");

        if (emaShort > emaLong && rsi < 30)
        {
            if (!_positionOpen)
            {
                _entryPrice = price;
                _stopLossPrice = price * (1 - _trailingStopPercentage); // 1% abaixo
                _takeProfitPrice = price * 1.02m; // 2% acima
                Console.WriteLine($"[COMPRA] Entrada: {_entryPrice}, SL: {_stopLossPrice}, TP: {_takeProfitPrice} " +
                                  $"- {DateTime.UtcNow}");
                await new Telegram().SendMessageAsync($"[COMPRA] Entrada: {_entryPrice}, SL: {_stopLossPrice}, TP: {_takeProfitPrice} - {DateTime.UtcNow}");
                new BinanceApi().ExecuteOrder("BUY", price);
                _positionOpen = true;
            }
        }
        else
        {
            if (_positionOpen)
            {
                if (price > _entryPrice && price * (1 - _trailingStopPercentage) > _stopLossPrice)
                {
                    _stopLossPrice = price * (1 - _trailingStopPercentage);
                    Console.WriteLine($"[TRAILING STOP] Novo SL ajustado para {_stopLossPrice:F2}");
                    await new Telegram().SendMessageAsync($"[TRAILING STOP] Novo SL ajustado para {_stopLossPrice:F2}");
                }

                if (price <= _stopLossPrice)
                {
                    Console.WriteLine($"[STOP-LOSS] Preço atingiu {_stopLossPrice}. Vendendo. - {DateTime.UtcNow}");
                    await new Telegram().SendMessageAsync($"[STOP-LOSS] Preço atingiu {_stopLossPrice}. Vendendo. - {DateTime.UtcNow}");
                    new BinanceApi().ExecuteOrder("SELL", price);
                    _positionOpen = false;
                }
                else if (price >= _takeProfitPrice)
                {
                    Console.WriteLine($"[TAKE-PROFIT] Preço atingiu {_takeProfitPrice}. Vendendo. - {DateTime.UtcNow}");
                    await new Telegram().SendMessageAsync($"[TAKE-PROFIT] Preço atingiu {_takeProfitPrice}. Vendendo. - {DateTime.UtcNow}");
                    new BinanceApi().ExecuteOrder("SELL", price);
                    _positionOpen = false;
                }
            }
            Console.WriteLine($"[HOLD] Aguardando melhor oportunidade. - {DateTime.UtcNow}");
        }
    }

    public async Task<List<decimal>> FetchLastCandlesAsync(int limit = 30)
    {
        try
        {
            using var httpClient = new HttpClient();
            var url = $"https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval=1m&limit={limit}";
            var response = await httpClient.GetStringAsync(url);
            var json = JsonSerializer.Deserialize<JsonElement>(response);

            var prices = new List<decimal>();
            foreach (var candle in json.EnumerateArray())
            {
                // índice 4 = preço de fechamento
                prices.Add(decimal.Parse(candle[4].ToString()));
            }

            Console.WriteLine($"Carregados {prices.Count} candles históricos.");
            return prices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar candles: {ex.Message}");
            return new List<decimal>();
        }
    }

    private static decimal CalculateEMA(List<decimal> prices, int period)
    {
        if (prices.Count < period)
            return 0;

        var k = 2m / (period + 1);
        var ema = prices.Take(period).Average();

        for (int i = period; i < prices.Count; i++)
        {
            ema = prices[i] * k + ema * (1 - k);
        }

        return Math.Round(ema, 2);
    }

    private static decimal CalculateRSI(List<decimal> prices, int period)
    {
        if (prices.Count < period + 1)
            return 0;

        decimal gain = 0, loss = 0;

        for (int i = prices.Count - period; i < prices.Count - 1; i++)
        {
            var change = prices[i + 1] - prices[i];
            if (change > 0)
                gain += change;
            else
                loss -= change;
        }

        if (loss == 0) return 100;
        var rs = gain / loss;
        return Math.Round(100 - (100 / (1 + rs)), 2);
    }
}