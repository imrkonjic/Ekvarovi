using System.Linq.Expressions;
using EKvarovi.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace EKvarovi.Api.Infrastructure;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        string? sortDir,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> allowed,
        string defaultKey)
    {
        var key = sortBy is not null && allowed.ContainsKey(sortBy) ? sortBy : defaultKey;
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return descending
            ? query.OrderByDescending(allowed[key])
            : query.OrderBy(allowed[key]);
    }

    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TDto>(
        this IQueryable<TDto> query, PagedRequest request, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }
}
