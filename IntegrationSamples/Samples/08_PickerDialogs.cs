using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// HSMAdvisor's pickers: let the user choose a workpiece material, a machine profile, or a saved
    /// tool and cut from their tool library. Each dialog returns null when the user cancels it.
    /// Pass your main window's handle instead of 0 in a real host (see sample 7).
    /// </summary>
    public sealed class PickerDialogsSample : ISample
    {
        public string Title => "Dialog: pick a material, machine and library cut";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // Workpiece material -> use as calc.Cut.Material_id
            Console.WriteLine("Pick a workpiece material in HSMAdvisor...");
            var material = await client.RequestAsync<SelectMaterialResponse>(MessageTypes.SelectMaterial, null, 0, ct);
            if (material?.Material != null)
            {
                Console.WriteLine($"  Material: {material.Material.Name} (id {material.Material.Id})");
            }

            // Machine profile -> use as calc.Cut.Machine_id. The user can also create or edit
            // machines in this dialog, so reload any cached reference data afterwards.
            Console.WriteLine("Pick a machine in HSMAdvisor...");
            var machine = await client.RequestAsync<SelectMachineResponse>(MessageTypes.SelectMachine, null, 0, ct);
            if (machine?.Machine != null)
            {
                Console.WriteLine($"  Machine: {machine.Machine.Name} ({machine.Machine.Guid})");
            }

            // Saved tool and cut from the tool library, returned as a complete calculation,
            // e.g. to load proven cutting data into a new CAM operation.
            Console.WriteLine("Pick a tool and cut from the HSMAdvisor library...");
            var request = new SelectCutRequest
            {
                Title = "Select a tool and cut",
                CamOnly = false,          // true: only cuts marked for CAM, filtered by ToolPathType
                ToolPathType = "Contour", // the CAM toolpath type stored with the cut (Cut.CAM.ToolPathType)
            };
            var selected = await client.RequestAsync<CalculationPayload>(MessageTypes.SelectCut, request, 0, ct);
            if (selected?.Calculation != null)
            {
                var calc = selected.Calculation;
                Console.WriteLine($"  Tool: {calc.Tool.Description} ({calc.Tool.Guid})");
                Console.WriteLine($"  Cut:  {calc.Cut.Comment} ({calc.Cut.Guid}): {calc.Cut.Rpm:N0} RPM, {calc.Cut.Feed:0.##} {(calc.Cut.Feed_m ? "mm/min" : "in/min")}");
            }
        }
    }
}
