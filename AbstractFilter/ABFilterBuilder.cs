using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AbstractFilter
{
    public class ABFilterBuilder<T>
    {
        public IABFFilter<T> Eq<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.Equal(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }

        public IABFFilter<T> Ne<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.NotEqual(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }

        public IABFFilter<T> Gte<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.GreaterThanOrEqual(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }

        public IABFFilter<T> Gt<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.GreaterThan(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }

        public IABFFilter<T> Lte<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.LessThanOrEqual(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }
        public IABFFilter<T> Lt<TField>(Expression<Func<T, TField>> fieldSelector, TField value)
        {
            var body = Expression.LessThan(fieldSelector.Body, Expression.Constant(value, typeof(TField)));
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }
        public IABFFilter<T> In<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            var list = values.ToList();
            var method = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TField));

            var constantList = Expression.Constant(list);
            var call = Expression.Call(method, constantList, fieldSelector.Body);
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(call, fieldSelector.Parameters));
        }

        public IABFFilter<T> NotIn<TField>(Expression<Func<T, TField>> fieldSelector, IEnumerable<TField> values)
        {
            var inFilter = (ABFExpressionFilter<T>)In(fieldSelector, values);
            var body = Expression.Not(inFilter.ToExpression().Body);
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(body, fieldSelector.Parameters));
        }

        public IABFFilter<T> Regex(Expression<Func<T, string>> fieldSelector, string pattern, string options = "")
        {
            RegexOptions regexOptions = RegexOptions.None;
            if (options.Contains("i", StringComparison.OrdinalIgnoreCase))
                regexOptions |= RegexOptions.IgnoreCase;

            var method = typeof(System.Text.RegularExpressions.Regex).GetMethod(
                nameof(System.Text.RegularExpressions.Regex.IsMatch),
                new[] { typeof(string), typeof(string), typeof(RegexOptions) });

            var call = Expression.Call(
                method,
                fieldSelector.Body,
                Expression.Constant(pattern),
                Expression.Constant(regexOptions)
            );

            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(call, fieldSelector.Parameters));
        }

        public IABFFilter<T> Where(Expression<Func<T, bool>> predicate)
        {
            return new ABFExpressionFilter<T>(predicate);
        }

        public IABFFilter<T> And(params IABFFilter<T>[] filters) => And(filters.AsEnumerable());

        public IABFFilter<T> And(IEnumerable<IABFFilter<T>> filters)
        {
            var expressions = filters.Select(f => f.ToExpression()).ToList();
            if (!expressions.Any()) return new ABFExpressionFilter<T>(_ => true);

            var parameter = expressions.First().Parameters[0];
            Expression combined = expressions[0].Body;

            for (int i = 1; i < expressions.Count; i++)
            {
                var swappedBody = new ABFParameterRebinder(expressions[i].Parameters[0], parameter).Visit(expressions[i].Body);
                combined = Expression.AndAlso(combined, swappedBody);
            }

            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }

        public IABFFilter<T> Or(params IABFFilter<T>[] filters) => Or(filters.AsEnumerable());

        public IABFFilter<T> Or(IEnumerable<IABFFilter<T>> filters)
        {
            var expressions = filters.Select(f => f.ToExpression()).ToList();
            if (!expressions.Any()) return new ABFExpressionFilter<T>(_ => false);

            var parameter = expressions.First().Parameters[0];
            Expression combined = expressions[0].Body;

            for (int i = 1; i < expressions.Count; i++)
            {
                var swappedBody = new ABFParameterRebinder(expressions[i].Parameters[0], parameter).Visit(expressions[i].Body);
                combined = Expression.OrElse(combined, swappedBody);
            }

            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }

        public IABFFilter<T> ElemMatch<TCollection, TElement>(
            Expression<Func<T, IEnumerable<TElement>>> collectionSelector,
            Expression<Func<TElement, bool>> elementFilter)
        {
            var method = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TElement));

            var call = Expression.Call(method, collectionSelector.Body, elementFilter);
            return new ABFExpressionFilter<T>(Expression.Lambda<Func<T, bool>>(call, collectionSelector.Parameters));
        }

        public IABFFilter<T> AnyIn<TCollection, TElement>(
            Expression<Func<T, IEnumerable<TElement>>> collectionSelector,
            IEnumerable<TElement> values)
        {
            var list = values.ToList();
            var elementParam = Expression.Parameter(typeof(TElement), "x");
            var method = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TElement));

            var containsCall = Expression.Call(method, Expression.Constant(list), elementParam);
            var lambda = Expression.Lambda<Func<TElement, bool>>(containsCall, elementParam);

            return ElemMatch<T, TElement>(collectionSelector, lambda);
        }
    }
}
