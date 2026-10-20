using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Sends one request without the client library, writing the wire format directly to the named
    /// pipe. This is what a Python, C++ or other non-.NET integration has to implement:
    ///
    ///   pipe:  \\.\pipe\HSMAdvisor.Integration.v1.&lt;session-id&gt;.&lt;domain&gt;.&lt;user&gt;
    ///   frame: 4-byte little-endian length + UTF-8 JSON
    ///   one request per connection; closing the connection early cancels the request
    /// </summary>
    public sealed class RawPipeProtocolSample : ISample
    {
        public string Title => "Raw pipe protocol (no client library)";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // A raw client has to start HSMAdvisor itself when the pipe does not exist:
            //   HSMAdvisor.exe /integration /parent-pid:<your process id>
            // and then retry connecting. To keep this sample short, the client library does that part.
            if (!await client.IsRunningAsync(ct))
            {
                await client.RequestAsync<HelloResponse>(MessageTypes.Hello, null, 0, ct);
            }

            // Request envelope. "payload" is the message's request object, or null.
            var request = new JObject
            {
                ["protocol"] = IntegrationProtocol.Version,
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = MessageTypes.FindMaterials,
                ["client"] = new JObject { ["name"] = "Raw pipe sample", ["version"] = "1.0" },
                ["owner_window"] = 0,
                ["payload"] = new JObject { ["name"] = "6061" },
            };

            // The pipe name is per Windows session and user
            string pipeName = IntegrationProtocol.GetPipeName();
            using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipe.ConnectAsync(2000, ct);

                await WriteFrameAsync(pipe, request.ToString(Formatting.None), ct);
                JObject response = JObject.Parse(await ReadFrameAsync(pipe, ct));

                // Response envelope: { protocol, id, status: ok|cancelled|error|busy, error: { code, message }, payload }
                Console.WriteLine(response.ToString(Formatting.Indented));
            }
        }

        private static async Task WriteFrameAsync(Stream pipe, string json, CancellationToken ct)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] length = { (byte)body.Length, (byte)(body.Length >> 8), (byte)(body.Length >> 16), (byte)(body.Length >> 24) };
            await pipe.WriteAsync(length, 0, length.Length, ct);
            await pipe.WriteAsync(body, 0, body.Length, ct);
            await pipe.FlushAsync(ct);
        }

        private static async Task<string> ReadFrameAsync(Stream pipe, CancellationToken ct)
        {
            byte[] header = await ReadExactlyAsync(pipe, 4, ct);
            int length = header[0] | header[1] << 8 | header[2] << 16 | header[3] << 24;
            byte[] body = await ReadExactlyAsync(pipe, length, ct);
            return Encoding.UTF8.GetString(body);
        }

        private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken ct)
        {
            var buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, read, count - read, ct);
                if (n == 0)
                {
                    throw new EndOfStreamException("HSMAdvisor closed the connection.");
                }
                read += n;
            }
            return buffer;
        }
    }
}
