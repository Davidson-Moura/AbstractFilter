using System.Linq.Expressions;

namespace AbstractFilter
{
    public class ABFinder<T>
    {
        public IABFFilter<T> Filter { get; set; }
        public IABFSortDefinition<T> Sort { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public List<Expression<Func<T, object>>> Includes { get; set; } = new();
    }
    public class ABFinder<T, TResult> : ABFinder<T>
    {
        public Expression<Func<T, TResult>> Projection { get; set; }
    }
    public interface IABFFilter<T>
    {
        Expression<Func<T, bool>> ToExpression();
    }

    public class ABFExpressionFilter<T> : IABFFilter<T>
    {
        private readonly Expression<Func<T, bool>> _expression;

        public ABFExpressionFilter(Expression<Func<T, bool>> expression)
        {
            _expression = expression;
        }

        public Expression<Func<T, bool>> ToExpression() => _expression;
    }
}
