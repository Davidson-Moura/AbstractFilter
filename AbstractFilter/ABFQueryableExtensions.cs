namespace AbstractFilter
{
    public static class ABFQueryableExtensions
    {
        public static IQueryable<T> ApplyFinder<T>(this IQueryable<T> query, ABFinder<T> finder) where T : class
        {
            if (finder == null) return query;

            if (finder.Filter != null)
            {
                query = query.Where(finder.Filter.ToExpression());
            }

            if (finder.Includes != null && finder.Includes.Any())
            {
                throw new NotSupportedException("Includes não é suportado em IQueryable. Use IEnumerable para incluir propriedades relacionadas.");
            }

            if (finder.Sort != null)
            {
                query = query.ApplySort(finder.Sort);
            }

            if (finder.Skip.HasValue && finder.Skip.Value > 0)
            {
                query = query.Skip(finder.Skip.Value);
            }

            if (finder.Take.HasValue && finder.Take.Value > 0)
            {
                query = query.Take(finder.Take.Value);
            }

            return query;
        }
        public static IQueryable<TResult> ApplyFinder<T, TResult>(this IQueryable<T> query, ABFinder<T, TResult> finder) where T : class
        {
            if (finder == null)
                throw new ArgumentNullException(nameof(finder));

            var appliedQuery = query.ApplyFinder((ABFinder<T>)finder);

            if (finder.Projection != null)
            {
                return appliedQuery.Select(finder.Projection);
            }

            return (IQueryable<TResult>)appliedQuery;
        }
    }
}
