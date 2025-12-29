using Blazored.Modal;
using Template.Shared.Dto;
using Template.Shared.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Blazored.Modal.Services;
using Template.Web.Shared.Components.Base;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Template.Web.Application.Services;
using IToastService = Blazored.Toast.Services.IToastService;

namespace Template.Web.Shared.Components.PaginatedListView;

public partial class PaginatedListView<T> : CustomComponentBase
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IHttpService HttpService { get; set; } = null!;
    [Inject] public IDialogService DialogService { get; set; } = null!;
    [Inject] public IToastService ToastService { get; set; } = null!;
    [Inject] public IModalService ModalService { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    /// <summary>
    /// Data query result
    /// </summary>
    private IQueryable<T>? _dataQuery;

    /// <summary>
    /// Paginated list of data returned from server
    /// </summary>
    private PaginatedListDto<T> _paginatedList = new PaginatedListDto<T>(new PaginatedList<T>(new List<T>(), 0, 0, 0, 0));

    /// <summary>
    /// Paginator for data
    /// </summary>
    [Parameter]
    public BasePaginator Paginator { get; set; } = new BasePaginator();

    /// <summary>
    /// Search query for data
    /// </summary>
    private string? _query;

    /// <summary>
    /// Number of items per page
    /// </summary>
    private string _numOfItems = "50";

    /// <summary>
    /// Uri for getting data from API
    /// </summary>
    [Parameter]
    public required string GetUrl { get; set; }

    /// <summary>
    /// Edit uri for redirecting to edit page
    /// </summary>
    [Parameter]
    public string? EditUrl { get; set; }

    /// <summary>
    /// Parameter for edit url
    /// </summary>
    [Parameter]
    public string RouteParam { get; set; } = "Id";

    /// <summary>
    /// Parameter for details url
    /// </summary>
    [Parameter]
    public string DetailsParam { get; set; } = "Id";

    // Method to open the delete dialog
    public void OpenDeleteDialog(object item)
    {
        var parameters = new ModalParameters();
        parameters.Add(nameof(DeleteDialog.OnDelete),
            EventCallback.Factory.Create<MouseEventArgs>(this, _ => DeleteItem(item)));

        var options = new ModalOptions
        {
            Size = ModalSize.Small,
            Position = ModalPosition.Middle,
            AnimationType = ModalAnimationType.FadeInOut
        };

        ModalService.Show<DeleteDialog>("Delete Confirmation", parameters, options);
    }

    // Implement the DeleteItem method that will handle the actual deletion
    private void DeleteItem(object item)
    {
        try
        {
            OpenDeleteDialog(item);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// If exists, Url for creating new item
    /// </summary>
    [Parameter]
    public string? CreateUrl { get; set; }

    /// <summary>
    /// Edit uri for redirecting to edit page
    /// </summary>
    [Parameter]
    public string? DeleteUrl { get; set; }

    /// <summary>
    /// Details uri for redirecting to details page
    /// </summary>
    [Parameter]
    public string? DetailsUrl { get; set; }

    /// <summary>
    /// Cancellation token for API call
    /// </summary>
    [Parameter]
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered inside the component.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Represents a component that displays a paginated list of items and provides actions for manipulating the data.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? Actions { get; set; }

    /// <summary>
    /// Gets or sets the string representation of the grid template columns.
    /// </summary>
    /// <remarks>
    /// The grid template columns property allows you to define the columns of a CSS grid
    /// layout using a string representation. Each column can be defined using a specific
    /// size or a keyword.
    /// The format of the string representation is as follows:
    /// - Each column is defined using a value that represents the width or size of the column.
    /// - Multiple columns are separated by a whitespace or comma.
    /// - Columns can be defined using a fixed size value, such as pixels ("px"), percentage ("%"),
    /// or a keyword such as "auto" or "minmax()".
    /// Examples of valid grid template columns string representations include:
    /// - "100px 200px 300px" - Defines three columns with fixed sizes of 100 pixels, 200 pixels, and 300 pixels.
    /// - "20% auto" - Defines two columns, one with a width of 20% and the other taking up the remaining space.
    /// - "1fr 2fr 1fr" - Defines three columns with relative sizes of 1, 2, and 1 respectively.
    /// </remarks>
    [Parameter]
    public string? GridTemplateColumns { get; set; }

    /// <summary>
    /// Indicates whether the current device is a mobile device.
    /// </summary>
    public bool IsMobile { get; set; }

    /// <summary>
    /// Render fragment designed to display the mobile view layout for items of type <typeparamref name="T"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? MobileView { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsMobile = await JsRuntime.InvokeAsync<bool>("isMobile", CancellationToken);

        await LoadData();
    }

    public async Task RefreshData()
    {
        await LoadData();
    }

    /// <summary>
    /// Load data from API
    /// </summary>
    private async Task LoadData()
    {
        if (_query != null)
            Paginator.Query = _query;
        Paginator.ItemsPerPage = int.Parse(_numOfItems);
        _paginatedList = await HttpService.Get<PaginatedListDto<T>>($"{GetUrl}?{Paginator.GetAsParams()}", CancellationToken) ??
                         new PaginatedListDto<T>(new PaginatedList<T>(new List<T>(), 0, 0, 0, 0));
        _dataQuery = _paginatedList.Items.AsQueryable();
    }

    /// <summary>
    /// Load next page of data
    /// </summary>
    private async Task NextPage()
    {
        Paginator.Page += 1;
        await LoadData();
    }

    /// <summary>
    /// Load previous page of data
    /// </summary>
    private async Task PrevPage()
    {
        Paginator.Page -= 1;
        await LoadData();
    }

    /// <summary>
    /// Load first page of data
    /// </summary>
    private async Task FirstPage()
    {
        Paginator.Page = 1;
        await LoadData();
    }

    /// <summary>
    /// Load last page of data
    /// </summary>
    private async Task LastPage()
    {
        Paginator.Page = _paginatedList.TotalPages;
        await LoadData();
    }

    /// <summary>
    /// Redirect to edit page
    /// </summary>
    /// <param name="item"></param>
    private void RedirectToEdit(T item)
    {
        var id = item?.GetType().GetProperty(RouteParam)?.GetValue(item, null);
        if (id is not null)
        {
            if (EditUrl is not null && EditUrl.Contains("{" + RouteParam + "}"))
            {
                NavigationManager.NavigateTo(EditUrl.Replace("{" + RouteParam + "}", $"{id}"));
            }
            else
            {
                NavigationManager.NavigateTo($"{EditUrl}/{id}");
            }
        }
    }

    /// <summary>
    /// Redirect to details page
    /// </summary>
    /// <param name="item"></param>
    private void RedirectToDetails(T item)
    {
        var id = item?.GetType().GetProperty(DetailsParam)?.GetValue(item, null);
        if (id is not null)
        {
            NavigationManager.NavigateTo($"{DetailsUrl}/{id}");
        }
    }

    /// <summary>
    /// Redirect to create page
    /// </summary>
    private void RedirectToCreate()
    {
        NavigationManager.NavigateTo($"{CreateUrl}");
    }

    private async Task Delete(T item)
    {
        DialogParameters parameters = new()
        {
            Title = "Prajete is naozaj vymazať?",
            PrimaryAction = "Vymazať",
            SecondaryAction = "Vrátiť sa späť",
            Width = "500px",
            PreventScroll = true
        };
        var dialog = await DialogService.ShowDialogAsync<DeleteDialog>(parameters);
        var result = await dialog.Result;

        if (result.Cancelled)
            return;

        var id = item?.GetType().GetProperty(RouteParam)?.GetValue(item, null);
        await HttpService.Delete($"{DeleteUrl}/{id}");

        ToastService.ShowSuccess("Položka bola vymazaná");
        await LoadData();
    }
}
