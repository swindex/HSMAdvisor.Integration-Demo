using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Calculates speeds and feeds for an end mill without showing any HSMAdvisor window, then
    /// recalculates the same light cut with high speed machining and chip thinning compensation.
    /// </summary>
    public sealed class CalculateMillingSample : ISample
    {
        public string Title => "Calculate: end mill speeds and feeds (no UI)";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // 0.5 in 4-flute end mill, 1.0 in deep, 0.05 in stepover in 1018 steel
            CalculationData conventional = SampleData.EndMill(diameter: 0.5, flutes: 4, doc: 1.0, woc: 0.05);
            CalculationData result = await CalculateAsync(client, conventional, ct);

            Console.WriteLine("Conventional milling:");
            Print(result);

            // Same cut with:
            //   Hss = true  -> high speed machining (HSMAdvisor's "HSM" toolpath option)
            //   Hsm = true  -> radial chip thinning compensation
            // Note the names: "hsm" is chip thinning, "hss" is high speed machining.
            CalculationData highSpeed = SampleData.EndMill(diameter: 0.5, flutes: 4, doc: 1.0, woc: 0.05);
            highSpeed.Cut.Hss = true;
            highSpeed.Cut.Hsm = true;
            CalculationData highSpeedResult = await CalculateAsync(client, highSpeed, ct);

            Console.WriteLine("High speed machining with chip thinning:");
            Print(highSpeedResult);

            if (result.Cut.Feed > 0)
            {
                Console.WriteLine($"The HSM feed is {highSpeedResult.Cut.Feed / result.Cut.Feed:P0} of the conventional feed.");
            }
        }

        /// <summary>Sends one Calculate request and returns HSMAdvisor's calculation.</summary>
        private static async Task<CalculationData> CalculateAsync(HSMAdvisorClient client, CalculationData calc, CancellationToken ct)
        {
            var request = new CalculateRequest
            {
                Calculation = calc,
                // true: start from HSMAdvisor's recommended speeds for this tool and material.
                // false: keep the Rpm/Feed already in the calculation and only update the analysis.
                PresetSpeeds = true,
            };
            var response = await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate, request, 0, ct);
            return response.Calculation;
        }

        private static void Print(CalculationData calc)
        {
            var cut = calc.Cut;
            // Results holds read-only extras such as display names and the real chip thickness
            Console.WriteLine($"  Material:        {calc.Results?.MaterialName}");
            Console.WriteLine($"  RPM:             {cut.Rpm:N0}");
            Console.WriteLine($"  Surface speed:   {cut.Sfm:0} SFM");
            Console.WriteLine($"  Feed per tooth:  {cut.Ipt:0.#####} in");
            Console.WriteLine($"  Chip thickness:  {calc.Results?.ChipThickness:0.#####} in");
            Console.WriteLine($"  Feed rate:       {cut.Feed:0.#} in/min");
            Console.WriteLine($"  MRR:             {cut.Mrr:0.##} in³/min");
            Console.WriteLine($"  Deflection:      {cut.Tool_deflection:0.#####} in");
        }
    }
}
