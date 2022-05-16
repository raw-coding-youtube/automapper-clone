using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


var b = Mapper.Map<B>(new A() { Id = 1, Name = "Foo" });

Console.WriteLine(b.Id + " name:" + b.Name + " " + b.Word);

public static class Mapper
{
    private static Dictionary<(Type, Type), Type> _cache = new();

    public static T Map<T>(object from)
    {
        var key = (from: from.GetType(), to: typeof(T));
        if (!_cache.ContainsKey(key))
        {
            PopulateCacheKey(key);
        }

        return (T)_cache[key].InvokeMember(
            "Map",
            BindingFlags.Default | BindingFlags.InvokeMethod,
            null,
            null,
            new[] { from }
        );
    }

    private static void PopulateCacheKey((Type from, Type to) key)
    {
        var fromProps = key.from.GetProperties();
        var toProps = key.to.GetProperties();

        var sb = new StringBuilder();
        sb.Append(@"
        namespace Gen;
        
        public static class Mapper {");

        sb.Append("public static ");
        sb.Append(key.to.FullName);
        sb.Append(" Map(");
        sb.Append(key.from.FullName);
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
                MetadataReference.CreateFromFile(typeof(Program).Assembly.Location),
            },
            options: new(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics)
            {
                Console.WriteLine(d.GetMessage());
            }

            return;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var mapperType = assembly.GetType("Gen.Mapper");
        _cache[key] = mapperType;
    }
}

public class A
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Word { get; set; } = "body";
}

public class B
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Word { get; set; } = "word";
}