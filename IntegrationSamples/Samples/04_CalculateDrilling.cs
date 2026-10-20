using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// The same Calculate message covers every tool family: the tool type decides which calculator
    /// HSMAdvisor uses. This sample drills a hole and then cuts a full-width slot.
    /// </summary>
    public sealed class CalculateDrillingSample : ISample
    {
        public string Title => "Calculate: drilling and slotting";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // Drilling: 1/4 in carbide drill, 1 in deep hole, 1/4 in pecks
            var drill = SampleData.Drill(diameter: 0.25, depth: 1.0, peck: 0.25);
            var drilled = (await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate,
                new CalculateRequest { Calculation = drill, PresetSpeeds = true }, 0, ct)).Calculation;

            Console.WriteLine("Drilling:");
            Console.WriteLine($"  RPM:             {drilled.Cut.Rpm:N0}");
            Console.WriteLine($"  Feed per tooth:  {drilled.Cut.Ipt:0.#####} in");
            Console.WriteLine($"  Feed rate:       {drilled.Cut.Feed:0.##} in/min");

            // Slotting: the width of cut equals the diameter. Slotting = true tells HSMAdvisor the tool
            // is fully engaged, so it reduces the speeds and feeds accordingly.
            var slot = SampleData.EndMill(diameter: 0.5, flutes: 3, doc: 0.25);
            slot.Cut.Woc = slot.Tool.Diameter;
            slot.Cut.Slotting = true;
            var slotted = (await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate,
                new CalculateRequest { Calculation = slot, PresetSpeeds = true }, 0, ct)).Calculation;

            Console.WriteLine("Slotting:");
            Console.WriteLine($"  RPM:             {slotted.Cut.Rpm:N0}");
            Console.WriteLine($"  Feed per tooth:  {slotted.Cut.Ipt:0.#####} in");
            Console.WriteLine($"  Feed rate:       {slotted.Cut.Feed:0.##} in/min");
            Console.WriteLine($"  Plunge feed:     {slotted.Cut.Feed_plunge:0.##} in/min");
        }
    }
}
