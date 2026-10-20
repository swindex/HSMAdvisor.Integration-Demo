using System;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationSamples.Samples
{
    /// <summary>
    /// Finds the installed HSMAdvisor, checks its version and connects with the Hello handshake.
    /// Use this at startup to tell the user early that HSMAdvisor is missing or too old.
    /// </summary>
    public sealed class ConnectSample : ISample
    {
        public string Title => "Connect: locate HSMAdvisor and say Hello";

        public async Task RunAsync(HSMAdvisorClient client, CancellationToken ct)
        {
            // 1. Where is HSMAdvisor.exe? The client searches, in order: the HSMADVISOR_EXE environment
            //    variable, the installer's registry value, the uninstall entry, then Program Files.
            //    Set client.ExecutablePath to start a specific copy instead (e.g. a portable install).
            string exe = client.ExecutablePath ?? HSMAdvisorLocator.FindExecutable();
            if (exe == null)
            {
                Console.WriteLine("HSMAdvisor is not installed. Download it from " + HSMAdvisorClient.DownloadUrl);
                return;
            }
            Console.WriteLine($"HSMAdvisor.exe: {exe}");

            // 2. Check the file version before starting it. Requests to an older HSMAdvisor would fail
            //    with IntegrationException code HSMAdvisorClient.VersionTooOldCode.
            Version fileVersion = HSMAdvisorClient.GetExecutableVersion(exe);
            if (fileVersion != null && HSMAdvisorClient.IsOlder(fileVersion, client.MinimumVersion))
            {
                Console.WriteLine($"HSMAdvisor {fileVersion} is older than the required {client.MinimumVersion}. Please update it.");
                return;
            }
            Console.WriteLine($"File version:  {fileVersion}");

            // 3. Is it already running? This only probes the pipe and never starts HSMAdvisor.
            //    If it is not running, the next request starts it, which takes a few seconds.
            bool running = await client.IsRunningAsync(ct);
            Console.WriteLine($"Running:       {running}");

            // The Launching event fires when a request has to start HSMAdvisor first:
            // a good moment to show a "Starting HSMAdvisor..." message.
            EventHandler onLaunching = (s, e) => Console.WriteLine("Starting HSMAdvisor...");
            client.Launching += onLaunching;
            try
            {
                // 4. Hello returns information about the running HSMAdvisor. Only interactive requests
                //    (dialogs) use the owner window, so non-interactive ones pass 0.
                var hello = await client.RequestAsync<HelloResponse>(MessageTypes.Hello, null, 0, ct);

                Console.WriteLine($"App version:   {hello.AppVersion}");
                Console.WriteLine($"Protocol:      {hello.Protocol}");
                Console.WriteLine($"Data folder:   {hello.DataPath}");
                Console.WriteLine($"Process id:    {hello.ProcessId}");
                Console.WriteLine($"Main window:   {(hello.MainWindowOpen ? "open" : "hidden (integration mode)")}");
            }
            finally
            {
                client.Launching -= onLaunching;
            }

            // To open HSMAdvisor's full main window (e.g. from an "Open HSMAdvisor" menu item):
            //   await client.RequestAsync<JObject>(MessageTypes.ShowMainWindow, null, 0, ct);
        }
    }
}
