using System.Globalization;

namespace Admin.Shared.Utils;

public class BasePaginator : IParsable<BasePaginator>
{
    public int Page { get; set; } = 1;
    public int ItemsPerPage { get; set; } = 50;
    public SortOrder SortOrder { get; set; } = 0;
    public string? OrderBy { get; set; }
    public string? Query { get; set; }

    public virtual string GetAsParams()
    {
        return $"Page={Page}&ItemsPerPage={ItemsPerPage}&SortOrder={(int)SortOrder}&OrderBy={OrderBy}&Query={Query}";
    }

    // Implementation of IParsable<BasePaginator>
    public static BasePaginator Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"Could not parse '{s}' as {nameof(BasePaginator)}");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out BasePaginator result)
    {
        result = new BasePaginator();

        if (string.IsNullOrEmpty(s))
            return true;  // Return default instance

        try
        {
            // Parse from query string format
            // Example: "Page=2&ItemsPerPage=10&SortOrder=1&OrderBy=Name&Query=test"
            var queryParams = s.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in queryParams)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length != 2)
                    continue;

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "page":
                        if (int.TryParse(value, out int page))
                            result.Page = page;
                        break;
                    case "itemsperpage":
                        if (int.TryParse(value, out int itemsPerPage))
                            result.ItemsPerPage = itemsPerPage;
                        break;
                    case "sortorder":
                        if (int.TryParse(value, out int sortOrder) && Enum.IsDefined(typeof(SortOrder), sortOrder))
                            result.SortOrder = (SortOrder)sortOrder;
                        break;
                    case "orderby":
                        result.OrderBy = value;
                        break;
                    case "query":
                        result.Query = value;
                        break;
                }
            }

            return true;
        }
        catch
        {
            // If any exception occurs, return false
            return false;
        }
    }
}

public enum SortOrder
{
    Desc = 1,
    Asc = 0
}