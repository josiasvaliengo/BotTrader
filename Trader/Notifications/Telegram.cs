using Microsoft.Extensions.Configuration;

namespace Trader.Notifications;

public class Telegram
{
    private readonly string _botToken;
    private readonly string _chatId;

    public Telegram()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Telegram>()
            .Build();
        
        _botToken = config["TelegramToken"]!;
        _chatId = config["TelegramChatId"]!;
    }

    public async Task SendMessageAsync(string message)
    {
        using var client = new HttpClient();
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var data = new Dictionary<string, string>
        {
            { "chat_id", _chatId },
            { "text", message }
        };
        var content = new FormUrlEncodedContent(data);
        await client.PostAsync(url, content);
    }
}