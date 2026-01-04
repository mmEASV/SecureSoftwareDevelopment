using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Admin.Web.Shared.Components.PaginatedListView;

public partial class DeleteDialog : IDialogContentComponent
{
    [CascadingParameter]
    public FluentDialog? Dialog { get; set; }
}
