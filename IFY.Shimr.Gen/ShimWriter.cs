using System.Text;

namespace IFY.Shimr.Gen;

internal class ShimWriter
{
    private readonly StringBuilder _src;

    public ShimWriter(StringBuilder src)
    {
        _src = src;
    }

    public void CreateExtensionMethod(ShimTypeDefinition[] shims)
    {
        _src.AppendLine("using System.Diagnostics.CodeAnalysis;");

        _src.AppendLine($"namespace {shims[0].TargetNamespace}");
        _src.AppendLine("{");
        _src.AppendLine($"\tpublic static class {shims[0].TargetSafeName}ShimrExtension");
        _src.AppendLine("\t{");

        _src.AppendLine("\t\t[return: NotNullIfNotNull(\"obj\")]");
        _src.AppendLine($"\t\tpublic static T? Shim<T>(this {shims[0].TargetFullName}? obj)");
        _src.AppendLine("\t\t{");

        _src.AppendLine("\t\t\tif (obj == null)");
        _src.AppendLine("\t\t\t{");
        _src.AppendLine("\t\t\t\treturn default;");
        _src.AppendLine("\t\t\t}");

        foreach (var shim in shims)
        {
            _src.AppendLine($"\t\t\telse if (typeof(T) == typeof({shim.ShimFullName}))");
            _src.AppendLine("\t\t\t{");
            _src.AppendLine($"\t\t\t\treturn (T)(object)new _Shimr.{shim.ShimrName}(obj);");
            _src.AppendLine("\t\t\t}");
        }

        _src.AppendLine($"\t\t\tthrow new Exception(\"Invalid shim target type for this object: \" + typeof(T).FullName);");
        _src.AppendLine("\t\t}");

        _src.AppendLine("\t}");
        _src.AppendLine("}");
    }

    internal void CreateShim(ShimTypeDefinition shim)
    {
        // TODO: usings?

        // TODO: better way to show target doesn't implement shim (if strict)
        // TODO: type checking
        // TODO: pull through documentation

        _src.AppendLine($"namespace _Shimr;");
        _src.AppendLine($"internal class {shim.ShimrName} : {shim.ShimFullName}");
        _src.AppendLine("{");
        _src.AppendLine($"\tprivate readonly {shim.TargetFullName} _obj;");

        _src.AppendLine($"\tpublic {shim.ShimrName}({shim.TargetFullName} obj)");
        _src.AppendLine("\t{");
        _src.AppendLine("\t\t_obj = obj;");
        _src.AppendLine("\t}");

        foreach (var property in shim.Members.Where(m => m.Kind == ShimMemberDefinition.MemberKind.Property))
        {
            _src.Append($"\tpublic {property.ReturnTypeFullName} {property.Name} {{ ");
            if (property.CanRead)
            {
                _src.Append($"get => _obj.{property.TargetName ?? property.Name}; ");
            }
            if (property.CanWrite)
            {
                _src.Append($"set => _obj.{property.TargetName ?? property.Name} = value; ");
            }
            _src.AppendLine("}");
        }

        foreach (var method in shim.Members.Where(m => m.Kind == ShimMemberDefinition.MemberKind.Method))
        {
            
            _src.Append($"\tpublic {method.ReturnTypeFullName ?? "void"} {method.Name}(");
            _src.Append(string.Join(", ", method.Parameters.Select(p => $"{p.Value} {p.Key}")));
            _src.AppendLine(")");

            _src.AppendLine("\t{");
            if (method.ReturnTypeFullName != null)
            {
                _src.Append($"\t\treturn _obj.{method.TargetName ?? method.Name}(");
                _src.Append(string.Join(", ", method.Parameters.Keys));
                _src.AppendLine(");");
            }
            else
            {
                _src.Append($"\t\t_obj.{method.TargetName ?? method.Name}(");
                _src.Append(string.Join(", ", method.Parameters.Keys));
                _src.AppendLine(");");
            }
            _src.AppendLine("\t}");
        }

        _src.AppendLine("}");
    }
}
