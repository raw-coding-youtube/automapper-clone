using Bench;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

// dotnet run -c Release
BenchmarkRunner.Run<Bencher>();

[MemoryDiagnoser(false)]
public class Bencher
{
    private readonly A _a = new A() { Id = 1, Name = "Foo", Body = "Bar" };

    [Benchmark]
    public B Reflection() => Reflection_Mapper.Map<B>(_a);

    [Benchmark]
    public B Expressions() => Expression_Mapper.Map<B>(_a);

    [Benchmark]
    public B IL_Generator() => IL_Mapper.Map<B>(_a);

    [Benchmark]
    public B Roslyn() => Roslyn_Mapper.Map<B>(_a);

    [Benchmark]
    public B SourceGenerators() => SourceGen_Mapper.MapToB(_a);

    [Benchmark]
    public B ManualLabour() => ManualLabour_Mapping.MapToB(_a);
}

[Map]
public class A
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
}

[Map]
public class B
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
}


public class MapAttribute : Attribute
{
}