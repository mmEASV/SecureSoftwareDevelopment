namespace Admin.Shared.Dto;

public record DeploymentStatistics
{
    public int Total { get; init; }
    public int PendingDeployments { get; init; }
    public int Downloading { get; init; }
    public int Installing { get; init; }
    public int CompletedDeployments { get; init; }
    public int FailedDeployments { get; init; }
    public int Postponed { get; init; }
    public int Cancelled { get; init; }
    public double SuccessRate { get; init; }
}
