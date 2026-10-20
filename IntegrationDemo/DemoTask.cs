using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration;
using HSMAdvisor.Integration.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IntegrationDemo
{
    /// <summary>
    /// Base class for all demo tasks
    /// </summary>
    public abstract class DemoTask
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        /// <summary>
        /// Parameters that the task needs
        /// </summary>
        public List<TaskParameter> Parameters { get; set; } = new List<TaskParameter>();

        /// <summary>
        /// Execute the task, writing formatted output through <paramref name="ctx"/>
        /// </summary>
        public abstract Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues);

        /// <summary>
        /// Get parameter value with fallback to default
        /// </summary>
        protected T GetParameter<T>(Dictionary<string, object> parameterValues, string paramName, T defaultValue = default(T))
        {
            if (parameterValues.ContainsKey(paramName) && parameterValues[paramName] != null)
            {
                try
                {
                    return (T)Convert.ChangeType(parameterValues[paramName], typeof(T), CultureInfo.CurrentCulture);
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Represents a parameter for a task
    /// </summary>
    public class TaskParameter
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public Type ParameterType { get; set; }
        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }

        // For combo box options
        public List<string> Options { get; set; }
    }

    /// <summary>
    /// Everything a running task needs: the HSMAdvisor connection, the output window and the Cancel button.
    /// </summary>
    public class TaskContext
    {
        private readonly Action<string> write;

        public TaskContext(IntegrationSession session, long ownerWindow, bool showJson, CancellationToken cancellation, Action<string> write)
        {
            Session = session;
            OwnerWindow = ownerWindow;
            ShowJson = showJson;
            Cancellation = cancellation;
            this.write = write;
        }

        public IntegrationSession Session { get; private set; }

        /// <summary>HWND of the demo window. HSMAdvisor dialogs opened for interactive requests are owned by it.</summary>
        public long OwnerWindow { get; private set; }

        /// <summary>Print request/response JSON (the "{ } Show JSON" button in the status bar).</summary>
        public bool ShowJson { get; private set; }

        /// <summary>Signalled by the Cancel button. Cancelling closes the pipe, which makes HSMAdvisor close its dialog.</summary>
        public CancellationToken Cancellation { get; private set; }

        public void Write(string text)
        {
            write(text);
        }

        /// <summary>
        /// Sends one request to HSMAdvisor (starting it when needed) and logs the call.
        /// Returns the response payload, or null when the user cancelled the HSMAdvisor dialog.
        /// Throws <see cref="IntegrationException"/> for error/busy responses.
        /// </summary>
        public async Task<T> RequestAsync<T>(string type, object payload = null) where T : class, new()
        {
            var response = await SendAsync(type, payload);
            switch (response.Status)
            {
                case ResponseStatus.Ok:
                    return response.GetPayload<T>();
                case ResponseStatus.Cancelled:
                    Write(OutputFormatter.FormatWarning("The user cancelled the HSMAdvisor dialog."));
                    return null;
                default:
                    throw new IntegrationException(
                        response.Error == null ? response.Status : response.Error.Code,
                        response.Error == null ? "HSMAdvisor returned '" + response.Status + "'." : response.Error.Message);
            }
        }

        /// <summary>Sends one request and returns the raw response envelope (any status), logging the call.</summary>
        public Task<IntegrationResponse> SendAsync(string type, object payload = null)
        {
            return SendAsync(type, payload, Cancellation);
        }

        public async Task<IntegrationResponse> SendAsync(string type, object payload, CancellationToken cancellation)
        {
            bool interactive = MessageTypes.IsInteractive(type);
            string payloadText = payload == null ? "null" : "new " + payload.GetType().Name + " {...}";
            Write(OutputFormatter.FormatApiCall($"client.SendAsync(MessageTypes.{type}, {payloadText}, owner: {(interactive ? "0x" + OwnerWindow.ToString("X") : "0")})"));
            if (ShowJson && payload != null)
            {
                Write(OutputFormatter.FormatJson("request payload", IntegrationJson.ToJObject(payload)));
            }
            if (interactive)
            {
                Write(OutputFormatter.FormatInfo("HSMAdvisor shows a dialog now. Finish it there, or click \"■ Cancel\" here to close it."));
            }

            var sw = Stopwatch.StartNew();
            var response = await Session.Client.SendAsync(type, payload, interactive ? OwnerWindow : 0, cancellation, launchIfNeeded: true);
            string error = response.Error == null ? "" : $" [{response.Error.Code}] {response.Error.Message}";
            Write($"     Status: {response.Status}{error} ({sw.ElapsedMilliseconds} ms)\n");
            if (ShowJson && response.Payload != null)
            {
                Write(OutputFormatter.FormatJson("response payload", response.Payload));
            }
            return response;
        }

        /// <summary>Reference data loaded once per session, for tasks that only need names.</summary>
        public async Task<ReferenceData> GetReferenceDataAsync()
        {
            if (Session.Reference == null)
            {
                Session.Reference = await RequestAsync<ReferenceData>(MessageTypes.GetReferenceData);
            }
            return Session.Reference;
        }
    }

    /// <summary>
    /// Helper class for formatting output
    /// </summary>
    public static class OutputFormatter
    {
        /// <summary>Longest JSON block printed; reference data can be several hundred KB.</summary>
        public const int MaxJsonChars = 6000;

        public static string FormatHeader(string text)
        {
            return $"\n{'='.ToString().PadRight(60, '=')}\n{text}\n{'='.ToString().PadRight(60, '=')}\n";
        }

        public static string FormatSection(string title)
        {
            return $"\n--- {title} ---\n";
        }

        public static string FormatProperty(string name, object value)
        {
            return $"  {name}: {value}\n";
        }

        public static string FormatSuccess(string message)
        {
            return $"✓ SUCCESS: {message}\n";
        }

        public static string FormatError(string message)
        {
            return $"✗ ERROR: {message}\n";
        }

        public static string FormatWarning(string message)
        {
            return $"⚠ WARNING: {message}\n";
        }

        public static string FormatInfo(string message)
        {
            return $"ℹ INFO: {message}\n";
        }

        public static string FormatStep(int step, string description)
        {
            return $"\n[Step {step}] {description}\n";
        }

        public static string FormatApiCall(string methodCall)
        {
            return $"  → API: {methodCall}\n";
        }

        public static string FormatApiCallWithReturn(string methodCall, object returnValue)
        {
            return $"  → API: {methodCall}\n     Returns: {returnValue}\n";
        }

        public static string FormatApiProperty(string propertyName, object value)
        {
            return $"  → API: {propertyName} = {value}\n";
        }

        public static string FormatJson(string title, JToken json)
        {
            string text = json == null ? "null" : json.ToString(Formatting.Indented);
            if (text.Length > MaxJsonChars)
            {
                text = text.Substring(0, MaxJsonChars) + $"\n... ({text.Length - MaxJsonChars:N0} more characters)";
            }
            var sb = new StringBuilder();
            sb.Append($"     {{ }} {title}:\n");
            foreach (var line in text.Split('\n'))
            {
                sb.Append("       ").Append(line.TrimEnd('\r')).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Fixed-width text table.</summary>
        public static string FormatTable(IEnumerable<string> headers, IEnumerable<IEnumerable<object>> rows)
        {
            var head = headers.ToList();
            var cells = rows.Select(r => r.Select(c => c == null ? "" : Convert.ToString(c, CultureInfo.CurrentCulture)).ToList()).ToList();
            var widths = head.Select((h, i) => Math.Max(h.Length, cells.Count == 0 ? 0 : cells.Max(r => i < r.Count ? r[i].Length : 0))).ToList();

            var sb = new StringBuilder();
            sb.Append("  ").Append(string.Join("  ", head.Select((h, i) => h.PadRight(widths[i])))).Append('\n');
            sb.Append("  ").Append(string.Join("  ", widths.Select(w => new string('-', w)))).Append('\n');
            foreach (var r in cells)
            {
                sb.Append("  ").Append(string.Join("  ", r.Select((c, i) => c.PadRight(widths[i])))).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>The speeds, feeds and cut data HSMAdvisor returned, in the units given by each value's *_m flag.</summary>
        public static string FormatCalculation(CalculationData calc)
        {
            var t = calc.Tool;
            var c = calc.Cut;
            var r = calc.Results ?? new CalculationResults();
            var sb = new StringBuilder();

            sb.Append(FormatSection("Tool"));
            sb.Append(FormatProperty("Type", string.IsNullOrEmpty(r.ToolTypeName) ? t.Tool_type_id.ToString() : r.ToolTypeName));
            if (!string.IsNullOrEmpty(t.Comment))
            {
                sb.Append(FormatProperty("Name", t.Comment));
            }
            sb.Append(FormatProperty("Material / Coating", $"{t.Tool_material_id} / {t.Coating_id}"));
            sb.Append(FormatProperty("Diameter", Length(c.Diameter > 0 ? c.Diameter : t.Diameter, c.Diameter > 0 ? c.Diameter_m : t.Diameter_m)));
            sb.Append(FormatProperty("Flutes", t.Flute_N));
            sb.Append(FormatProperty("Stickout", Length(c.Stickout > 0 ? c.Stickout : t.Stickout, c.Stickout > 0 ? c.Stickout_m : t.Stickout_m)));

            sb.Append(FormatSection("Cut"));
            sb.Append(FormatProperty("Workpiece material", string.IsNullOrEmpty(r.MaterialName) ? "#" + c.Material_id : $"{r.MaterialName} (#{c.Material_id})"));
            if (!string.IsNullOrEmpty(r.MachineName))
            {
                sb.Append(FormatProperty("Machine", r.MachineName));
            }
            if (c.Doc > 0)
            {
                sb.Append(FormatProperty("Depth of cut (DOC)", Length(c.Doc, c.Doc_m)));
            }
            if (c.Woc > 0)
            {
                sb.Append(FormatProperty("Width of cut (WOC)", Length(c.Woc, c.Woc_m)));
            }
            if (c.Depth > 0)
            {
                sb.Append(FormatProperty("Hole depth", Length(c.Depth, c.Depth_m)));
            }
            if (c.Peck > 0)
            {
                sb.Append(FormatProperty("Peck", Length(c.Peck, c.Peck_m)));
            }
            sb.Append(FormatProperty("High speed machining (high_speed_machining)", c.HSM));
            sb.Append(FormatProperty("Chip thinning (chip_thinning)", c.Chip_Thinning));

            sb.Append(FormatSection("Speeds and feeds"));
            sb.Append(FormatProperty("RPM", c.Rpm.ToString("N0")));
            sb.Append(FormatProperty("Surface speed", c.Sfm_m ? $"{c.Sfm:0.#} m/min" : $"{c.Sfm:0.#} SFM"));
            sb.Append(FormatProperty("Feed per tooth", c.Ipt_m ? $"{c.Ipt:0.####} mm" : $"{c.Ipt:0.#####} in"));
            if (r.ChipThickness > 0)
            {
                sb.Append(FormatProperty("Chip thickness", c.Ipt_m ? $"{r.ChipThickness:0.####} mm" : $"{r.ChipThickness:0.#####} in"));
            }
            sb.Append(FormatProperty("Feed rate", c.Feed_m ? $"{c.Feed:0.#} mm/min" : $"{c.Feed:0.##} in/min"));
            if (c.Feed_plunge > 0)
            {
                sb.Append(FormatProperty("Plunge feed", c.Feed_m ? $"{c.Feed_plunge:0.#} mm/min" : $"{c.Feed_plunge:0.##} in/min"));
            }

            sb.Append(FormatSection("Cut analysis"));
            sb.Append(FormatProperty("Metal removal rate", c.Result_units_m ? $"{c.Mrr:0.##} cm³/min" : $"{c.Mrr:0.###} in³/min"));
            sb.Append(FormatProperty("Tool deflection", c.Result_units_m ? $"{c.Tool_deflection:0.####} mm" : $"{c.Tool_deflection:0.#####} in"));
            if (c.Maxdeflection_pc >= 0)
            {
                sb.Append(FormatProperty("Max deflection setting", $"{c.Maxdeflection_pc:0.#} %"));
            }
            if (c.Maxtorque_pc >= 0)
            {
                sb.Append(FormatProperty("Max torque setting", $"{c.Maxtorque_pc:0.#} %"));
            }
            sb.Append(FormatProperty("Units", c.Result_units_m ? "metric" : "imperial"));
            return sb.ToString();
        }

        public static string Length(double value, bool metric)
        {
            return metric ? $"{value:0.###} mm" : $"{value:0.####} in";
        }
    }
}
