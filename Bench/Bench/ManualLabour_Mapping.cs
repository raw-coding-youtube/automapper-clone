namespace Bench;

public class ManualLabour_Mapping
{
    public static B MapToB(A a) =>
        new B()
        {
            Id = a.Id,
            Name = a.Name,
            Body = a.Body,
        };
}