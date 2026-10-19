using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntegrationDemo.Tasks
{
    public class MetricImperialConversionTask : EndMillTask
    {
        public MetricImperialConversionTask()
        {
            Name = "Metric / Imperial Conversion";
            Description = "Calculates an imperial cut, then asks HSMAdvisor to convert every value to metric (ConvertUnits). Each value carries its own unit flag (*_m), so a metric CAM system can send and receive metric data directly.";
            Category = "Advanced Features";

            AddEndMillParameters(0.5, 4, 0.5, 0.1);
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("METRIC / IMPERIAL CONVERSION"));

            ctx.Write(OutputFormatter.FormatStep(1, "Calculating in imperial units..."));
            var imperial = await CalculationHelper.CalculateAsync(ctx, BuildEndMill(parameterValues));

            ctx.Write(OutputFormatter.FormatStep(2, "Converting to metric..."));
            var metric = (await ctx.RequestAsync<CalculationPayload>(MessageTypes.ConvertUnits, new ConvertUnitsRequest { Calculation = imperial, Metric = true })).Calculation;

            ctx.Write(OutputFormatter.FormatStep(3, "Comparison:"));
            var rows = new[]
            {
                new object[] { "Diameter", OutputFormatter.Length(imperial.Tool.Diameter, imperial.Tool.Diameter_m), OutputFormatter.Length(metric.Tool.Diameter, metric.Tool.Diameter_m), metric.Tool.Diameter_m },
                new object[] { "DOC", OutputFormatter.Length(imperial.Cut.Doc, imperial.Cut.Doc_m), OutputFormatter.Length(metric.Cut.Doc, metric.Cut.Doc_m), metric.Cut.Doc_m },
                new object[] { "WOC", OutputFormatter.Length(imperial.Cut.Woc, imperial.Cut.Woc_m), OutputFormatter.Length(metric.Cut.Woc, metric.Cut.Woc_m), metric.Cut.Woc_m },
                new object[] { "RPM", imperial.Cut.Rpm.ToString("N0"), metric.Cut.Rpm.ToString("N0"), "-" },
                new object[] { "Surface speed", $"{imperial.Cut.Sfm:0.#} SFM", $"{metric.Cut.Sfm:0.#} m/min", metric.Cut.Sfm_m },
                new object[] { "Feed per tooth", $"{imperial.Cut.Ipt:0.#####} in", $"{metric.Cut.Ipt:0.####} mm", metric.Cut.Ipt_m },
                new object[] { "Feed rate", $"{imperial.Cut.Feed:0.##} in/min", $"{metric.Cut.Feed:0.#} mm/min", metric.Cut.Feed_m },
            };
            ctx.Write(OutputFormatter.FormatTable(new[] { "", "Imperial", "Metric", "*_m flag" }, rows));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("Converted. Cut.Input_units_m / Result_units_m = " + metric.Cut.Input_units_m + " / " + metric.Cut.Result_units_m));
            ctx.Write(OutputFormatter.FormatInfo("ConvertUnits converts values. To send metric values yourself, set them with their *_m flags = true."));
        }
    }

    public class TranslateTask : DemoTask
    {
        public TranslateTask()
        {
            Name = "Translate UI Text";
            Description = "Looks up text in the user's HSMAdvisor language (Translate), so your integration's own dialogs can use the same wording and language as HSMAdvisor.";
            Category = "Advanced Features";

            Parameters.Add(new TaskParameter
            {
                Name = "Keys",
                Label = "Translation keys",
                ParameterType = typeof(string),
                DefaultValue = "DLG_Title88, DLG_Text88",
                Description = "Comma-separated HSMAdvisor translation keys"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            var keys = GetParameter<string>(parameterValues, "Keys", "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim()).Where(k => k.Length > 0).ToList();

            ctx.Write(OutputFormatter.FormatHeader("TRANSLATE"));
            ctx.Write(OutputFormatter.FormatStep(1, $"Translating {keys.Count} key(s)..."));
            var request = new TranslateRequest();
            request.Keys.AddRange(keys);
            var result = await ctx.RequestAsync<TranslateResponse>(MessageTypes.Translate, request);

            ctx.Write(OutputFormatter.FormatStep(2, "Translations:"));
            foreach (var key in keys)
            {
                string text;
                ctx.Write(OutputFormatter.FormatProperty(key, result.Strings.TryGetValue(key, out text) && !string.IsNullOrEmpty(text) ? text : "(no translation)"));
            }

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess($"Received {result.Strings.Count} string(s)."));
        }
    }

    public class CancelInteractiveRequestTask : EndMillTask
    {
        public CancelInteractiveRequestTask()
        {
            Name = "Cancel an Interactive Request";
            Description = "Opens the HSMAdvisor calculator, then cancels the request from code after a few seconds. Cancelling closes the pipe connection and HSMAdvisor closes its dialog, e.g. when the user closes your application or cancels your own wait dialog.";
            Category = "Advanced Features";

            Parameters.Add(new TaskParameter
            {
                Name = "Seconds",
                Label = "Cancel after (s)",
                ParameterType = typeof(int),
                DefaultValue = 8,
                Description = "Seconds to wait before cancelling"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            int seconds = GetParameter<int>(parameterValues, "Seconds", 8);

            ctx.Write(OutputFormatter.FormatHeader("CANCEL AN INTERACTIVE REQUEST"));

            ctx.Write(OutputFormatter.FormatStep(1, $"Opening the HSMAdvisor calculator; cancelling it in {seconds} s..."));
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ctx.Cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(seconds));
                ctx.Write(OutputFormatter.FormatApiCall($"cts.CancelAfter(TimeSpan.FromSeconds({seconds}))"));
                try
                {
                    var request = new EditCalculationRequest { Calculation = Samples.EndMill(Samples.DemoMaterialId, 0.5, 4, 0.5, 0.1), Prompt = $"This dialog closes by itself after {seconds} seconds." };
                    var response = await ctx.SendAsync(MessageTypes.EditCalculation, request, timeout.Token);
                    ctx.Write("\n");
                    ctx.Write(OutputFormatter.FormatInfo($"The user finished the dialog first (status '{response.Status}')."));
                    return;
                }
                catch (OperationCanceledException) when (!ctx.Cancellation.IsCancellationRequested)
                {
                    ctx.Write(OutputFormatter.FormatApiCallWithReturn("await client.SendAsync(...)", "throws OperationCanceledException"));
                }
            }

            ctx.Write(OutputFormatter.FormatStep(2, "Checking that HSMAdvisor is free again..."));
            var hello = await ctx.SendAsync(MessageTypes.Hello);

            ctx.Write("\n");
            ctx.Write(hello.IsOk
                ? OutputFormatter.FormatSuccess("The request was cancelled and HSMAdvisor closed its dialog.")
                : OutputFormatter.FormatWarning("HSMAdvisor answered '" + hello.Status + "'."));
        }
    }

    public class ErrorResponsesTask : DemoTask
    {
        public ErrorResponsesTask()
        {
            Name = "Error and Busy Responses";
            Description = "Triggers the responses an integration has to handle: 'error' with a code (not_found, unknown_message) and 'busy' when an HSMAdvisor dialog is already open for another request (only one interactive request runs at a time).";
            Category = "Advanced Features";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("ERROR AND BUSY RESPONSES"));

            ctx.Write(OutputFormatter.FormatStep(1, "Loading a tool that does not exist..."));
            var notFound = await ctx.SendAsync(MessageTypes.GetTool, new GetToolRequest { Guid = Guid.NewGuid().ToString() });
            ctx.Write(OutputFormatter.FormatProperty("Expected", $"status '{ResponseStatus.Error}', code '{ErrorCodes.NotFound}'"));

            ctx.Write(OutputFormatter.FormatStep(2, "Sending a message type HSMAdvisor does not know..."));
            var unknown = await ctx.SendAsync("NoSuchMessage");
            ctx.Write(OutputFormatter.FormatProperty("Expected", $"status '{ResponseStatus.Error}', code '{ErrorCodes.UnknownMessage}'"));

            ctx.Write(OutputFormatter.FormatStep(3, "Opening a dialog, then sending a second interactive request..."));
            bool busy;
            using (var first = CancellationTokenSource.CreateLinkedTokenSource(ctx.Cancellation))
            {
                var dialog = ctx.SendAsync(MessageTypes.SelectMaterial, null, first.Token);
                await Task.Delay(1500, ctx.Cancellation);
                if (dialog.IsCompleted)
                {
                    await dialog; // surfaces a failure to open the dialog
                }
                var second = await ctx.SendAsync(MessageTypes.SelectMachine);
                busy = second.Status == ResponseStatus.Busy;
                ctx.Write(OutputFormatter.FormatProperty("Expected", $"status '{ResponseStatus.Busy}'"));

                ctx.Write(OutputFormatter.FormatStep(4, "Cancelling the first request (closes the material picker)..."));
                first.Cancel();
                try
                {
                    await dialog;
                }
                catch (OperationCanceledException) when (!ctx.Cancellation.IsCancellationRequested)
                {
                    ctx.Write(OutputFormatter.FormatApiCallWithReturn("await dialog", "throws OperationCanceledException"));
                }
            }

            ctx.Write(OutputFormatter.FormatSection("Summary"));
            ctx.Write(OutputFormatter.FormatProperty("not_found", notFound.Error?.Code == ErrorCodes.NotFound ? "✓" : "✗ " + notFound.Status));
            ctx.Write(OutputFormatter.FormatProperty("unknown_message", unknown.Error?.Code == ErrorCodes.UnknownMessage ? "✓" : "✗ " + unknown.Status));
            ctx.Write(OutputFormatter.FormatProperty("busy", busy ? "✓" : "✗"));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("HSMAdvisorClient.RequestAsync turns error/busy responses into IntegrationException(code, message)."));
        }
    }

    public class RawProtocolTask : DemoTask
    {
        public RawProtocolTask()
        {
            Name = "Raw Pipe Protocol (No Client Library)";
            Description = "Sends one request by writing the wire format directly to the named pipe: a 4-byte little-endian length followed by the UTF-8 JSON envelope. Shows what a Python, C++ or other non-.NET integration has to implement.";
            Category = "Advanced Features";

            Parameters.Add(new TaskParameter
            {
                Name = "Message",
                Label = "Message",
                ParameterType = typeof(string),
                DefaultValue = MessageTypes.Hello,
                Options = new List<string> { MessageTypes.Hello, MessageTypes.FindMaterials, MessageTypes.Translate },
                Description = "Request to send"
            });
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            string type = GetParameter<string>(parameterValues, "Message", MessageTypes.Hello);

            ctx.Write(OutputFormatter.FormatHeader("RAW PIPE PROTOCOL"));

            ctx.Write(OutputFormatter.FormatStep(1, "Making sure HSMAdvisor is running..."));
            if (!await ctx.Session.Client.IsRunningAsync(ctx.Cancellation))
            {
                ctx.Write(OutputFormatter.FormatInfo("Not running: starting it with the client library first (a raw client would start HSMAdvisor.exe /integration /parent-pid:N itself)."));
                await ctx.RequestAsync<HelloResponse>(MessageTypes.Hello);
            }

            ctx.Write(OutputFormatter.FormatStep(2, "Building the request envelope..."));
            JObject payload = null;
            if (type == MessageTypes.FindMaterials)
            {
                payload = new JObject { ["name"] = "6061" };
            }
            else if (type == MessageTypes.Translate)
            {
                payload = new JObject { ["keys"] = new JArray("DLG_Title88") };
            }
            var envelope = new JObject
            {
                ["protocol"] = IntegrationProtocol.Version,
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = type,
                ["client"] = new JObject { ["name"] = IntegrationSession.ClientName, ["version"] = IntegrationSession.AppVersion, ["action"] = "RawProtocol" },
                ["owner_window"] = 0,
                ["payload"] = payload,
            };
            byte[] body = Encoding.UTF8.GetBytes(envelope.ToString(Formatting.None));
            byte[] header = { (byte)body.Length, (byte)(body.Length >> 8), (byte)(body.Length >> 16), (byte)(body.Length >> 24) };
            ctx.Write(OutputFormatter.FormatJson("request envelope", envelope));
            ctx.Write(OutputFormatter.FormatProperty("Frame", $"[{BitConverter.ToString(header)}] (length {body.Length}, little-endian) + {body.Length} bytes UTF-8 JSON"));

            ctx.Write(OutputFormatter.FormatStep(3, "Writing to the pipe..."));
            string pipeName = IntegrationProtocol.GetPipeName();
            ctx.Write(OutputFormatter.FormatApiCall($"new NamedPipeClientStream(\".\", \"{pipeName}\").ConnectAsync(2000)"));
            using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipe.ConnectAsync(2000, ctx.Cancellation);
                await pipe.WriteAsync(header, 0, header.Length, ctx.Cancellation);
                await pipe.WriteAsync(body, 0, body.Length, ctx.Cancellation);
                await pipe.FlushAsync(ctx.Cancellation);

                ctx.Write(OutputFormatter.FormatStep(4, "Reading the response frame..."));
                byte[] lengthBytes = await ReadExactlyAsync(pipe, 4, ctx.Cancellation);
                int length = lengthBytes[0] | lengthBytes[1] << 8 | lengthBytes[2] << 16 | lengthBytes[3] << 24;
                byte[] responseBody = await ReadExactlyAsync(pipe, length, ctx.Cancellation);
                var response = JObject.Parse(Encoding.UTF8.GetString(responseBody));

                ctx.Write(OutputFormatter.FormatProperty("Frame", $"[{BitConverter.ToString(lengthBytes)}] (length {length}) + {length} bytes"));
                ctx.Write(OutputFormatter.FormatJson("response envelope", response));

                ctx.Write("\n");
                ctx.Write((string)response["status"] == ResponseStatus.Ok
                    ? OutputFormatter.FormatSuccess("Raw request answered. One request per connection: the pipe is closed now.")
                    : OutputFormatter.FormatWarning($"HSMAdvisor answered '{response["status"]}'."));
            }
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
