
using Microsoft.EntityFrameworkCore;

namespace Admin.Shared.Utils;

public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalItems { get; }
    public int ItemsPerPage { get; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList(List<T> items, int totalPages, int totalItems, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = totalPages;
        TotalItems = totalItems;
        ItemsPerPage = pageSize;

        AddRange(items);
    }

    // A CreateAsync method is used instead of a constructor to create the PaginatedList<T>
    // object because constructors can't run asynchronous code.
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize, CancellationToken token)
    {
        var count = await source.CountAsync(token);
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);

        return new PaginatedList<T>(items, totalPages, count, pageIndex, pageSize);
    }
}
