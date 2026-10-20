using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.GameFlow.Play
{
    /// <summary>
    /// Service contract for covering and revealing the 8x8 play table during scene transitions.
    /// </summary>
    public interface IPlaySceneTransitionVfxService
    {
        /// <summary>
        /// Moves two fog curtains inward to cover the play table.
        /// Resolves once the table is fully covered.
        /// </summary>
        Task CoverTableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the fog curtains immediately to their initial hidden state.
        /// </summary>
        void ResetImmediate();
    }
}
