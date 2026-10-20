using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Saves a calculated cut into the user's HSMAdvisor tool library, then finds the tool again by
    /// name and loads it with all its cuts. This is how an integration links a CAM tool to a library tool.
    /// </summary>
    public sealed class ToolLibrarySample : ISample
    {
        private const string ToolName = "Integration sample 3/8 3FL";

        public string Title => "Tool library: save a cut, find and load the tool";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // 1. Calculate the cut to save
            var calc = SampleData.EndMill(diameter: 0.375, flutes: 3, doc: 0.375, woc: 0.05);
            calc.Tool.Comment = ToolName;        // the tool's name in the library
            calc.Cut.Comment = "Sample contour"; // the cut's name
            calc.Cut.CAM.Enabled = true;         // mark the cut for CAM use...
            calc.Cut.CAM.ToolPathType = "Contour"; // ...for this toolpath type (see SelectCut's filter)
            var calculated = (await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate,
                new CalculateRequest { Calculation = calc, PresetSpeeds = true }, 0, ct)).Calculation;

            // 2. AddCut opens HSMAdvisor's "Add cut" dialog, where the user chooses the library and
            //    confirms. SaveToolIfNew also saves the tool when the library does not have it yet.
            Console.WriteLine("Confirm the Add Cut dialog in HSMAdvisor...");
            var saved = await client.RequestAsync<AddCutResponse>(MessageTypes.AddCut,
                new AddCutRequest { Calculation = calculated, SaveToolIfNew = true }, 0, ct);
            if (saved == null)
            {
                Console.WriteLine("The user cancelled; nothing was saved.");
                return;
            }
            Console.WriteLine($"Saved tool {saved.ToolGuid}, cut {saved.CutGuid}");

            // 3. Find library tools by their exact name. Store the tool GUID with your CAM tool to
            //    skip this lookup next time.
            var found = await client.RequestAsync<FindToolsResponse>(MessageTypes.FindTools,
                new FindToolsRequest { Comment = ToolName }, 0, ct);
            foreach (var summary in found.Tools)
            {
                Console.WriteLine($"Found: {summary.Name} in library \"{summary.Library}\" ({summary.Guid})");
            }
            if (found.Tools.Count == 0)
            {
                return;
            }

            // 4. Load the full tool record with all its cuts
            var tool = (await client.RequestAsync<GetToolResponse>(MessageTypes.GetTool,
                new GetToolRequest { Guid = found.Tools[0].Guid }, 0, ct)).Tool;
            Console.WriteLine($"{tool.Description}: {tool.Cuts.Count} cut(s)");
            foreach (var cut in tool.Cuts)
            {
                Console.WriteLine($"  {cut.Comment}: {cut.Rpm:N0} RPM, {cut.Feed:0.##} {(cut.Feed_m ? "mm/min" : "in/min")}");
            }
        }
    }
}
