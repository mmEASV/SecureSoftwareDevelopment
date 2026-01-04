namespace Admin.Shared.Enums;

/// <summary>
/// Severity level of an update (important for CRA compliance)
/// </summary>
public enum UpdateSeverity
{
    /// <summary>
    /// Critical security vulnerability - must be applied immediately
    /// </summary>
    Critical = 0,

    /// <summary>
    /// High priority - should be applied within days
    /// </summary>
    High = 1,

    /// <summary>
    /// Medium priority - should be applied within weeks
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Low priority - can be scheduled during regular maintenance
    /// </summary>
    Low = 3
}
