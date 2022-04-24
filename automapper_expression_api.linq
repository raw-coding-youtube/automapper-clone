<Query Kind="Program" />

void Main()
{
	Expression<Func<A, B>> exp = a => new B { Id = a.Id, Name = a.Name };

	Mapper.To<B>(new A { Id = 2, Name = "Something asdf" }).Dump("result");

}

public class Mapper
{
	
	public static Dictionary<(Type, Type), Delegate> _cache = new ();

	public static T To<T>(object o)
	{
		var inType = o.GetType();
		var outType = typeof(T);
		
		var key= (inType, outType);
		if(!_cache.ContainsKey(key)){
			_cache[key] = CreateDelegate(inType, outType);
		}
		
		return (T) _cache[key].DynamicInvoke(o);
	}
	private static Delegate CreateDelegate(Type inType, Type outType)
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

		return Expression.Lambda(body, false, param).Compile();
	}
}


public class A
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Body { get; set; } = "asdfasdfasdfdsa";
}

public class B
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Body { get; set; }
}