using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Calculates a cut for a specific machine profile. HSMAdvisor keeps RPM, feed and power within
    /// that machine's limits, so the same cut can give different results on different machines.
    /// </summary>
    public sealed class MachineProfileSample : ISample
    {
        public string Title => "Calculate: apply a machine profile";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // Without Cut.Machine_id HSMAdvisor uses the user's default machine.
            var defaultCalc = await CalculateAsync(client, null, ct);
            string defaultMachine = defaultCalc.Results?.MachineName;
            Console.WriteLine($"Default machine{(string.IsNullOrEmpty(defaultMachine) ? "" : $" ({defaultMachine})")}: {defaultCalc.Cut.Rpm:N0} RPM, {defaultCalc.Cut.Feed:0.##} in/min");

            // Pick another machine from the reference data. HSMAdvisor's list starts with a
            // "--none--" placeholder, which is skipped here, and lathes are skipped for an end mill.
            // A real integration would let the user choose (sample 8) or store the machine GUID.
            var data = await client.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData, null, 0, ct);
            var machine = data.Machines.FirstOrDefault(m => !m.Name.StartsWith("--")
                && m.Name.IndexOf("lathe", StringComparison.OrdinalIgnoreCase) < 0
                && m.Name != defaultMachine);
            if (machine == null)
            {
                Console.WriteLine("Only one machine is defined. Add another one in HSMAdvisor (or with sample 8) and run this again.");
                return;
            }

            // The machine is identified by its GUID
            var machineCalc = await CalculateAsync(client, machine.Guid, ct);
            Console.WriteLine($"{machine.Name}: {machineCalc.Cut.Rpm:N0} RPM, {machineCalc.Cut.Feed:0.##} in/min");
        }

        private static async Task<CalculationData> CalculateAsync(HSMAdvisorClient client, string machineGuid, CancellationToken ct)
        {
            // A small tool runs at high RPM, so a machine's spindle limit is likely to show
            var calc = SampleData.EndMill(diameter: 0.125, flutes: 3, doc: 0.125, woc: 0.0125);
            calc.Cut.Machine_id = machineGuid;

            var response = await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate,
                new CalculateRequest { Calculation = calc, PresetSpeeds = true }, 0, ct);
            return response.Calculation;
        }
    }
}
