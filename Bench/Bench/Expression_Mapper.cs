using System.Linq.Expressions;
using Bench.Infrastructure;

namespace Bench;

public class Expression_Mapper
{
    public static T Map<T>(object o)
    {
        var inType = o.GetType();
        var outType = typeof(T);

        if (!Cache<Expression_Mapper, T, Func<object, T>>.ContainsKey(inType))
        {
            Cache<Expression_Mapper, T, Func<object, T>>.Add(inType, CreateDelegate<T>(inType, outType));
        }

        return Cache<Expression_Mapper, T, Func<object, T>>.Get(inType)(o);
    }

    private static Func<object, T> CreateDelegate<T>(Type inType, Type outType)
    {
        var param = Expression.Parameter(inType);
        var newExpression = Expression.New(outType.GetConstructor(Type.EmptyTypes));

        List<MemberBinding> bindings = new();
        foreach (var prop in inType.GetProperties())
        {
            var tbm = outType.GetProperty(prop.Name);
            if (tbm == null)
            {
                continue;
            }

            var pma = Expression.MakeMemberAccess(param, prop);

            var binding = Expression.Bind(tbm, pma);
            bindings.Add(binding);
        }

        var body = Expression.MemberInit(newExpression, bindings);
        var lambda = Expression.Lambda(body, false, param);
        return SprinkleOfIndirection<T>(lambda, inType);
    }

    private static Func<object, T> SprinkleOfIndirection<T>(LambdaExpression exp, Type inType)
    {
        var param = Expression.Parameter(typeof(object));
        var cast = Expression.Convert(param, inType);
        var body = Expression.Invoke(exp, cast);
        return Expression.Lambda<Func<object, T>>(body, false, param).Compile();
    }
}