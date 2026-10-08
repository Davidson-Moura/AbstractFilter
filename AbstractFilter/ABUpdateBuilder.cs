using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AbstractFilter
{
    public class ABUpdateDefinition<T>
    {
        public Dictionary<string, object> Assignments { get; } = new();

        public ABUpdateDefinition<T> Set<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
        {
            var propertyName = GetPropertyName(propertySelector);
            Assignments[propertyName] = value;
            return this;
        }

        private string GetPropertyName<TValue>(Expression<Func<T, TValue>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;

            if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
                return unaryMember.Member.Name;

            throw new ArgumentException("Expressão de propriedade inválida para atualização.");
        }
    }

    public class ABUpdateBuilder<T>
    {
        private readonly ABUpdateDefinition<T> _definition = new();

        public ABUpdateBuilder<T> Set<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
        {
            _definition.Set(propertySelector, value);
            return this;
        }

        public ABUpdateDefinition<T> Build() => _definition;
    }
}
