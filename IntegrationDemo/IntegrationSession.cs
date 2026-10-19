using System;
using System.Reflection;
using HSMAdvisor.Integration.Client;
using HSMAdvisor.Integration.Protocol;

namespace IntegrationDemo
{
    /// <summary>
    /// The demo's connection to the separately installed HSMAdvisor application.
    ///
    /// One <see cref="HSMAdvisorClient"/> is kept for the life of the app (it remembers which HSMAdvisor
    /// process it already version-checked). Each request opens its own pipe connection; HSMAdvisor is
    /// started in integration mode on the first request and exits a few seconds after this app closes.
    /// </summary>
    public sealed class IntegrationSession
    {
        public const string ClientName = "HSMAdvisor.Integration Demo";

        /// <summary>
        /// Oldest HSMAdvisor this demo works with: the first release with the integration server.
        /// An older HSMAdvisor fails every request with <see cref="HSMAdvisorClient.VersionTooOldCode"/>.
        /// </summary>
        public static readonly Version MinimumHSMAdvisorVersion = new Version(3, 2, 0);

        public IntegrationSession()
        {
            Client = new HSMAdvisorClient(new ClientInfo { Name = ClientName, Version = AppVersion })
            {
                MinimumVersion = MinimumHSMAdvisorVersion,
            };
        }

        public HSMAdvisorClient Client { get; private set; }

        /// <summary>Cached by <see cref="TaskContext.GetReferenceDataAsync"/>.</summary>
        public ReferenceData Reference { get; set; }

        public static string AppVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(3); }
        }

        /// <summary>Version of the referenced HSMAdvisor.Integration.dll.</summary>
        public static string IntegrationVersion
        {
            get { return typeof(HSMAdvisorClient).Assembly.GetName().Version.ToString(); }
        }

        /// <summary>The HSMAdvisor.exe the client starts: the explicit path, or the installed one.</summary>
        public string ResolveExecutable()
        {
            return string.IsNullOrEmpty(Client.ExecutablePath) ? HSMAdvisorLocator.FindExecutable() : Client.ExecutablePath;
        }
    }
}
