using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Bench.Infrastructure;

namespace Bench;

public class IL_Mapper
{
    public static T Map<T>(object v)
    {
        var fromType = v.GetType();
        if (!Cache<IL_Mapper, T, Func<object, T>>.ContainsKey(fromType))
        {
            Cache<IL_Mapper, T, Func<object, T>>.Add(fromType, CreateMapFunc<T>(fromType));
        }

        return Cache<IL_Mapper, T, Func<object, T>>.Get(fromType)(v);
    }

    static Func<object, T> CreateMapFunc<T>(Type fromType)
    {
        var aName = new AssemblyName("InternalMapperAssembly");
        var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule(aName.Name);
        var typeBuilder = mb.DefineType("Mapper", TypeAttributes.NotPublic);
        var toType = typeof(T);
        
        // newobj instance void C/B::.ctor()
        // dup
        // ldarg.0
        // callvirt instance int32 C/A::get_Id()
        // callvirt instance void C/B::set_Id(int32)
        // dup
        // ldarg.0
        // callvirt instance string C/A::get_Name()
        // callvirt instance void C/B::set_Name(string)
        // ret
        var methodBuilder = typeBuilder.DefineMethod(
            "Map",
            MethodAttributes.Public | MethodAttributes.Static,
            toType,
            new[] { typeof(object) }
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

        var method = typeBuilder.CreateType()
            .GetMethod("Map", BindingFlags.Public | BindingFlags.Static, new[] { fromType });

        return (Func<object, T>)Delegate.CreateDelegate(typeof(Func<object, T>), method);
    }
}