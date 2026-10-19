using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationDemo.Tasks
{
    public class ListToolTypesTask : DemoTask
    {
        public ListToolTypesTask()
        {
            Name = "List Tool Types";
            Description = "Loads HSMAdvisor's reference data (GetReferenceData) and lists the tool types with their translated names and calculator family. Map your application's tool types to these ids.";
            Category = "Reference Data";

            Parameters.Add(new TaskParameter
            {
                Name = "SelectableOnly",
                Label = "Selectable only",
                ParameterType = typeof(bool),
                DefaultValue = true,
                Description = "Hide internal types that HSMAdvisor does not offer in its tool type list"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            bool selectableOnly = GetParameter<bool>(parameterValues, "SelectableOnly", true);

            ctx.Write(OutputFormatter.FormatHeader("TOOL TYPES"));
            ctx.Write(OutputFormatter.FormatStep(1, "Loading reference data..."));
            var data = await ctx.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData);

            var types = data.ToolTypes.Where(t => !selectableOnly || t.Selectable).OrderBy(t => t.Type).ThenBy(t => t.Name).ToList();
            ctx.Write(OutputFormatter.FormatStep(2, $"{types.Count} of {data.ToolTypes.Count} tool types:"));
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Id", "Enum", "Name", "Family", "Subtype", "Selectable" },
                types.Select(t => new object[] { (int)t.Id, t.Id, t.Name, t.Type, t.Subtype, t.Selectable })));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Listed {types.Count} tool types."));
        }
    }

    public class ListToolMaterialsAndCoatingsTask : DemoTask
    {
        public ListToolMaterialsAndCoatingsTask()
        {
            Name = "List Tool Materials and Coatings";
            Description = "Lists the cutting tool materials (HSS, carbide, ...) and coatings (TiN, AlTiN, ...) HSMAdvisor knows, with their ids and display names.";
            Category = "Reference Data";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("TOOL MATERIALS AND COATINGS"));
            ctx.Write(OutputFormatter.FormatStep(1, "Loading reference data..."));
            var data = await ctx.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData);

            ctx.Write(OutputFormatter.FormatSection($"Tool materials ({data.ToolMaterials.Count})"));
            ctx.Write(OutputFormatter.FormatTable(new[] { "Id", "Name" }, data.ToolMaterials.Select(m => new object[] { m.Id, m.Name })));

            ctx.Write(OutputFormatter.FormatSection($"Tool coatings ({data.ToolCoatings.Count})"));
            ctx.Write(OutputFormatter.FormatTable(new[] { "Id", "Name" }, data.ToolCoatings.Select(m => new object[] { m.Id, m.Name })));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Ids match the Enums.ToolMaterials / Enums.ToolCoatings values in HSMAdvisor.Integration.Records."));
        }
    }

    public class ListMachinesTask : DemoTask
    {
        public ListMachinesTask()
        {
            Name = "List Machines";
            Description = "Lists the machine profiles defined in the user's HSMAdvisor. Assign a machine to a calculation through Cut.Machine_id (the machine GUID) so its RPM, feed and power limits are applied.";
            Category = "Reference Data";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("MACHINES"));
            ctx.Write(OutputFormatter.FormatStep(1, "Loading reference data..."));
            var data = await ctx.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData);

            ctx.Write(OutputFormatter.FormatStep(2, $"{data.Machines.Count} machine(s):"));
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Name", "Metric", "Id", "Guid" },
                data.Machines.Select(m => new object[] { m.Name, m.IsMetric, m.Id, m.Guid })));

            ctx.Write("\n");
            if (data.Machines.Count == 0)
            {
                ctx.Write(OutputFormatter.FormatInfo("No machines are defined. Add one in HSMAdvisor (Machine profile) or run \"Select Machine\"."));
            }
            else
            {
                ctx.Write(OutputFormatter.FormatSuccess($"Listed {data.Machines.Count} machine(s)."));
            }
        }
    }

    public class ListMaterialsTask : DemoTask
    {
        public ListMaterialsTask()
        {
            Name = "List Workpiece Materials";
            Description = "Lists the workpiece materials from HSMAdvisor's reference data. Assign one to a calculation through Cut.Material_id.";
            Category = "Reference Data";

            Parameters.Add(new TaskParameter
            {
                Name = "GroupLeadersOnly",
                Label = "Group leaders only",
                ParameterType = typeof(bool),
                DefaultValue = true,
                Description = "Show only the main material of each material group"
            });

            Parameters.Add(new TaskParameter
            {
                Name = "MaxRows",
                Label = "Max rows",
                ParameterType = typeof(int),
                DefaultValue = 50,
                Description = "Maximum number of materials to print"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            bool leadersOnly = GetParameter<bool>(parameterValues, "GroupLeadersOnly", true);
            int maxRows = GetParameter<int>(parameterValues, "MaxRows", 50);

            ctx.Write(OutputFormatter.FormatHeader("WORKPIECE MATERIALS"));
            ctx.Write(OutputFormatter.FormatStep(1, "Loading reference data..."));
            var data = await ctx.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData);

            var materials = data.Materials.Where(m => !leadersOnly || m.GroupLeader).OrderBy(m => m.Group).ThenBy(m => m.Name).ToList();
            ctx.Write(OutputFormatter.FormatStep(2, $"{materials.Count} of {data.Materials.Count} materials match, showing {System.Math.Min(maxRows, materials.Count)}:"));
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Id", "Group", "Name" },
                materials.Take(maxRows).Select(m => new object[] { m.Id, m.Group.ToString("0.0#"), m.Name })));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Listed {System.Math.Min(maxRows, materials.Count)} material(s)."));
        }
    }

    public class FindMaterialsTask : DemoTask
    {
        public FindMaterialsTask()
        {
            Name = "Find Materials by Name";
            Description = "Searches workpiece materials by part of their name (FindMaterials), e.g. to map a material name stored in your CAM file to an HSMAdvisor material id.";
            Category = "Reference Data";

            Parameters.Add(new TaskParameter
            {
                Name = "Name",
                Label = "Name contains",
                ParameterType = typeof(string),
                DefaultValue = "1018",
                Description = "Case-insensitive part of the material name, e.g. 1018, 6061, 304, Inconel"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            string name = GetParameter<string>(parameterValues, "Name", "1018");

            ctx.Write(OutputFormatter.FormatHeader("FIND MATERIALS"));
            ctx.Write(OutputFormatter.FormatStep(1, $"Searching for \"{name}\"..."));
            var result = await ctx.RequestAsync<FindMaterialsResponse>(MessageTypes.FindMaterials, new FindMaterialsRequest { Name = name });

            ctx.Write(OutputFormatter.FormatStep(2, $"{result.Materials.Count} match(es):"));
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Id", "Group", "Leader", "Name" },
                result.Materials.Select(m => new object[] { m.Id, m.Group.ToString("0.0#"), m.GroupLeader ? "*" : "", m.Name })));

            ctx.Write("\n");
            ctx.Write(result.Materials.Count > 0
                ? OutputFormatter.FormatSuccess($"Found {result.Materials.Count} material(s).")
                : OutputFormatter.FormatWarning("No material matches."));
        }
    }

    public class FindToolsTask : DemoTask
    {
        public FindToolsTask()
        {
            Name = "Find Library Tools by Name";
            Description = "Looks up tools in the user's HSMAdvisor tool library by their exact name (FindTools), then loads the first match with all its cuts (GetTool). This is how an integration links a CAM tool to a saved HSMAdvisor tool.";
            Category = "Reference Data";

            Parameters.Add(new TaskParameter
            {
                Name = "Comment",
                Label = "Tool name",
                ParameterType = typeof(string),
                DefaultValue = "",
                Description = "Exact tool name (Comment) as shown in the HSMAdvisor library"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            string comment = GetParameter<string>(parameterValues, "Comment", "");

            ctx.Write(OutputFormatter.FormatHeader("FIND LIBRARY TOOLS"));
            if (string.IsNullOrWhiteSpace(comment))
            {
                ctx.Write(OutputFormatter.FormatWarning("Enter the name of a tool from your HSMAdvisor library. Tip: run \"Save Calculation to Tool Library\" first, then search for its tool name."));
                return;
            }

            ctx.Write(OutputFormatter.FormatStep(1, $"Searching the library for \"{comment}\"..."));
            var found = await ctx.RequestAsync<FindToolsResponse>(MessageTypes.FindTools, new FindToolsRequest { Comment = comment });
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Name", "Library", "Guid" },
                found.Tools.Select(t => new object[] { t.Name, t.Library, t.Guid })));

            if (found.Tools.Count == 0)
            {
                ctx.Write("\n");
                ctx.Write(OutputFormatter.FormatWarning("No tool with this exact name was found."));
                return;
            }

            ctx.Write(OutputFormatter.FormatStep(2, "Loading the first tool..."));
            var tool = (await ctx.RequestAsync<GetToolResponse>(MessageTypes.GetTool, new GetToolRequest { Guid = found.Tools[0].Guid })).Tool;

            ctx.Write(OutputFormatter.FormatSection("Tool"));
            ctx.Write(OutputFormatter.FormatProperty("Description", tool.Description));
            ctx.Write(OutputFormatter.FormatProperty("Type", tool.Tool_type_id));
            ctx.Write(OutputFormatter.FormatProperty("Diameter", OutputFormatter.Length(tool.Diameter, tool.Diameter_m)));
            ctx.Write(OutputFormatter.FormatProperty("Flutes", tool.Flute_N));
            ctx.Write(OutputFormatter.FormatProperty("Material / Coating", $"{tool.Tool_material_id} / {tool.Coating_id}"));

            ctx.Write(OutputFormatter.FormatSection($"Cuts ({tool.Cuts.Count})"));
            ctx.Write(OutputFormatter.FormatTable(
                new[] { "Name", "RPM", "Feed", "DOC", "WOC", "Guid" },
                tool.Cuts.Select(c => new object[] { c.Comment, c.Rpm, c.Feed.ToString("0.##"), c.Doc.ToString("0.####"), c.Woc.ToString("0.####"), c.Guid })));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Loaded '{tool.Comment}' with {tool.Cuts.Count} cut(s)."));
        }
    }
}
