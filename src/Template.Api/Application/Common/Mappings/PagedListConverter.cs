using AutoMapper;
using Template.Shared.Utils;

namespace Template.Api.Application.Common.Mappings;

public class PagedListConverter<TIn, TOut> : ITypeConverter<PaginatedList<TIn>, PaginatedList<TOut>>
{

    public PaginatedList<TOut> Convert(PaginatedList<TIn> source, PaginatedList<TOut> destination, ResolutionContext context)
    {
        var mapped = context.Mapper.Map<List<TOut>>(source);

        return new PaginatedList<TOut>(mapped, source.TotalPages, source.TotalItems, source.PageIndex, source.ItemsPerPage);
    }
}
