using System.Linq.Expressions;

namespace AbstractFilter
{
    public enum ABFSortDirection
    {
        Ascending,
        Descending
    }

    public class ABFSortField<T>
    {
        public Expression<Func<T, object>> KeySelector { get; set; }
        public ABFSortDirection Direction { get; set; }
    }

    public interface IABFSortDefinition<T>
    {
        IReadOnlyList<ABFSortField<T>> GetFields();
    }

    internal class ABFSortDefinition<T> : IABFSortDefinition<T>
    {
        private readonly IReadOnlyList<ABFSortField<T>> _fields;

        public ABFSortDefinition(IReadOnlyList<ABFSortField<T>> fields)
        {
            _fields = fields;
        }

        public IReadOnlyList<ABFSortField<T>> GetFields() => _fields;
    }

    public class ABFSortBuilder<T>
    {
        private readonly List<ABFSortField<T>> _fields = new();

        public ABFSortBuilder<T> OrderBy(Expression<Func<T, object>> keySelector)
        {
            _fields.Clear();
            _fields.Add(new ABFSortField<T> { KeySelector = keySelector, Direction = ABFSortDirection.Ascending });
            return this;
        }

        public ABFSortBuilder<T> OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            _fields.Clear();
            _fields.Add(new ABFSortField<T> { KeySelector = keySelector, Direction = ABFSortDirection.Descending });
            return this;
        }

        public ABFSortBuilder<T> ThenBy(Expression<Func<T, object>> keySelector)
        {
            _fields.Add(new ABFSortField<T> { KeySelector = keySelector, Direction = ABFSortDirection.Ascending });
            return this;
        }

        public ABFSortBuilder<T> ThenByDescending(Expression<Func<T, object>> keySelector)
        {
            _fields.Add(new ABFSortField<T> { KeySelector = keySelector, Direction = ABFSortDirection.Descending });
            return this;
        }

        public IABFSortDefinition<T> Build() => new ABFSortDefinition<T>(_fields);
    }
    public static class ABFSortExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, IABFSortDefinition<T> sortDefinition)
        {
            if (sortDefinition == null || !sortDefinition.GetFields().Any())
                return query;

            IOrderedQueryable<T> orderedQuery = null;
            var fields = sortDefinition.GetFields();

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var isAscending = field.Direction == ABFSortDirection.Ascending;

                if (i == 0)
                {
                    orderedQuery = isAscending
                        ? Queryable.OrderBy(query, (dynamic)field.KeySelector)
                        : Queryable.OrderByDescending(query, (dynamic)field.KeySelector);
                }
                else
                {
                    orderedQuery = isAscending
                        ? Queryable.ThenBy(orderedQuery, (dynamic)field.KeySelector)
                        : Queryable.ThenByDescending(orderedQuery, (dynamic)field.KeySelector);
                }
            }

            return orderedQuery ?? query;
        }

        public static IEnumerable<T> ApplySort<T>(this IEnumerable<T> query, IABFSortDefinition<T> sortDefinition)
        {
            if (sortDefinition == null || !sortDefinition.GetFields().Any())
                return query;

            IOrderedEnumerable<T> orderedQuery = null;
            var fields = sortDefinition.GetFields();

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var compiledKeySelector = (Func<T, object>)((LambdaExpression)field.KeySelector).Compile();
                var isAscending = field.Direction == ABFSortDirection.Ascending;

                if (i == 0)
                {
                    orderedQuery = isAscending
                        ? Enumerable.OrderBy(query, x => ((LambdaExpression)field.KeySelector).Compile().DynamicInvoke(x))
                        : Enumerable.OrderByDescending(query, x => ((LambdaExpression)field.KeySelector).Compile().DynamicInvoke(x));
                }
                else
                {
                    orderedQuery = isAscending
                        ? Enumerable.ThenBy(orderedQuery, x => ((LambdaExpression)field.KeySelector).Compile().DynamicInvoke(x))
                        : Enumerable.ThenByDescending(orderedQuery, x => ((LambdaExpression)field.KeySelector).Compile().DynamicInvoke(x));
                }
            }

            return orderedQuery ?? query;
        }
    }

}
