namespace Trader.Binance;

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class BinanceApi
{
    private readonly string? _apiKey;
    private readonly string? _apiSecret;
    
    public BinanceApi()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<BinanceApi>()
            .Build();
        
        _apiKey = config["BinanceApiKey"];
        _apiSecret = config["BinanceApiSecret"];
    }

    public async Task<decimal?> GetUsdtBalance()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"timestamp={timestamp}";
        var signature = Sign(query, _apiSecret);
        var url = $"https://api1.binance.com/sapi/v1/capital/config/getall?{query}&signature={signature}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

        try
        {
            var response = await client.GetStringAsync(url);
            var assets = JsonSerializer.Deserialize<JsonElement>(response);
            var usdtAsset = assets.EnumerateArray().FirstOrDefault(a => a.GetProperty("coin").GetString() == "USDT");

            if (usdtAsset.ValueKind == JsonValueKind.Undefined)
            {
                Console.WriteLine("USDT não encontrado na carteira.");
                return 0;
            }

            var balance = decimal.Parse(usdtAsset.GetProperty("free").GetString());
            return Math.Round(balance, 2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar saldo USDT: {ex.Message}");
            return null;
        }
    }

    public async Task<decimal?> GetBtcBalance()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"timestamp={timestamp}";
        var signature = Sign(query, _apiSecret);
        var url = $"https://api1.binance.com/sapi/v1/capital/config/getall?{query}&signature={signature}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

        try
        {
            var response = await client.GetStringAsync(url);
            var assets = JsonSerializer.Deserialize<JsonElement>(response);
            var btcAsset = assets.EnumerateArray().FirstOrDefault(a => a.GetProperty("coin").GetString() == "BTC");

            if (btcAsset.ValueKind == JsonValueKind.Undefined)
            {
                Console.WriteLine("BTC não encontrado na carteira.");
                return 0;
            }

            var balance = decimal.Parse(btcAsset.GetProperty("free").GetString());
            return Math.Round(balance, 8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar saldo BTC: {ex.Message}");
            return null;
        }
    }

    public async Task<decimal?> GetBitcoinPrice()
    {
        using var client = new HttpClient();
        try
        {
            var response = await client.GetStringAsync("https://api1.binance.com/api/v3/ticker/price?symbol=BTCUSDT");
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            var price = decimal.Parse(json.GetProperty("price").GetString());
            return Math.Round(price, 2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter o preço do BTC: {ex.Message}");
            return null;
        }
    }

    public async Task PlaceMarketBuyOrder(decimal quantity, decimal stepSize, decimal minQty)
    {
        var rawQty = Math.Floor(quantity / stepSize) * stepSize;
        if (rawQty < minQty)
        {
            Console.WriteLine($"❌ Quantidade {rawQty} abaixo do mínimo permitido ({minQty}). Compra cancelada.");
            return;
        }

        var formattedQty = rawQty.ToString("0.######");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"symbol=BTCUSDT&side=BUY&type=MARKET&quantity={formattedQty}&recvWindow=5000&timestamp={timestamp}";
        var signature = Sign(query, _apiSecret);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

        try
        {
            var response = await client.PostAsync($"https://api1.binance.com/api/v3/order?{query}&signature={signature}", null);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("✅ Ordem de COMPRA executada:");
            Console.WriteLine(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao executar ordem de compra: {ex.Message}");
        }
    }
    
    public async Task PlaceMarketSellOrder(decimal quantity, decimal stepSize, decimal minQty)
    {
        var rawQty = Math.Floor(quantity / stepSize) * stepSize;
        if (rawQty < minQty)
        {
            Console.WriteLine($"❌ Quantidade {rawQty} abaixo do mínimo permitido ({minQty}). Venda cancelada.");
            return;
        }

        var formattedQty = rawQty.ToString("0.######");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"symbol=BTCUSDT&side=SELL&type=MARKET&quantity={formattedQty}&recvWindow=5000&timestamp={timestamp}";
        var signature = Sign(query, _apiSecret);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);

        try
        {
            var response = await client.PostAsync($"https://api1.binance.com/api/v3/order?{query}&signature={signature}", null);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("✅ Ordem de VENDA executada:");
            Console.WriteLine(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao executar ordem de venda: {ex.Message}");
        }
    }
    
    public async void ExecuteOrder(string side, decimal price)
    {
        try
        {
            using var httpClient = new HttpClient();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var queryString = $"symbol=BTCUSDT&side={side}&type=MARKET&quantity=0.001&recvWindow=5000&&timestamp={timestamp}";
            var secret = _apiSecret;
            var apiKey = _apiKey;

            var signature = CreateHmacSignature(queryString, secret);
            var finalUrl = $"https://api.binance.com/api/v3/order?{queryString}&signature={signature}";

            var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
            request.Headers.Add("X-MBX-APIKEY", apiKey);

            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ORDEM ENVIADA] {side}: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao executar ordem: {ex.Message}");
        }
    }
    
    public async Task<long> GetServerTime()
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync("https://api.binance.com/api/v3/time");
        var json = JsonSerializer.Deserialize<JsonElement>(response);
        return json.GetProperty("serverTime").GetInt64();
    }
    
    private static string CreateHmacSignature(string data, string? secret)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string Sign(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}