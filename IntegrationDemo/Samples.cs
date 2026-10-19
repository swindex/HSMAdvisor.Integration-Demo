using HSMAdvisor.Integration;
using static HSMAdvisor.Integration.Records.Enums;

namespace IntegrationDemo
{
    /// <summary>
    /// Builds the calculations the demo tasks send to HSMAdvisor.
    ///
    /// A calculation is a Tool record (geometry) plus a Cut record (cutting parameters). Only set what
    /// you know: fields left unset take HSMAdvisor's defaults for a new calculation. Values shared by
    /// both records (diameter, stickout, hsm, hss) are read from the Cut when set there.
    /// </summary>
    public static class Samples
    {
        /// <summary>1018 steel, the license-free demo material.</summary>
        public const int DemoMaterialId = 227;

        public static CalculationData EndMill(int materialId, double diameter, int flutes, double doc, double woc)
        {
            var data = new CalculationData();
            var t = data.Tool;
            t.Tool_type_id = ToolTypes.SolidEndMill;
            t.Tool_material_id = ToolMaterials.Carbide;
            t.Coating_id = ToolCoatings.TiAlN;
            t.Diameter = diameter;
            t.Flute_N = flutes;
            t.Flute_Len = diameter * 3;
            t.Stickout = diameter * 4;
            t.Leadangle = 90;
            t.Helix_angle = 35;
            t.Comment = $"Demo {diameter:0.###} in {flutes}FL end mill";
            t.Number = 1;

            var c = data.Cut;
            c.Material_id = materialId;
            c.Diameter = diameter;
            c.Stickout = diameter * 4;
            c.Doc = doc;
            c.Woc = woc;
            c.Comment = "Demo cut";
            return data;
        }

        public static CalculationData Drill(int materialId, double diameter, double depth, double peck)
        {
            var data = new CalculationData();
            var t = data.Tool;
            t.Tool_type_id = ToolTypes.JobberTwistDrill;
            t.Tool_material_id = ToolMaterials.Carbide;
            t.Coating_id = ToolCoatings.TiAlN;
            t.Diameter = diameter;
            t.Flute_N = 2;
            t.Flute_Len = depth * 1.5;
            t.Stickout = depth * 2;
            t.Leadangle = 31; // 118° point
            t.Comment = $"Demo {diameter:0.###} in drill";
            t.Number = 2;

            var c = data.Cut;
            c.Material_id = materialId;
            c.Diameter = diameter;
            c.Stickout = depth * 2;
            c.Depth = depth;
            c.Peck = peck;
            c.Comment = "Demo drilling";
            return data;
        }
    }
}
