using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Bench.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bench;

public class Roslyn_Mapper
{
    public static T Map<T>(object from)
    {
        var inType = from.GetType();
        if (!Cache<Roslyn_Mapper, T, Func<object, T>>.ContainsKey(inType))
        {
            Cache<Roslyn_Mapper, T, Func<object, T>>.Add(inType, PopulateCacheKey<T>(inType));
        }

        return Cache<Roslyn_Mapper, T, Func<object, T>>.Get(inType)(from);
    }

    private static Func<object, T> PopulateCacheKey<T>(Type fromType)
    {
        var toType = typeof(T);
        var fromProps = fromType.GetProperties();
        var toProps = toType.GetProperties();

        var sb = new StringBuilder();
        sb.Append(@"
        namespace Gen;
        
        public static class Mapper {");

        sb.Append("public static ");
        sb.Append(toType.FullName);
        sb.Append(" Map(");
        sb.Append(fromType.FullName);
        sb.Append(" from) => new(){");

        foreach (var from in fromProps)
        {
            var to = toProps.FirstOrDefault(x => x.Name == from.Name);
            if (to == null)
            {
                continue;
            }

            sb.Append(to.Name);
            sb.Append("=from.");
            sb.Append(from.Name);
            sb.Append(",");
        }

        sb.Append("};");
        sb.Append("}");

        var tree = CSharpSyntaxTree.ParseText(sb.ToString());


        var coreAssemblyLocation = typeof(object).Assembly.Location;
        var baseAssemblyPath = Path.GetDirectoryName(coreAssemblyLocation);
        Console.WriteLine(coreAssemblyLocation);
        var compilation = CSharpCompilation.Create(
            "Gen",
            syntaxTrees: new[] { tree },
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(coreAssemblyLocation),
                // MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                // MetadataReference.CreateFromFile(Path.Combine(baseAssemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Roslyn_Mapper).Assembly.Location),
            },
            options: new(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release
            )
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
            {
                Console.WriteLine(d.GetMessage());
            }

            throw new Exception("bad bad");
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var method = assembly.GetType("Gen.Mapper")
            .GetMethod("Map", BindingFlags.Public | BindingFlags.Static, new[] { fromType });

        var param = Expression.Parameter(typeof(object));
        var cast = Expression.Convert(param, fromType);
        var body = Expression.Call(method, cast);
        return Expression.Lambda<Func<object, T>>(body, false, param).Compile();
    }
}