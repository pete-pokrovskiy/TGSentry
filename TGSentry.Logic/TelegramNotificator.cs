using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TGSentry.Infra.Contract;
using TGSentry.Logic.Contract;

namespace TGSentry.Logic
{
    public class TelegramNotificator : INotificator, IScoped
    {
        private readonly ILogger<TelegramNotificator> _logger;
        private readonly TelegramSettings _settings;

        public TelegramNotificator(ILogger<TelegramNotificator> logger, IOptions<TelegramSettings> settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task SendMessage(string message)
        {
            _logger.LogInformation("Sending message..");
            
            using var httpClient = new HttpClient();

            var baseUrl = string.Format(_settings.ApiUrl, _settings.BotApiToken, _settings.ChatId);
            
            var urlWithMessage = $"{baseUrl}&text={HttpUtility.UrlEncode(message, Encoding.UTF8)}";
            
            var response = await httpClient.GetAsync(new Uri(urlWithMessage));

            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Got response: {content}", content);
        }
    }
}