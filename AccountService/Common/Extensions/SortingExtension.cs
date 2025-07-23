using AccountService.Common.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Common.Extensions;

public static class SortingExtension
{
    public static IQueryable<T> Sort<T>(this IQueryable<T> query, IEnumerable<SortOrder> sortRequests)
    {
        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var (propertyName, isDescending) in sortRequests)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                continue;

            var propertyInfo = typeof(T).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo == null)
                continue;

            if (orderedQuery == null)
                orderedQuery = isDescending
                    ? query.OrderByDescending(entity => EF.Property<object>(entity!, propertyInfo.Name))
                    : query.OrderBy(entity => EF.Property<object>(entity!, propertyInfo.Name));
            else
                orderedQuery = isDescending
                    ? orderedQuery.ThenByDescending(entity => EF.Property<object>(entity!, propertyInfo.Name))
                    : orderedQuery.ThenBy(entity => EF.Property<object>(entity!, propertyInfo.Name));
        }

        return orderedQuery ?? query;
    }
}