using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// The main CAM integration workflow: send the operation's tool and cut to HSMAdvisor, let the user
    /// fine-tune it in the full HSMAdvisor calculator, and apply the result when they click OK.
    /// </summary>
    public sealed class EditCalculationDialogSample : ISample
    {
        public string Title => "Dialog: edit a calculation in the HSMAdvisor calculator";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            var calc = SampleData.EndMill();

            var request = new EditCalculationRequest
            {
                Calculation = calc,
                Prompt = "Adjust the cut, then click OK to send it back.", // shown next to OK/Cancel
                CalculateFirst = true, // recalculate before showing, instead of showing the values as sent
            };

            // HSMAdvisor's dialog is owned by the window you pass, so it stays in front of it.
            // In a real host pass your main window's handle (e.g. form.Handle.ToInt64());
            // a console app has none, so 0 opens an unowned dialog.
            long ownerWindow = 0;

            Console.WriteLine("Waiting for the HSMAdvisor calculator (Ctrl+C closes it)...");
            var response = await client.RequestAsync<CalculationPayload>(MessageTypes.EditCalculation, request, ownerWindow, ct);

            // RequestAsync returns null when the user clicked Cancel in HSMAdvisor
            if (response == null)
            {
                Console.WriteLine("The user cancelled. Keep the operation as it was.");
                return;
            }

            // Apply these values to your CAM operation. Fields you sent (tool name, number, comment,
            // CAM info) come back unchanged unless the user edited them.
            var cut = response.Calculation.Cut;
            Console.WriteLine($"Tool:      {response.Calculation.Tool.Comment}");
            Console.WriteLine($"DOC x WOC: {cut.Doc:0.####} x {cut.Woc:0.####} {(cut.Doc_m ? "mm" : "in")}");
            Console.WriteLine($"RPM:       {cut.Rpm:N0}");
            Console.WriteLine($"Feed:      {cut.Feed:0.##} {(cut.Feed_m ? "mm/min" : "in/min")}");
        }
    }
}
