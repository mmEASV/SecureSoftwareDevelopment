namespace Admin.Web.Application.Services;

public class TenantRouteService
{
    public Guid? TenantId { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? WorkplaceId { get; set; }

    public Guid? DeviceOnWorkplaceId { get; set; }

    public event Action? OnParametersChanged;


    public void SetParams(Guid? tenantId = null, Guid? locationId = null, Guid? workplaceId = null, Guid? deviceOnWorkplaceId = null)
    {
        var changed = false;
        if (tenantId != TenantId)
        {
            changed = true;
            TenantId = tenantId != Guid.Empty ? tenantId : null;
        }

        if (locationId != LocationId)
        {
            changed = true;
            LocationId = locationId != Guid.Empty ? locationId : null;
        }

        if (workplaceId != WorkplaceId)
        {
            changed = true;
            WorkplaceId = workplaceId != Guid.Empty ? workplaceId : null;
        }

        if (deviceOnWorkplaceId != DeviceOnWorkplaceId)
        {
            changed = true;
            DeviceOnWorkplaceId = deviceOnWorkplaceId != Guid.Empty ? deviceOnWorkplaceId : null;
        }

        if (changed)
        {
            // Raise the event to notify subscribers
            OnParametersChanged?.Invoke();
        }
    }
}
