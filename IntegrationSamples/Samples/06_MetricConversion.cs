using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Every value in a calculation carries its own unit flag (Diameter_m, Doc_m, Feed_m, ...:
    /// true = metric). ConvertUnits converts all values of a calculation to metric or imperial.
    /// </summary>
    public sealed class MetricConversionSample : ISample
    {
        public string Title => "Units: convert a calculation to metric";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            var imperial = (await client.RequestAsync<CalculationPayload>(MessageTypes.Calculate,
                new CalculateRequest { Calculation = SampleData.EndMill(), PresetSpeeds = true }, 0, ct)).Calculation;

            var metric = (await client.RequestAsync<CalculationPayload>(MessageTypes.ConvertUnits,
                new ConvertUnitsRequest { Calculation = imperial, Metric = true }, 0, ct)).Calculation;

            Console.WriteLine($"{"",-15}{"Imperial",-16}{"Metric",-16}Flag");
            Console.WriteLine($"{"Diameter",-15}{$"{imperial.Tool.Diameter:0.###} in",-16}{$"{metric.Tool.Diameter:0.##} mm",-16}Diameter_m = {metric.Tool.Diameter_m}");
            Console.WriteLine($"{"Depth of cut",-15}{$"{imperial.Cut.Doc:0.###} in",-16}{$"{metric.Cut.Doc:0.##} mm",-16}Doc_m = {metric.Cut.Doc_m}");
            Console.WriteLine($"{"Surface speed",-15}{$"{imperial.Cut.Sfm:0} SFM",-16}{$"{metric.Cut.Sfm:0} m/min",-16}Sfm_m = {metric.Cut.Sfm_m}");
            Console.WriteLine($"{"Feed rate",-15}{$"{imperial.Cut.Feed:0.#} in/min",-16}{$"{metric.Cut.Feed:0} mm/min",-16}Feed_m = {metric.Cut.Feed_m}");
            Console.WriteLine($"{"RPM",-15}{$"{imperial.Cut.Rpm:N0}",-16}{$"{metric.Cut.Rpm:N0}",-16}(no unit)");

            // A metric CAM system does not need ConvertUnits: set metric values directly together with
            // their flags, e.g.  calc.Tool.Diameter = 12; calc.Tool.Diameter_m = true;
        }
    }
}
