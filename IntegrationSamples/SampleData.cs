using HSMAdvisor.Integration;
using static HSMAdvisor.Integration.Records.Enums;

namespace IntegrationSamples
{
    /// <summary>
    /// Builds the calculations the samples send to HSMAdvisor.
    ///
    /// A calculation is a Tool record (geometry) plus a Cut record (cutting parameters). Only set what
    /// you know: fields left unset take HSMAdvisor's defaults for a new calculation. Every length here is
    /// in inches because each value's *_m flag (Diameter_m, Doc_m, ...) defaults to false (imperial).
    /// </summary>
    public static class SampleData
    {
        /// <summary>1018 steel, a material that works without an HSMAdvisor license.</summary>
        public const int Steel1018 = 227;

        /// <summary>Solid carbide, TiAlN-coated end mill with a cut in the given material.</summary>
        public static CalculationData EndMill(double diameter = 0.5, int flutes = 4, double doc = 0.5, double woc = 0.1, int materialId = Steel1018)
        {
            var calc = new CalculationData();

            var tool = calc.Tool;
            tool.Tool_type_id = ToolTypes.SolidEndMill;  // decides which calculator HSMAdvisor uses
            tool.Tool_material_id = ToolMaterials.Carbide;
            tool.Coating_id = ToolCoatings.TiAlN;
            tool.Diameter = diameter;
            tool.Flute_N = flutes;
            tool.Flute_Len = diameter * 3;
            tool.Stickout = diameter * 4;
            tool.Helix_angle = 35;
            tool.Comment = $"{diameter:0.###} in {flutes}FL carbide end mill"; // the tool's name

            var cut = calc.Cut;
            cut.Material_id = materialId; // workpiece material, see sample 2 for how to look ids up
            cut.Doc = doc;                // axial depth of cut
            cut.Woc = woc;                // radial width of cut (stepover)
            return calc;
        }

        /// <summary>Carbide jobber drill with a 118° point, drilling a hole of the given depth.</summary>
        public static CalculationData Drill(double diameter = 0.25, double depth = 1.0, double peck = 0.25, int materialId = Steel1018)
        {
            var calc = new CalculationData();

            var tool = calc.Tool;
            tool.Tool_type_id = ToolTypes.JobberTwistDrill;
            tool.Tool_material_id = ToolMaterials.Carbide;
            tool.Coating_id = ToolCoatings.TiAlN;
            tool.Diameter = diameter;
            tool.Flute_N = 2;
            tool.Flute_Len = depth * 1.5;
            tool.Stickout = depth * 2;
            tool.Leadangle = 31; // half of the (180° - 118°) point angle
            tool.Comment = $"{diameter:0.###} in carbide drill";

            var cut = calc.Cut;
            cut.Material_id = materialId;
            cut.Depth = depth; // hole depth
            cut.Peck = peck;   // peck increment, 0 = no pecking
            return calc;
        }
    }
}
