using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PDOE.Infrastructure.Notifications;


public class HttpNotificationSender(HttpClient http) : INotificationSender
{
    public async Task<NotificationSendResult> EnvoyerAsync(
         string canal, string destinataire, string sujet, string corps, CancellationToken cancellationToken)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                "api/v1/envoyer",
                new
                {
                    to = destinataire,
                    subject = sujet,
                    emailBody = corps,
                    copy = "",
                    attachment = ""
                },
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorCode = await response.Content.ReadAsStringAsync(cancellationToken);
                return new NotificationSendResult(false, null, errorCode);
            }

            var body = await response.Content.ReadFromJsonAsync<MessagerieResponse>(cancellationToken);
            return new NotificationSendResult(true, body?.MessageId, null);
        }

        catch (HttpRequestException ex)
        {
            return new NotificationSendResult(false, null, ex.Message);
        }


    }
    private record MessagerieResponse(string MessageId);
}
