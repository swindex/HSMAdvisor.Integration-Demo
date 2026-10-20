using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// The non-happy paths an integration has to handle:
    ///   * cancelling a request from code, which closes the HSMAdvisor dialog it opened
    ///   * "error" responses, which RequestAsync throws as IntegrationException with a Code
    ///   * "busy": only one interactive (dialog) request runs at a time
    /// </summary>
    public sealed class CancellationAndErrorsSample : ISample
    {
        public string Title => "Cancellation, error and busy responses";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            await CancelDialogFromCodeAsync(client, ct);
            await HandleErrorAsync(client, ct);
            await HandleBusyAsync(client, ct);
        }

        /// <summary>
        /// Cancelling the token closes the pipe connection; HSMAdvisor then treats the request as
        /// cancelled and closes its dialog. Use this when your application closes, or when the user
        /// cancels your own "waiting for HSMAdvisor" UI.
        /// </summary>
        private static async Task CancelDialogFromCodeAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                Console.WriteLine("Opening the calculator; it closes by itself in 5 seconds...");
                try
                {
                    var request = new EditCalculationRequest { Calculation = SampleData.EndMill(), Prompt = "This dialog closes in 5 seconds." };
                    await client.RequestAsync<CalculationPayload>(MessageTypes.EditCalculation, request, 0, timeout.Token);
                    Console.WriteLine("  The user finished the dialog first.");
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    Console.WriteLine("  Request cancelled from code; HSMAdvisor closed its dialog.");
                }
            }
        }

        /// <summary>RequestAsync throws IntegrationException for "error" responses.</summary>
        private static async Task HandleErrorAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            try
            {
                await client.RequestAsync<GetToolResponse>(MessageTypes.GetTool,
                    new GetToolRequest { Guid = Guid.NewGuid().ToString() }, 0, ct);
            }
            catch (IntegrationException ex) when (ex.Code == ErrorCodes.NotFound)
            {
                Console.WriteLine($"GetTool with an unknown GUID -> IntegrationException [{ex.Code}] {ex.Message}");
            }

            // SendAsync does not throw for error/busy responses: it returns the raw response envelope,
            // whose Status is ok, cancelled, error or busy.
            IntegrationResponse response = await client.SendAsync("NoSuchMessage", null, 0, ct);
            Console.WriteLine($"Unknown message type -> status '{response.Status}', code '{response.Error?.Code}'");
        }

        /// <summary>A second interactive request while a dialog is open is answered with "busy".</summary>
        private static async Task HandleBusyAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            using (var first = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                // Open a dialog and leave it open
                Task<IntegrationResponse> dialog = client.SendAsync(MessageTypes.SelectMaterial, null, 0, first.Token);
                await Task.Delay(1500, ct);

                IntegrationResponse second = await client.SendAsync(MessageTypes.SelectMachine, null, 0, ct);
                Console.WriteLine($"Second dialog while one is open -> status '{second.Status}'");

                // Close the first dialog again
                first.Cancel();
                try
                {
                    await dialog;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                }
            }
        }
    }
}
