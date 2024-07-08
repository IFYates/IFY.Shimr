using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFY.Shimr.CodeGen;

public static class TypeDeclarationSyntaxExtensions
{
    const char NESTED_CLASS_DELIMITER = '+';
    const char NAMESPACE_CLASS_DELIMITER = '.';
    const char TYPEPARAMETER_CLASS_DELIMITER = '`';

    public static string GetFullName(this TypeDeclarationSyntax source, bool asParent = false)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var namespaces = new LinkedList<NamespaceDeclarationSyntax>();
        var types = new LinkedList<TypeDeclarationSyntax>();
        for (var parent = source.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is NamespaceDeclarationSyntax @namespace)
            {
                namespaces.AddFirst(@namespace);
            }
            else if (parent is TypeDeclarationSyntax type)
            {
                types.AddFirst(type);
            }
        }

        var result = new StringBuilder();
        for (var item = namespaces.First; item is not null; item = item.Next)
        {
            result.Append(item.Value.Name).Append(NAMESPACE_CLASS_DELIMITER);
        }
        for (var item = types.First; item is not null; item = item.Next)
        {
            appendName(item.Value);
            result.Append(asParent ? NAMESPACE_CLASS_DELIMITER : NESTED_CLASS_DELIMITER);
        }
        appendName(source);

        return result.ToString();

        void appendName(TypeDeclarationSyntax type)
        {
            result.Append(type.Identifier.Text);
            var typeArguments = type.TypeParameterList?.ChildNodes()
                .Count(node => node is TypeParameterSyntax) ?? 0;
            if (typeArguments != 0)
            {
                result.Append(TYPEPARAMETER_CLASS_DELIMITER).Append(typeArguments);
            }
        }
    }
}

[Generator]
public class Class1 : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var attrType = Type.GetType("IFY.Shimr.ShimrAttribute");

        var attributeSymbol = context.Compilation.GetTypeByMetadataName("IFY.Shimr.ShimrAttribute");
        var classesWithAttribute = context.Compilation.SyntaxTrees
            .SelectMany(st => st.GetRoot()
                    .DescendantNodes()
                    .Where(n => n is InterfaceDeclarationSyntax)
                    .Select(n => n as InterfaceDeclarationSyntax)
                    .Where(r => r.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(a => a.Name.GetText().ToString() == "Shimr")));

        var cast = classesWithAttribute.Single().AttributeLists.Single().SyntaxTree.GetRoot().DescendantNodes()
.Where(n => n is TypeOfExpressionSyntax).Select(n => n as TypeOfExpressionSyntax).First();

        string source = $@"// Auto-generated code
using System;

namespace IFY.Shimr
{{
    [AttributeUsage(AttributeTargets.Interface)]
    public class ShimrAttribute : Attribute
    {{
        public Type ImplementationType {{ get; }}

        public ShimrAttribute(Type implementationType)
        {{
            ImplementationType = implementationType;
        }}
    }}
}}

namespace IFY.Shimr.Auto
{{
    public class StringShimr : {classesWithAttribute.Single().GetFullName(true)}
    {{
        private readonly static string _type;
        static StringShimr()
        {{
            _type = IFY.Shimr.Tests.CodeGenTests.Register(typeof(StringShimr), typeof({classesWithAttribute.Single().GetFullName(true)}), typeof({cast.Type}));
        }}

        private readonly string _inst;

        public StringShimr(string inst)
        {{
            _inst = inst;
        }}

        public override string ToString()
        {{
return ""> {classesWithAttribute.Single().GetFullName()}"";
//return ""... {cast.Type}"";
//            return _inst.ToString();
        }}
    }}
}}
";
        context.AddSource($"Shimr.g.cs", source);
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        //context.RegisterForSyntaxNotifications(() => new ShimrReceiver());
    }

    //class ShimrReceiver : ISyntaxReceiver
    //{
    //    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    //    {
    //        if (syntaxNode is ClassDeclarationSyntax cds
    //            && cds.HaveAttribute(ShimrAttribute))
    //        {
    //        }
    //    }
    //}
}
