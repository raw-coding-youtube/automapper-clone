using System.Reflection;
using System.Reflection.Emit;

var b = Mapper.Map<B>(new A { Id = 1, Name = "foo" });

Console.WriteLine(b.Id + " " + b.Name + " "  + b.Body);

public class Mapper
{
    private static readonly Dictionary<(Type, Type), MethodInfo> _cache = new();

    public static T Map<T>(object v)
    {
        var key = (v.GetType(), typeof(T));
        if (!_cache.ContainsKey(key))
        {
            _cache[key] = CreateMapMethod(key.Item1, key.Item2);
        }

        return (T)_cache[key].Invoke(null, new[] { v });
    }

    static MethodInfo CreateMapMethod(Type fromType, Type toType)
    {
        var aName = new AssemblyName("InternalMapperAssembly");
        var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule(aName.Name);
        var typeBuilder = mb.DefineType("Mapper", TypeAttributes.NotPublic);

        // IL_0000: newobj instance void C/B::.ctor()
        // IL_0005: dup
        // IL_0006: ldarg.0
        // IL_0007: callvirt instance int32 C/A::get_Id()
        // IL_000c: callvirt instance void C/B::set_Id(int32)
        // IL_0011: dup
        // IL_0012: ldarg.0
        // IL_0013: callvirt instance string C/A::get_Name()
        // IL_0018: callvirt instance void C/B::set_Name(string)
        // IL_001d: ret
        var methodBuilder = typeBuilder.DefineMethod(
            "Map",
            MethodAttributes.Public | MethodAttributes.Static,
            toType,
            new[] { fromType }
        );

        var gen = methodBuilder.GetILGenerator();
        gen.Emit(OpCodes.Newobj, toType.GetConstructor(Type.EmptyTypes));
        foreach (var property in fromType.GetProperties())
        {
            var toProp = toType.GetProperty(property.Name);
            if (toProp == null)
            {
                continue;
            }

            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Callvirt, property.GetMethod);
            gen.Emit(OpCodes.Callvirt, toProp.SetMethod);
        }

        gen.Emit(OpCodes.Ret);

        var type = typeBuilder.CreateType();

        return type.GetMethod("Map", BindingFlags.Public | BindingFlags.Static, new[] { fromType });
    }
}


public class A
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Body { get; set; } = "asdfasdfsdaf";
}

public class B
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
}