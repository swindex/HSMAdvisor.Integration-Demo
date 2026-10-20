using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationDemo.Tasks
{
    /// <summary>Calculate request helpers shared by the calculation tasks.</summary>
    internal static class CalculationHelper
    {
        /// <summary>Recalculates speeds and feeds without any HSMAdvisor UI.</summary>
        public static async Task<CalculationData> CalculateAsync(TaskContext ctx, CalculationData calc, bool presetSpeeds = true)
        {
            var result = await ctx.RequestAsync<CalculationPayload>(MessageTypes.Calculate, new CalculateRequest { Calculation = calc, PresetSpeeds = presetSpeeds });
            return result.Calculation;
        }

        /// <summary>Side-by-side table of the main results (all calculations in the same units).</summary>
        public static string Compare(IList<string> titles, IList<CalculationData> calcs)
        {
            var rows = new List<object[]>
            {
                Row("RPM", calcs, c => c.Cut.Rpm.ToString("N0")),
                Row("Surface speed (SFM)", calcs, c => c.Cut.Sfm.ToString("0.#")),
                Row("Feed per tooth (in)", calcs, c => c.Cut.Ipt.ToString("0.#####")),
                Row("Chip thickness (in)", calcs, c => c.Results == null ? "" : c.Results.ChipThickness.ToString("0.#####")),
                Row("Feed rate (in/min)", calcs, c => c.Cut.Feed.ToString("0.##")),
                Row("DOC x WOC (in)", calcs, c => $"{c.Cut.Doc:0.####} x {c.Cut.Woc:0.####}"),
                Row("MRR (in³/min)", calcs, c => c.Cut.Mrr.ToString("0.###")),
                Row("Deflection (in)", calcs, c => c.Cut.Tool_deflection.ToString("0.#####")),
            };
            return OutputFormatter.FormatTable(new[] { "" }.Concat(titles), rows);
        }

        private static object[] Row(string label, IList<CalculationData> calcs, System.Func<CalculationData, string> value)
        {
            return new object[] { label }.Concat(calcs.Select(value)).ToArray();
        }
    }

    public class BasicMillingCalculationTask : EndMillTask
    {
        public BasicMillingCalculationTask()
        {
            Name = "Basic Milling Calculation";
            Description = "Builds a solid carbide end mill calculation and sends it to HSMAdvisor (Calculate) for speeds and feeds. No HSMAdvisor window is shown.";
            Category = "Calculations";

            AddEndMillParameters(0.5, 4, 0.5, 0.1);
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("BASIC MILLING CALCULATION"));

            ctx.Write(OutputFormatter.FormatStep(1, "Building the calculation..."));
            var calc = BuildEndMill(parameterValues);
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Tool_type_id", calc.Tool.Tool_type_id));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Tool_material_id", calc.Tool.Tool_material_id));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Coating_id", calc.Tool.Coating_id));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Diameter", calc.Tool.Diameter));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Flute_N", calc.Tool.Flute_N));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Material_id", calc.Cut.Material_id));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Doc", calc.Cut.Doc));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Woc", calc.Cut.Woc));

            ctx.Write(OutputFormatter.FormatStep(2, "Calculating..."));
            var result = await CalculationHelper.CalculateAsync(ctx, calc);

            ctx.Write(OutputFormatter.FormatStep(3, "Results:"));
            ctx.Write(OutputFormatter.FormatCalculation(result));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Calculation completed."));
        }
    }

    public class HSMMachiningCalculationTask : EndMillTask
    {
        public HSMMachiningCalculationTask()
        {
            Name = "HSM Machining Calculation";
            Description = "Calculates the same light-stepover cut twice: conventionally, and with high speed machining (Cut.HSM) plus radial chip thinning compensation (Cut.Chip_Thinning). Shows how HSMAdvisor raises the feed to keep the real chip thickness.";
            Category = "Calculations";

            AddEndMillParameters(0.5, 5, 1.0, 0.05);
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("HSM MACHINING CALCULATION"));

            ctx.Write(OutputFormatter.FormatStep(1, "Conventional milling (HSM = false, Chip_Thinning = false)..."));
            var conventional = BuildEndMill(parameterValues);
            conventional.Cut.HSM = false;
            conventional.Cut.Chip_Thinning = false;
            var conventionalResult = await CalculationHelper.CalculateAsync(ctx, conventional);

            ctx.Write(OutputFormatter.FormatStep(2, "High speed machining with chip thinning (HSM = true, Chip_Thinning = true)..."));
            var hsm = BuildEndMill(parameterValues);
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.HSM (high speed machining)", true));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Chip_Thinning (radial chip thinning)", true));
            hsm.Cut.HSM = true;
            hsm.Cut.Chip_Thinning = true;
            var hsmResult = await CalculationHelper.CalculateAsync(ctx, hsm);

            ctx.Write(OutputFormatter.FormatStep(3, "Comparison:"));
            ctx.Write(CalculationHelper.Compare(new[] { "Conventional", "HSM + chip thinning" }, new[] { conventionalResult, hsmResult }));

            ctx.Write("\n");
            if (conventionalResult.Cut.Feed > 0)
            {
                ctx.Write(OutputFormatter.FormatSuccess($"HSM feed is {hsmResult.Cut.Feed / conventionalResult.Cut.Feed * 100:0}% of the conventional feed."));
            }
            ctx.Write(OutputFormatter.FormatInfo("On the wire, Cut.Chip_Thinning is the \"chip_thinning\" key and Cut.HSM is the \"high_speed_machining\" key."));
        }
    }

    public class SlottingCalculationTask : EndMillTask
    {
        public SlottingCalculationTask()
        {
            Name = "Slotting Calculation";
            Description = "Full-width slotting: the width of cut equals the tool diameter (Cut.Slotting = true). HSMAdvisor reduces the speeds and feeds for the full engagement.";
            Category = "Calculations";

            AddEndMillParameters(0.5, 3, 0.25, 0.5);
            Parameters.RemoveAll(p => p.Name == "WOC");
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SLOTTING CALCULATION"));

            ctx.Write(OutputFormatter.FormatStep(1, "Building a full-width cut..."));
            var calc = BuildEndMill(parameterValues);
            calc.Cut.Woc = calc.Cut.Diameter;
            calc.Cut.Slotting = true;
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Woc", calc.Cut.Woc));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Slotting", true));

            ctx.Write(OutputFormatter.FormatStep(2, "Calculating..."));
            var result = await CalculationHelper.CalculateAsync(ctx, calc);

            ctx.Write(OutputFormatter.FormatStep(3, "Results:"));
            ctx.Write(OutputFormatter.FormatCalculation(result));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Slotting calculation completed."));
        }
    }

    public class DrillingCalculationTask : EndMillTask
    {
        public DrillingCalculationTask()
        {
            Name = "Drilling Calculation";
            Description = "Calculates a carbide twist drill (118° point) with a hole depth and peck. Shows that one message (Calculate) covers every tool family: the tool type decides which calculator HSMAdvisor uses.";
            Category = "Calculations";

            Parameters.Add(new TaskParameter
            {
                Name = "Diameter",
                Label = "Drill Diameter",
                ParameterType = typeof(double),
                DefaultValue = 0.25,
                Description = "Drill diameter in inches"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "Depth",
                Label = "Hole Depth",
                ParameterType = typeof(double),
                DefaultValue = 1.0,
                Description = "Hole depth in inches"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "Peck",
                Label = "Peck Depth",
                ParameterType = typeof(double),
                DefaultValue = 0.25,
                Description = "Peck increment in inches (0 = no pecking)"
            });
            AddMaterialParameter();
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("DRILLING CALCULATION"));

            ctx.Write(OutputFormatter.FormatStep(1, "Building a drill calculation..."));
            var calc = Samples.Drill(
                GetMaterialId(parameterValues),
                GetParameter<double>(parameterValues, "Diameter", 0.25),
                GetParameter<double>(parameterValues, "Depth", 1.0),
                GetParameter<double>(parameterValues, "Peck", 0.25));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Tool_type_id", calc.Tool.Tool_type_id));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Tool.Leadangle", calc.Tool.Leadangle + " (118° point)"));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Depth", calc.Cut.Depth));
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Peck", calc.Cut.Peck));

            ctx.Write(OutputFormatter.FormatStep(2, "Calculating..."));
            var result = await CalculationHelper.CalculateAsync(ctx, calc);

            ctx.Write(OutputFormatter.FormatStep(3, "Results:"));
            ctx.Write(OutputFormatter.FormatCalculation(result));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Drilling calculation completed."));
        }
    }

    public class MachineLimitCalculationTask : EndMillTask
    {
        public MachineLimitCalculationTask()
        {
            Name = "Apply a Machine Profile";
            Description = "Calculates the same cut for HSMAdvisor's default machine (Cut.Machine_id not set) and for a chosen machine profile (Cut.Machine_id = machine GUID). HSMAdvisor keeps RPM, feed and power within the machine's limits.";
            Category = "Calculations";

            Parameters.Add(new TaskParameter
            {
                Name = "Machine",
                Label = "Machine name contains",
                ParameterType = typeof(string),
                DefaultValue = "",
                Description = "Empty = first machine other than the default one. See \"List Machines\"."
            });
            AddEndMillParameters(0.125, 3, 0.125, 0.0125);
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            string filter = GetParameter<string>(parameterValues, "Machine", "");

            ctx.Write(OutputFormatter.FormatHeader("APPLY A MACHINE PROFILE"));

            ctx.Write(OutputFormatter.FormatStep(1, "Calculating without Machine_id (HSMAdvisor's default machine)..."));
            var free = BuildEndMill(parameterValues);
            var freeResult = await CalculationHelper.CalculateAsync(ctx, free);
            string defaultName = freeResult.Results?.MachineName ?? "";
            ctx.Write(OutputFormatter.FormatProperty("Default machine", defaultName.Length == 0 ? "(none)" : defaultName));

            ctx.Write(OutputFormatter.FormatStep(2, "Finding the machine..."));
            var reference = await ctx.GetReferenceDataAsync();
            // HSMAdvisor's machine list starts with a "--none--" placeholder
            var matches = reference.Machines
                .Where(m => !string.IsNullOrEmpty(m.Name) && !m.Name.StartsWith("--"))
                .Where(m => m.Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            var machine = filter.Length > 0
                ? matches.FirstOrDefault()
                : matches.FirstOrDefault(m => m.Name != defaultName) ?? matches.FirstOrDefault();
            if (machine == null)
            {
                ctx.Write(OutputFormatter.FormatWarning(reference.Machines.Count == 0
                    ? "No machines are defined in HSMAdvisor. Run \"Select Machine\" to create one."
                    : $"No machine name contains \"{filter}\"."));
                return;
            }
            ctx.Write(OutputFormatter.FormatProperty("Machine", $"{machine.Name} ({machine.Guid})"));

            ctx.Write(OutputFormatter.FormatStep(3, "Calculating with the machine..."));
            var limited = BuildEndMill(parameterValues);
            limited.Cut.Machine_id = machine.Guid;
            ctx.Write(OutputFormatter.FormatApiProperty("calc.Cut.Machine_id", machine.Guid));
            var limitedResult = await CalculationHelper.CalculateAsync(ctx, limited);

            ctx.Write(OutputFormatter.FormatStep(4, "Comparison:"));
            ctx.Write(CalculationHelper.Compare(
                new[] { "Default" + (defaultName.Length == 0 ? "" : $" ({defaultName})"), limitedResult.Results?.MachineName ?? machine.Name },
                new[] { freeResult, limitedResult }));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Speeds and feeds were calculated within the selected machine's limits."));
            ctx.Write(OutputFormatter.FormatInfo("Differences show where a machine limits RPM, feed or power. Try a smaller tool (higher RPM) or a bigger cut (more power)."));
        }
    }
}
