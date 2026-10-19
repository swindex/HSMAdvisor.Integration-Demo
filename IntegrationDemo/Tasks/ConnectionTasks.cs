using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;
using Newtonsoft.Json.Linq;

namespace IntegrationDemo.Tasks
{
    public class LocateHSMAdvisorTask : DemoTask
    {
        public LocateHSMAdvisorTask()
        {
            Name = "Locate HSMAdvisor Installation";
            Description = "Finds the installed HSMAdvisor.exe the way the client does before starting it: the HSMADVISOR_EXE environment variable, the installer's registry entries, then the default install folders. Also checks its file version against the minimum this integration needs.";
            Category = "Connection";
        }

        public override Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("LOCATE HSMADVISOR"));

            ctx.Write(OutputFormatter.FormatStep(1, "Checking the environment variable override..."));
            string env = Environment.GetEnvironmentVariable(HSMAdvisorLocator.EnvironmentVariable);
            ctx.Write(OutputFormatter.FormatApiCallWithReturn($"Environment.GetEnvironmentVariable(\"{HSMAdvisorLocator.EnvironmentVariable}\")", env ?? "null"));

            ctx.Write(OutputFormatter.FormatStep(2, "Searching for HSMAdvisor.exe..."));
            string exe = HSMAdvisorLocator.FindExecutable();
            ctx.Write(OutputFormatter.FormatApiCallWithReturn("HSMAdvisorLocator.FindExecutable()", exe ?? "null"));
            ctx.Write(OutputFormatter.FormatInfo($"Registry key read by the locator: HKCU/HKLM\\{HSMAdvisorLocator.RegistryKeyPath}\\{HSMAdvisorLocator.RegistryValue}"));

            if (!string.IsNullOrEmpty(ctx.Session.Client.ExecutablePath))
            {
                ctx.Write(OutputFormatter.FormatApiProperty("client.ExecutablePath", ctx.Session.Client.ExecutablePath));
                ctx.Write(OutputFormatter.FormatInfo("The client starts this explicit path instead of the located one."));
                exe = ctx.Session.Client.ExecutablePath;
            }

            if (exe == null || !File.Exists(exe))
            {
                ctx.Write(OutputFormatter.FormatError("HSMAdvisor.exe was not found."));
                ctx.Write(OutputFormatter.FormatInfo("Download HSMAdvisor from " + HSMAdvisorClient.DownloadUrl));
                return Task.CompletedTask;
            }

            ctx.Write(OutputFormatter.FormatStep(3, "Checking the executable version..."));
            var version = HSMAdvisorClient.GetExecutableVersion(exe);
            ctx.Write(OutputFormatter.FormatApiCallWithReturn("HSMAdvisorClient.GetExecutableVersion(exe)", version?.ToString() ?? "null"));
            bool tooOld = version != null && HSMAdvisorClient.IsOlder(version, IntegrationSession.MinimumHSMAdvisorVersion);
            ctx.Write(OutputFormatter.FormatApiCallWithReturn($"HSMAdvisorClient.IsOlder({version}, {IntegrationSession.MinimumHSMAdvisorVersion})", tooOld));

            ctx.Write(OutputFormatter.FormatSection("Pipe"));
            ctx.Write(OutputFormatter.FormatApiCallWithReturn("IntegrationProtocol.GetPipeName()", IntegrationProtocol.GetPipeName()));
            ctx.Write(OutputFormatter.FormatProperty("Protocol version", IntegrationProtocol.Version));
            ctx.Write(OutputFormatter.FormatProperty("Launch arguments", $"{IntegrationProtocol.IntegrationArgument} {IntegrationProtocol.ParentPidArgumentPrefix}<this process id>"));

            ctx.Write("\n");
            if (tooOld)
            {
                ctx.Write(OutputFormatter.FormatWarning($"HSMAdvisor {version} is older than {IntegrationSession.MinimumHSMAdvisorVersion}. Requests will fail with '{HSMAdvisorClient.VersionTooOldCode}'."));
            }
            else
            {
                ctx.Write(OutputFormatter.FormatSuccess("HSMAdvisor is installed and supports the integration protocol."));
            }
            return Task.CompletedTask;
        }
    }

    public class CheckRunningTask : DemoTask
    {
        public CheckRunningTask()
        {
            Name = "Check If HSMAdvisor Is Running";
            Description = "Tries to connect to the integration pipe of the current Windows session without starting HSMAdvisor. Useful to decide whether a request will be instant or will first launch HSMAdvisor.";
            Category = "Connection";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("IS HSMADVISOR RUNNING?"));

            ctx.Write(OutputFormatter.FormatStep(1, "Probing the integration pipe (300 ms timeout)..."));
            bool isRunning = await ctx.Session.Client.IsRunningAsync(ctx.Cancellation);
            ctx.Write(OutputFormatter.FormatApiCallWithReturn("client.IsRunningAsync(ct)", isRunning));

            ctx.Write("\n");
            if (isRunning)
            {
                ctx.Write(OutputFormatter.FormatSuccess("An HSMAdvisor instance is listening in this session."));
            }
            else
            {
                ctx.Write(OutputFormatter.FormatInfo("HSMAdvisor is not running. The next request (with launchIfNeeded: true) starts it in integration mode."));
            }
        }
    }

    public class HelloTask : DemoTask
    {
        public HelloTask()
        {
            Name = "Connect (Hello Handshake)";
            Description = "Sends the Hello message, starting HSMAdvisor in integration mode (no main window) when it is not running. Returns the HSMAdvisor version, protocol version, data folder and process id.";
            Category = "Connection";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("HELLO"));

            bool launched = false;
            EventHandler onLaunch = (s, e) =>
            {
                launched = true;
                ctx.Write(OutputFormatter.FormatInfo("client.Launching: HSMAdvisor was not running, starting it now..."));
            };

            ctx.Write(OutputFormatter.FormatStep(1, "Sending Hello..."));
            ctx.Session.Client.Launching += onLaunch;
            HelloResponse hello;
            try
            {
                hello = await ctx.RequestAsync<HelloResponse>(MessageTypes.Hello);
            }
            finally
            {
                ctx.Session.Client.Launching -= onLaunch;
            }

            ctx.Write(OutputFormatter.FormatSection("HelloResponse"));
            ctx.Write(OutputFormatter.FormatProperty("AppVersion", hello.AppVersion));
            ctx.Write(OutputFormatter.FormatProperty("Protocol", hello.Protocol));
            ctx.Write(OutputFormatter.FormatProperty("DataPath", hello.DataPath));
            ctx.Write(OutputFormatter.FormatProperty("ProcessId", hello.ProcessId));
            ctx.Write(OutputFormatter.FormatProperty("MainWindowOpen", hello.MainWindowOpen));
            ctx.Write(OutputFormatter.FormatProperty("Started by this request", launched));

            ctx.Write(OutputFormatter.FormatStep(2, "Checking the version..."));
            Version version;
            if (Version.TryParse(hello.AppVersion, out version))
            {
                bool tooOld = HSMAdvisorClient.IsOlder(version, IntegrationSession.MinimumHSMAdvisorVersion);
                ctx.Write(OutputFormatter.FormatApiCallWithReturn($"HSMAdvisorClient.IsOlder({version}, {IntegrationSession.MinimumHSMAdvisorVersion})", tooOld));
                ctx.Write("\n");
                ctx.Write(tooOld
                    ? OutputFormatter.FormatWarning("This HSMAdvisor is too old for this integration.")
                    : OutputFormatter.FormatSuccess($"Connected to HSMAdvisor {hello.AppVersion}."));
            }
            ctx.Write(OutputFormatter.FormatInfo("Hello skips the client's automatic version check. Every other request checks it once per HSMAdvisor process."));
        }
    }

    public class ShowMainWindowTask : DemoTask
    {
        public ShowMainWindowTask()
        {
            Name = "Show HSMAdvisor Main Window";
            Description = "Asks the running HSMAdvisor to open (or bring to the front) its full main window, e.g. for a \"Open HSMAdvisor\" menu item in your application.";
            Category = "Connection";
        }

        public override async Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            ctx.Write(OutputFormatter.FormatHeader("SHOW MAIN WINDOW"));

            ctx.Write(OutputFormatter.FormatStep(1, "Sending ShowMainWindow..."));
            await ctx.RequestAsync<JObject>(MessageTypes.ShowMainWindow); // no response payload

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("HSMAdvisor's main window should now be open."));
            ctx.Write(OutputFormatter.FormatInfo("Closing it does not stop HSMAdvisor: an integration-mode instance stays resident while this demo runs."));
        }
    }

    public class SetExecutablePathTask : DemoTask
    {
        public SetExecutablePathTask()
        {
            Name = "Use a Specific HSMAdvisor.exe";
            Description = "Sets HSMAdvisorClient.ExecutablePath, the HSMAdvisor.exe started when no instance is running (e.g. a portable copy or a development build). Leave empty to use the installed HSMAdvisor. An instance that is already running is used as is.";
            Category = "Connection";

            Parameters.Add(new TaskParameter
            {
                Name = "Path",
                Label = "HSMAdvisor.exe",
                ParameterType = typeof(string),
                DefaultValue = HSMAdvisorLocator.FindExecutable() ?? "",
                Description = "Full path to HSMAdvisor.exe. Empty = HSMAdvisorLocator.FindExecutable()."
            });
        }

        public override Task ExecuteAsync(TaskContext ctx, Dictionary<string, object> parameterValues)
        {
            string path = GetParameter<string>(parameterValues, "Path", "").Trim().Trim('"');

            ctx.Write(OutputFormatter.FormatHeader("SET EXECUTABLE PATH"));

            if (path.Length > 0 && !File.Exists(path))
            {
                ctx.Write(OutputFormatter.FormatError($"File not found: {path}"));
                return Task.CompletedTask;
            }

            ctx.Session.Client.ExecutablePath = path.Length == 0 ? null : path;
            ctx.Write(OutputFormatter.FormatApiProperty("client.ExecutablePath", ctx.Session.Client.ExecutablePath ?? "null"));
            ctx.Write(OutputFormatter.FormatProperty("HSMAdvisor.exe to start", ctx.Session.ResolveExecutable() ?? "(not found)"));

            ctx.Write("\n");
            ctx.Write(OutputFormatter.FormatSuccess("The new path is used the next time HSMAdvisor has to be started."));
            return Task.CompletedTask;
        }
    }
}
