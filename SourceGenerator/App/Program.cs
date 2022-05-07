var b = Mapper.Map(new A { Id = 1, Name = "Foo" });

Console.WriteLine(b.Id + " name: " + b.Name + " word: " + b.Word);

[Map]
public class A
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Word { get; set; } = "asdfasdfdsfsafsad";
}

[Map]
public class B
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Word { get; set; } = "a";
}

public class MapAttribute : Attribute
{
}