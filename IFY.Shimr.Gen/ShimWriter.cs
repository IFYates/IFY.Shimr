using Microsoft.CodeAnalysis;
using System.Text;
using Tortuga.TestMonkey;

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
        _src.AppendLine($"namespace {shims[0].TargetNamespace}");
        _src.AppendLine("{");
        _src.AppendLine($"\tinternal static class {shims[0].TargetSafeName}ShimrExtension");
        _src.AppendLine("\t{");

        _src.AppendLine("\t\t[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"obj\")]");
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

        _src.AppendLine($"using {shim.TargetNamespace};");
        _src.AppendLine($"namespace _Shimr;");
        _src.AppendLine($"[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
        _src.AppendLine($"internal class {shim.ShimrName} : {shim.ShimFullName}, IFY.Shimr.IShim");
        _src.AppendLine("{");
        _src.AppendLine($"\tprivate readonly {shim.TargetFullName} _obj;");

        _src.AppendLine($"\tpublic {shim.ShimrName}({shim.TargetFullName} obj)");
        _src.AppendLine("\t{");
        _src.AppendLine("\t\t_obj = obj;");
        _src.AppendLine("\t}");

        foreach (var property in shim.Members.Where(m => m.Kind == SymbolKind.Property))
        {
            var returnTypeFullName = property.ReturnType!.FullName();
            _src.Append($"\tpublic {returnTypeFullName} {property.Name} {{ ");
            if (property.CanRead)
            {
                _src.Append($"get => _obj.{property.TargetName ?? property.Name}");
                if (property.IsReturnShim)
                {
                    _src.Append($".Shim<{returnTypeFullName}>()");
                }
                _src.Append("; ");
            }
            if (property.CanWrite)
            {
                _src.Append($"set => _obj.{property.TargetName ?? property.Name} = ");
                if (property.IsReturnShim)
                {
                    var targetReturnTypeFullName = property.TargetReturnType.FullName();
                    _src.Append($"(value as {targetReturnTypeFullName}) ?? ({targetReturnTypeFullName})((IFY.Shimr.IShim)value).Unshim(); ");
                }
                else
                {
                    _src.Append("value; ");
                }
            }
            _src.AppendLine("}");
        }

        foreach (var method in shim.Members.Where(m => m.Kind == SymbolKind.Method))
        {
            var returnTypeFullName = method.ReturnType?.FullName();
            _src.Append($"\tpublic {returnTypeFullName ?? "void"} {method.Name}(");
            _src.Append(string.Join(", ", method.Parameters.Select(p => $"{p.Value} {p.Key}")));
            _src.AppendLine(")");

            _src.AppendLine("\t{");
            if (method.ReturnType != null)
            {
                _src.Append($"\t\treturn _obj.{method.TargetName ?? method.Name}(");
                _src.Append(string.Join(", ", method.Parameters.Keys));
                if (method.IsReturnShim)
                {
                    _src.Append($").Shim<{returnTypeFullName}>(");
                }
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

        _src.AppendLine("\tpublic object Unshim()");
        _src.AppendLine("\t{");
        _src.AppendLine("\t\treturn _obj;");
        _src.AppendLine("\t}");

        _src.AppendLine("}");
    }
}
