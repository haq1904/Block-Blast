/// <summary>
/// Defines the visual presentation mode used during a scene transition.
/// </summary>
public enum SceneTransitionPresentation
{
    /// <summary>
    /// The transition operates without visual loading overlay (framebuffer remains clear or untouched).
    /// </summary>
    None,

    /// <summary>
    /// The transition employs the configured ILoadingScreen presenter (cover, progress, reveal).
    /// </summary>
    LoadingScreen
}
