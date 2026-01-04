using System.Text.Json.Serialization;
using Admin.Shared.Utils;

namespace Admin.Shared.Dto;

[method: JsonConstructor]
public class PaginatedListDto<T>(List<T> items, int pageIndex, int totalPages, int totalItems, int itemsPerPage, bool hasPreviousPage, bool hasNextPage)
{
    public List<T> Items { get; } = items;
    public int PageIndex { get; } = pageIndex;
    public int TotalPages { get; } = totalPages;
    public int TotalItems { get; } = totalItems;
    public int ItemsPerPage { get; } = itemsPerPage;
    public bool HasPreviousPage { get; } = hasPreviousPage;
    public bool HasNextPage { get; } = hasNextPage;

    public PaginatedListDto(PaginatedList<T> consumers) : this(consumers, consumers.PageIndex, consumers.TotalPages, consumers.TotalItems, consumers.ItemsPerPage, consumers.HasPreviousPage, consumers.HasNextPage)
    {
    }
}

