using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sys.Common.Helper
{
    public static class FilterGeneric<T> where T : class
    {
        public static Expression<Func<T, bool>> BuildFilterExpression(GenericFilter filter)
        {
            var property = typeof(T).GetProperty(filter.Property);

            // Build the dynamic expression
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, filter.Property);

            // Kiểm tra xem kiểu của thuộc tính có phải là string hay không
            Expression filterExpression = null;
            if (property.PropertyType == typeof(string))
            {
                var toLowerCall = Expression.Call(
                    Expression.Call(propertyAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                    typeof(string).GetMethod("Trim", Type.EmptyTypes)
                );

                var stringEqualsExpressions = filter.Values
                    .Select(value => Expression.Equal(toLowerCall, Expression.Constant(value.ToLower(), typeof(string))))
                    .ToList();

                filterExpression = stringEqualsExpressions
                    .Aggregate((expr1, expr2) => Expression.Or(expr1, expr2));
            }
            else if (property.PropertyType == typeof(Guid))
            {
                var guidEqualsExpressions = filter.Values
                    .Select(value => Expression.Equal(propertyAccess, Expression.Constant(Guid.Parse(value))))
                    .ToList();

                filterExpression = guidEqualsExpressions
                    .Aggregate((expr1, expr2) => Expression.Or(expr1, expr2));
            }
            else
            {
                var equalsExpressions = filter.Values
                    .Select(value => Expression.Equal(propertyAccess, Expression.Constant(Convert.ChangeType(value, property.PropertyType))))
                    .ToList();

                filterExpression = equalsExpressions
                    .Aggregate((expr1, expr2) => Expression.Or(expr1, expr2));
            }

            // Create the final filter expression
            var finalFilterExpression = Expression.Lambda<Func<T, bool>>(
                filterExpression,
                parameter
            );

            return finalFilterExpression;
        }

    }
}
