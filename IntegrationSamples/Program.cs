using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;
using IntegrationSamples.Samples;

namespace IntegrationSamples
{
    internal static class Program
    {
        private static readonly ISample[] AllSamples =
        {
            new ConnectSample(),
            new ReferenceDataSample(),
            new CalculateMillingSample(),
            new CalculateDrillingSample(),
            new MachineProfileSample(),
            new MetricConversionSample(),
            new EditCalculationDialogSample(),
            new PickerDialogsSample(),
            new ToolLibrarySample(),
            new TranslateSample(),
            new CancellationAndErrorsSample(),
            new RawPipeProtocolSample(),
        };

        /// <summary>
        /// Usage: HSMAdvisor.Integration-Samples [sample number]
        /// Without an argument a menu lists the samples.
        /// </summary>
        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Keep one client for the life of your application. Each request opens its own pipe
            // connection; the first one starts HSMAdvisor in integration mode (no main window), and
            // HSMAdvisor exits a few seconds after this process does.
            var client = new HSMAdvisorClient(new ClientInfo
            {
                Name = "HSMAdvisor.Integration Samples",
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3),
            })
            {
                // An older HSMAdvisor fails every request with IntegrationException code "version_too_old"
                MinimumVersion = new Version(3, 2, 0),
            };

            if (args.Length > 0)
            {
                int number;
                if (!int.TryParse(args[0], out number) || number < 1 || number > AllSamples.Length)
                {
                    Console.Error.WriteLine($"Sample number must be 1-{AllSamples.Length}.");
                    return 1;
                }
                return await RunAsync(client, AllSamples[number - 1]) ? 0 : 1;
            }

            while (true)
            {
                Console.WriteLine();
                for (int i = 0; i < AllSamples.Length; i++)
                {
                    Console.WriteLine($"{i + 1,3}. {AllSamples[i].Title}");
                }
                Console.Write("Sample number (q to quit): ");
                string input = Console.ReadLine();
                if (input == null || input.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                int choice;
                if (int.TryParse(input, out choice) && choice >= 1 && choice <= AllSamples.Length)
                {
                    await RunAsync(client, AllSamples[choice - 1]);
                }
            }
        }

        private static async Task<bool> RunAsync(HSMAdvisorClient client, ISample sample)
        {
            Console.WriteLine();
            Console.WriteLine($"== {sample.Title} ==  (Ctrl+C cancels)");

            using (var cts = new CancellationTokenSource())
            {
                ConsoleCancelEventHandler onCancel = (s, e) =>
                {
                    e.Cancel = true; // keep the process alive, cancel only the running sample
                    cts.Cancel();
                };
                Console.CancelKeyPress += onCancel;
                try
                {
                    await sample.RunAsync(client, cts.Token);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Cancelled.");
                }
                catch (IntegrationException ex)
                {
                    // Error and busy responses, plus client-side failures such as
                    // "not_installed" (HSMAdvisor.exe not found) and "version_too_old"
                    Console.WriteLine($"HSMAdvisor error [{ex.Code}]: {ex.Message}");
                }
                finally
                {
                    Console.CancelKeyPress -= onCancel;
                }
                return false;
            }
        }
    }
}
