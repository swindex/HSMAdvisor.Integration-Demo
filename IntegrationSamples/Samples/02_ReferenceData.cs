using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Reads the ids an integration needs to map its own data to HSMAdvisor: tool types, tool materials,
    /// coatings, machines and workpiece materials. Names come back in the user's HSMAdvisor language.
    /// </summary>
    public sealed class ReferenceDataSample : ISample
    {
        public string Title => "Reference data: tool types, materials, machines";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // GetReferenceData returns everything in one response. It can be several hundred KB,
            // so load it once and cache it rather than requesting it per operation.
            var data = await client.RequestAsync<ReferenceData>(MessageTypes.GetReferenceData, null, 0, ct);

            // Tool types: map your CAM tool types to these ids (Tool.Tool_type_id).
            // Selectable = offered in HSMAdvisor's tool type list; Type = calculator family.
            Console.WriteLine($"Tool types ({data.ToolTypes.Count}):");
            foreach (var type in data.ToolTypes.Where(t => t.Selectable).OrderBy(t => t.Type).Take(10))
            {
                Console.WriteLine($"  {(int)type.Id,4}  {type.Id,-24} {type.Name} [{type.Type}]");
            }

            // Tool materials and coatings: Tool.Tool_material_id / Tool.Coating_id.
            // The ids match the Enums.ToolMaterials / Enums.ToolCoatings values.
            Console.WriteLine($"Tool materials: {string.Join(", ", data.ToolMaterials.Select(m => $"{m.Id}={m.Name}"))}");
            Console.WriteLine($"Coatings:       {string.Join(", ", data.ToolCoatings.Take(8).Select(c => $"{c.Id}={c.Name}"))}, ...");

            // Machines: the user's machine profiles. Assign one with Cut.Machine_id = machine.Guid.
            Console.WriteLine($"Machines ({data.Machines.Count}):");
            foreach (var machine in data.Machines)
            {
                Console.WriteLine($"  {machine.Name} ({(machine.IsMetric ? "metric" : "imperial")}) {machine.Guid}");
            }

            // Workpiece materials: Cut.Material_id. A group leader is the main material of its group.
            Console.WriteLine($"Workpiece materials: {data.Materials.Count}, of which {data.Materials.Count(m => m.GroupLeader)} group leaders");

            // To map a material name stored in your CAM file to an id, search by part of the name.
            var found = await client.RequestAsync<FindMaterialsResponse>(MessageTypes.FindMaterials,
                new FindMaterialsRequest { Name = "6061" }, 0, ct);

            Console.WriteLine("Materials containing \"6061\":");
            foreach (var material in found.Materials)
            {
                Console.WriteLine($"  {material.Id,5}  {material.Name}");
            }
        }
    }
}
