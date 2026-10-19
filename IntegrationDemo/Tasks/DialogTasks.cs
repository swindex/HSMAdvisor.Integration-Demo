using System.Collections.Generic;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationDemo.Tasks
{
    /// <summary>Parameters shared by the tasks that build a sample end mill calculation.</summary>
    public abstract class EndMillTask : DemoTask
    {
        protected void AddEndMillParameters(double diameter, int flutes, double doc, double woc)
        {
            Parameters.Add(new TaskParameter
            {
                Name = "Diameter",
                Label = "Tool Diameter",
                ParameterType = typeof(double),
                DefaultValue = diameter,
                Description = "End mill diameter in inches"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "Flutes",
                Label = "Number of Flutes",
                ParameterType = typeof(int),
                DefaultValue = flutes,
                Description = "Number of cutting edges"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "DOC",
                Label = "Depth of Cut (DOC)",
                ParameterType = typeof(double),
                DefaultValue = doc,
                Description = "Axial depth of cut in inches"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "WOC",
                Label = "Width of Cut (WOC)",
                ParameterType = typeof(double),
                DefaultValue = woc,
                Description = "Radial width of cut (stepover) in inches"
            });
            AddMaterialParameter();
        }

        protected void AddMaterialParameter()
        {
            Parameters.Add(new TaskParameter
            {
                Name = "MaterialId",
                Label = "Material ID",
                ParameterType = typeof(int),
                DefaultValue = Samples.DemoMaterialId,
                Description = $"Workpiece material id ({Samples.DemoMaterialId} = 1018 steel, license-free). See \"Find Materials by Name\"."
            });
        }

        protected int GetMaterialId(Dictionary<string, object> parameterValues)
        {
            return GetParameter<int>(parameterValues, "MaterialId", Samples.DemoMaterialId);
        }

        protected CalculationData BuildEndMill(Dictionary<string, object> parameterValues)
        {
            return Samples.EndMill(
                GetMaterialId(parameterValues),
                GetParameter<double>(parameterValues, "Diameter", 0.5),
                GetParameter<int>(parameterValues, "Flutes", 4),
                GetParameter<double>(parameterValues, "DOC", 0.5),
                GetParameter<double>(parameterValues, "WOC", 0.1));
        }
    }

    public class EditCalculationTask : EndMillTask
    {
        public EditCalculationTask()
        {
            Name = "Edit Calculation in HSMAdvisor";
            Description = "Sends a calculation to HSMAdvisor and opens the full HSMAdvisor calculator with OK/Cancel. The user fine-tunes the cut there; OK returns the edited calculation to your application. This is the main CAM integration workflow.";
            Category = "HSMAdvisor Dialogs";

            AddEndMillParameters(0.5, 4, 0.5, 0.1);
            Parameters.Add(new TaskParameter
            {
                Name = "Prompt",
                Label = "Prompt",
                ParameterType = typeof(string),
                DefaultValue = "Adjust the cut, then click OK to send it back to the demo.",
                Description = "Text HSMAdvisor shows next to its OK/Cancel buttons"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("EDIT CALCULATION IN HSMADVISOR"));

            ctx.Write(OutputFormatter.FormatStep(1, "Building the calculation (Tool + Cut records)..."));
            var calc = BuildEndMill(parameterValues);
            ctx.Write(OutputFormatter.FormatApiCall("new CalculationData { Tool = {...}, Cut = {...} }"));
            ctx.Write(OutputFormatter.FormatProperty("Tool", calc.Tool.Comment));
            ctx.Write(OutputFormatter.FormatProperty("DOC x WOC", $"{calc.Cut.Doc} x {calc.Cut.Woc} in"));

            ctx.Write(OutputFormatter.FormatStep(2, "Opening the HSMAdvisor calculator..."));
            var result = await ctx.RequestAsync<CalculationPayload>(MessageTypes.EditCalculation, new EditCalculationRequest
            {
                Calculation = calc,
                Prompt = GetParameter<string>(parameterValues, "Prompt", ""),
                CalculateFirst = true,
            });
            if (result == null)
            {
                return;
            }

            ctx.Write(OutputFormatter.FormatStep(3, "The user clicked OK. Calculation returned by HSMAdvisor:"));
            ctx.Write(OutputFormatter.FormatCalculation(result.Calculation));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Apply these values to your CAM operation."));
            ctx.Write(OutputFormatter.FormatInfo("Fields the demo sent (tool name, number, comment, CAM info) come back unchanged unless the user edited them."));
        }
    }

    public class SelectMaterialTask : DemoTask
    {
        public SelectMaterialTask()
        {
            Name = "Select Material";
            Description = "Opens HSMAdvisor's workpiece material picker and returns the chosen material's id and name.";
            Category = "HSMAdvisor Dialogs";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SELECT MATERIAL"));

            ctx.Write(OutputFormatter.FormatStep(1, "Opening the material picker..."));
            var result = await ctx.RequestAsync<SelectMaterialResponse>(MessageTypes.SelectMaterial);
            if (result == null)
            {
                return;
            }
            if (result.Material == null)
            {
                ctx.Write(OutputFormatter.FormatWarning("No material was selected."));
                return;
            }

            ctx.Write(OutputFormatter.FormatSection("Selected material"));
            ctx.Write(OutputFormatter.FormatProperty("Id", result.Material.Id));
            ctx.Write(OutputFormatter.FormatProperty("Name", result.Material.Name));
            ctx.Write(OutputFormatter.FormatProperty("Group", result.Material.Group.ToString("0.0#")));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Use it in a calculation with: calc.Cut.Material_id = {result.Material.Id};"));
        }
    }

    public class SelectMachineTask : DemoTask
    {
        public SelectMachineTask()
        {
            Name = "Select Machine";
            Description = "Opens HSMAdvisor's machine profile picker, where the user can also create or edit machines, and returns the chosen machine.";
            Category = "HSMAdvisor Dialogs";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SELECT MACHINE"));

            ctx.Write(OutputFormatter.FormatStep(1, "Opening the machine picker..."));
            var result = await ctx.RequestAsync<SelectMachineResponse>(MessageTypes.SelectMachine);
            if (result == null)
            {
                return;
            }
            if (result.Machine == null)
            {
                ctx.Write(OutputFormatter.FormatWarning("No machine was selected."));
                return;
            }

            ctx.Write(OutputFormatter.FormatSection("Selected machine"));
            ctx.Write(OutputFormatter.FormatProperty("Name", result.Machine.Name));
            ctx.Write(OutputFormatter.FormatProperty("Guid", result.Machine.Guid));
            ctx.Write(OutputFormatter.FormatProperty("Id", result.Machine.Id));
            ctx.Write(OutputFormatter.FormatProperty("Metric", result.Machine.IsMetric));

            // Machines may have been added in the picker
            ctx.Session.Reference = null;

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Use it in a calculation with: calc.Cut.Machine_id = \"{result.Machine.Guid}\";"));
        }
    }

    public class SelectCutTask : DemoTask
    {
        public SelectCutTask()
        {
            Name = "Select Tool and Cut from Library";
            Description = "Opens HSMAdvisor's tool library picker. The user picks a saved tool and cut; HSMAdvisor returns it as a calculation, e.g. to load proven cutting data into a new CAM operation.";
            Category = "HSMAdvisor Dialogs";

            Parameters.Add(new TaskParameter
            {
                Name = "Title",
                Label = "Dialog title",
                ParameterType = typeof(string),
                DefaultValue = "Select a tool and cut for the demo",
                Description = "Title of the picker window"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "CamOnly",
                Label = "CAM cuts only",
                ParameterType = typeof(bool),
                DefaultValue = false,
                Description = "Show only cuts marked for CAM, filtered by the toolpath type below"
            });
            Parameters.Add(new TaskParameter
            {
                Name = "ToolPathType",
                Label = "Toolpath type",
                ParameterType = typeof(string),
                DefaultValue = "Contour",
                Description = "CAM toolpath type stored with the cut (Cut.CAM.ToolPathType), e.g. Contour, Pocket, Drill"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SELECT TOOL AND CUT"));

            ctx.Write(OutputFormatter.FormatStep(1, "Opening the tool library picker..."));
            var result = await ctx.RequestAsync<CalculationPayload>(MessageTypes.SelectCut, new SelectCutRequest
            {
                Title = GetParameter<string>(parameterValues, "Title", ""),
                CamOnly = GetParameter<bool>(parameterValues, "CamOnly", false),
                ToolPathType = GetParameter<string>(parameterValues, "ToolPathType", ""),
            });
            if (result == null)
            {
                return;
            }
            if (result.Calculation == null)
            {
                ctx.Write(OutputFormatter.FormatWarning("No cut was selected."));
                return;
            }

            var calc = result.Calculation;
            ctx.Write(OutputFormatter.FormatStep(2, "Selected cut:"));
            ctx.Write(OutputFormatter.FormatProperty("Tool", calc.Tool.Description));
            ctx.Write(OutputFormatter.FormatProperty("Tool guid", calc.Tool.Guid));
            ctx.Write(OutputFormatter.FormatProperty("Cut", calc.Cut.Comment));
            ctx.Write(OutputFormatter.FormatProperty("Cut guid", calc.Cut.Guid));
            ctx.Write(OutputFormatter.FormatProperty("CAM toolpath", calc.Cut.CAM.ToolPathType));
            ctx.Write(OutputFormatter.FormatCalculation(calc));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Library cut loaded."));
        }
    }

    public class AddCutTask : EndMillTask
    {
        public AddCutTask()
        {
            Name = "Save Calculation to Tool Library";
            Description = "Calculates a cut, then opens HSMAdvisor's \"Add cut\" dialog to save it (and its tool, when new) into the user's tool library. Returns the GUIDs of the saved tool and cut.";
            Category = "HSMAdvisor Dialogs";

            AddEndMillParameters(0.375, 3, 0.375, 0.05);
            Parameters.Add(new TaskParameter
            {
                Name = "ToolName",
                Label = "Tool name",
                ParameterType = typeof(string),
                DefaultValue = "Integration demo 3/8 3FL",
                Description = "Name the tool gets in the library (Tool.Comment)"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SAVE TO TOOL LIBRARY"));

            ctx.Write(OutputFormatter.FormatStep(1, "Calculating speeds and feeds..."));
            var calc = BuildEndMill(parameterValues);
            calc.Tool.Comment = GetParameter<string>(parameterValues, "ToolName", calc.Tool.Comment);
            calc.Cut.Comment = "Demo contour";
            calc.Cut.CAM.Enabled = true;
            calc.Cut.CAM.ToolPathType = "Contour";
            var calculated = await ctx.RequestAsync<CalculationPayload>(MessageTypes.Calculate, new CalculateRequest { Calculation = calc, PresetSpeeds = true });
            ctx.Write(OutputFormatter.FormatProperty("RPM / Feed", $"{calculated.Calculation.Cut.Rpm:N0} / {calculated.Calculation.Cut.Feed:0.##} in/min"));

            ctx.Write(OutputFormatter.FormatStep(2, "Opening the Add Cut dialog..."));
            var saved = await ctx.RequestAsync<AddCutResponse>(MessageTypes.AddCut, new AddCutRequest { Calculation = calculated.Calculation, SaveToolIfNew = true });
            if (saved == null)
            {
                return;
            }

            ctx.Write(OutputFormatter.FormatSection("Saved"));
            ctx.Write(OutputFormatter.FormatProperty("Tool guid", saved.ToolGuid));
            ctx.Write(OutputFormatter.FormatProperty("Cut guid", saved.CutGuid));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Saved. Find it again with \"Find Library Tools by Name\" (\"{calc.Tool.Comment}\")."));
        }
    }
}
