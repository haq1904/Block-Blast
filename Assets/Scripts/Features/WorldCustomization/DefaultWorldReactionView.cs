using UnityEngine;

namespace BlockBlast.Features.WorldCustomization
{
    /// <summary>
    /// Default world gameplay reactor attached to DefaultWorld prefab parts.
    /// Resets presentation state without referencing or allocating placeholder VFX assets.
    /// </summary>
    public sealed class DefaultWorldReactionView : MonoBehaviour, IWorldGameplayReactor
    {
        public void PlayLineClear(in WorldLineClearContext context)
        {
            // Default visual world contains passive scenery; line clear presentation is handled by grid.
        }

        public void PlayTableClear(in WorldTableClearContext context)
        {
            // Default visual world contains passive scenery.
        }

        public void PlayComboChanged(in WorldComboContext context)
        {
            // Default visual world contains passive scenery.
        }

        public void PlayGameOver()
        {
            // Default visual world contains passive scenery.
        }

        public void PlayStressStateChanged(in WorldStressContext context)
        {
            // Default visual world contains passive scenery.
        }

        public void ResetPresentation()
        {
            // Reset presentation state cleanly.
        }
    }
}
