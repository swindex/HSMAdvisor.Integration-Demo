using System.Threading;
using System.Threading.Tasks;
using HSMAdvisor.Integration.Client;

namespace IntegrationSamples
{
    /// <summary>One runnable code sample.</summary>
    public interface ISample
    {
        string Title { get; }

        /// <param name="client">The application's single HSMAdvisor client.</param>
        /// <param name="ct">Cancelled by Ctrl+C. Cancelling a request closes any HSMAdvisor dialog it opened.</param>
        Task RunAsync(HSMAdvisorClient client, CancellationToken ct);
    }
}
