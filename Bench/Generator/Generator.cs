using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

[Generator]
public class Generator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var receiver = (ClassSyntaxReceiver)context.SyntaxReceiver;

        var sb = new StringBuilder();
        sb.Append("public static class SourceGen_Mapper {");

        for (int i = 0; i < receiver.ClassMappings.Count; i++)
        {
            var from = receiver.ClassMappings[i];

            for (int j = 0; j < receiver.ClassMappings.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var to = receiver.ClassMappings[j];
                sb.Append("public static ");
                sb.Append(to.Name);
                sb.Append(" MapTo");
                sb.Append(to.Name);
                sb.Append("(");
                sb.Append(from.Name);
                sb.Append(" from){");
                sb.Append("return new(){");
                
                foreach (var fromProperty in from.Properties)
                {
                    var toProperty = to.Properties.FirstOrDefault(x => x == fromProperty);
                    if (toProperty == null)
                    {
                        continue;
                    }
                        
                    sb.Append(toProperty);
                    sb.Append("=from.");
                    sb.Append(fromProperty);
                    sb.Append(',');
                }
                
                sb.Append("};");
                sb.Append('}');
            }
        }

        sb.Append('}');
        
        context.AddSource("Mapper.g.cs", sb.ToString());
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver());
    }
}

public class ClassSyntaxReceiver : ISyntaxReceiver
{
    public List<Clazz> ClassMappings { get; set; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax cds)
        {
            return;
        }

        if (!cds.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "Map")))
        {
            return;
        }

        var properties = cds.Members.Select(x => x as PropertyDeclarationSyntax)
            .Where(x => x != null)
            .Select(x => x.Identifier.Text)
            .ToList();

        ClassMappings.Add(new(cds.Identifier.Text, properties));
    }
}

public record Clazz(string Name, List<string> Properties);