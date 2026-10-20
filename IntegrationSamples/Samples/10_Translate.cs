using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Looks up text in the user's HSMAdvisor language, so your own dialogs can use the same wording
    /// and language as HSMAdvisor.
    /// </summary>
    public sealed class TranslateSample : ISample
    {
        public string Title => "Translate: text in the user's HSMAdvisor language";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // Keys are HSMAdvisor translation keys
            var request = new TranslateRequest();
            request.Keys.Add("DLG_Title88");
            request.Keys.Add("DLG_Text88");

            var response = await client.RequestAsync<TranslateResponse>(MessageTypes.Translate, request, 0, ct);

            foreach (var key in request.Keys)
            {
                string text;
                Console.WriteLine($"{key}: {(response.Strings.TryGetValue(key, out text) ? text : "(no translation)")}");
            }
        }
    }
}
