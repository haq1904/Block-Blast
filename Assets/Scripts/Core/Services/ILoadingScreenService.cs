using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Generic service contract for executing operations covered by the loading screen.
/// </summary>
public interface ILoadingScreenService
{
    /// <summary>
    /// Covers the screen, executes the provided asynchronous operation, and reveals the screen upon completion.
    /// In case of error or cancellation, the screen is reset immediately and the exception is propagated.
    /// </summary>
    Task RunCoveredOperationAsync(
        Func<CancellationToken, Task> coveredOperation,
        CancellationToken cancellationToken = default);
}
