using System.Reflection;
using Bench.Infrastructure;

namespace Bench;

public class Reflection_Mapper
{
    public static T Map<T>(object f) where T : class, new()
    {
        var key = (from: f.GetType(), to: typeof(T));

        if (!Cache<Reflection_Mapper, T, List<(MethodInfo Get, MethodInfo Set)>>.ContainsKey(key.from))
        {
            PopulateCacheKey<T>(key);
        }

        var result = new T();
        var entry = Cache<Reflection_Mapper,T, List<(MethodInfo Get, MethodInfo Set)>>.Get(key.from);
        foreach (var e in entry)
        {
            var val = e.Get.Invoke(f, null);
            e.Set.Invoke(result, new[] { val });
        }

        return result;
    }

    public static void PopulateCacheKey<T>((Type from, Type to) key)
    {
        var fromProps = key.from.GetProperties();
        var toProps = key.to.GetProperties();

        List<(MethodInfo, MethodInfo)> entry = new();
        foreach (var from in fromProps)
        {
            var to = toProps.FirstOrDefault(x => x.Name == from.Name);
            if (to == null)
            {
                continue;
            }

            entry.Add((from.GetMethod, to.SetMethod));
        }

        Cache<Reflection_Mapper,T, List<(MethodInfo Get, MethodInfo Set)>>.Add(key.from, entry);
    }
}