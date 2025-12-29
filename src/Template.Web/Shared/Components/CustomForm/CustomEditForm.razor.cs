using Blazored.Modal;
using Blazored.Modal.Services;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Template.Web.Application.Services;
using Template.Web.Shared.Components.Base;

namespace Template.Web.Shared.Components.CustomForm;

public partial class CustomEditForm<T> : CustomComponentBase
{
    [Inject] public IHttpService HttpService { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IToastService ToastService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IModalService ModalService { get; set; } = null!;

    /// <summary>
    /// Sets the object to be edited.
    /// </summary>
    [Parameter]
    public required object Object { get; set; }

    /// <summary>
    /// Url to put the form to.
    /// </summary>
    [Parameter]
    public required string PutUrl { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered inside the component.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public async Task SubmitForm()
    {
        try
        {
            await HttpService.Put<T>($"{PutUrl}", Object);
            ToastService.ShowSuccess("Edit successful");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Edit failed: {ex.Message}");
        }
    }

    private void OnOpenDeleteDialog()
    {
        var parameters = new ModalParameters();
        parameters.Add(nameof(Modals.DeleteConfirmationModal.OnDeleteConfirmed),
            EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await DeleteObject()));

        var options = new ModalOptions
        {
            Size = ModalSize.Small,
            Position = ModalPosition.Middle,
            AnimationType = ModalAnimationType.FadeInOut
        };

        ModalService.Show<Modals.DeleteConfirmationModal>("Delete Confirmation", parameters, options);
    }

    public async Task DeleteObject()
    {
        try
        {
            await HttpService.Delete($"{PutUrl}", Object);
            ToastService.ShowSuccess($"Delete successful");
            await Task.Delay(1000);
            await JsRuntime.InvokeVoidAsync("history.back");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Delete failed: {ex.Message}");
        }
    }
}
