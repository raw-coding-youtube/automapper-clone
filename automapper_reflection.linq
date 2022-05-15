<Query Kind="Program" />

void Main()
{
	Mapper.Map<B>(new A { Id = 5, Name = "Bob" }).Dump();
}

public class Mapper
{

	static Dictionary<(Type from, Type to), List<(MethodInfo Get, MethodInfo Set)>> _cache = new();

	public static T Map<T>(object f) where T : class, new()
	{
		var key = (from: f.GetType(), to: typeof(T));

		if (!_cache.ContainsKey(key))
		{
			PopulateCacheKey(key);
		}

		var result = new T();
		var entry = _cache[key];
		foreach (var e in entry)
		{
			var val = e.Get.Invoke(f, null);
			e.Set.Invoke(result, new[] { val });
		}

		return result;
	}

	public static void PopulateCacheKey((Type from, Type to) key)
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
		_cache[key] = entry;
	}
}

public class A
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Body { get; set; } = "word";
}

public class B
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Body { get; set; } = "asdfasd";
}