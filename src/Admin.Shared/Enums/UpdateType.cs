namespace Admin.Shared.Enums;

/// <summary>
/// Type of update being released
/// </summary>
public enum UpdateType
{
    /// <summary>
    /// Security patches and vulnerability fixes (CRA priority)
    /// </summary>
    Security = 0,

    /// <summary>
    /// Bug fixes and stability improvements
    /// </summary>
    BugFix = 1,

    /// <summary>
    /// New features and enhancements
    /// </summary>
    Feature = 2,

    /// <summary>
    /// Performance improvements
    /// </summary>
    Performance = 3
}
