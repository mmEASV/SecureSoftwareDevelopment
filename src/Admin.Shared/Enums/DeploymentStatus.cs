namespace Admin.Shared.Enums;

/// <summary>
/// Status of an update deployment to a device
/// </summary>
public enum DeploymentStatus
{
    /// <summary>
    /// Deployment scheduled but not started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Update file is being downloaded
    /// </summary>
    Downloading = 1,

    /// <summary>
    /// Update is being installed on device
    /// </summary>
    Installing = 2,

    /// <summary>
    /// Update installed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Deployment failed - see error message
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Client postponed the deployment (CRA opt-out mechanism)
    /// </summary>
    Postponed = 5,

    /// <summary>
    /// Deployment cancelled by user
    /// </summary>
    Cancelled = 6
}
