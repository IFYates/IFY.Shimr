using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text;
using Tortuga.TestMonkey;

namespace IFY.Shimr.Gen;

internal class ShimWriter
{
    private readonly StringBuilder _src;
    private readonly string _fileVersion;

    public ShimWriter(StringBuilder src)
    {
        _src = src;
        _fileVersion = GetType().Assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "0.0.0.0"; // TODO
    }

    internal void CreateShim(ShimTypeDefinition[] shims)
    {
        // TODO: better way to show target doesn't implement shim (if strict)
        // TODO: type checking
        // TODO: pull through documentation

        _src.AppendLine($"namespace {shims[0].TargetNamespace};");
        _src.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"IFY.Shimr\", \"{_fileVersion}\")]");
        _src.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
        _src.AppendLine($"internal static class {shims[0].TargetSafeName}ShimrExtension");
        _src.AppendLine("{");

        // Implementation per shim
        foreach (var shim in shims)
        {
            // TODO: pass through some target attributes, like DebuggerDisplay
            _src.AppendLine("\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
            _src.AppendLine($"\tinternal class {shim.ShimrName} : {shim.ShimFullName}, IFY.Shimr.IShim");
            _src.AppendLine("\t{");
            _src.AppendLine($"\t\tprivate readonly {shim.TargetName} _obj;");

            _src.AppendLine($"\t\tpublic {shim.ShimrName}({shim.TargetName} obj)");
            _src.AppendLine("\t\t{");
            _src.AppendLine("\t\t\t_obj = obj;");
            _src.AppendLine("\t\t}");

            // Properties
            foreach (var property in shim.Members.Where(m => m.Kind == SymbolKind.Property))
            {
                var returnTypeFullName = property.ReturnType!.FullName();
                _src.AppendLine($"\t\tpublic {returnTypeFullName} {property.Name}");
                _src.AppendLine("\t\t{");
                if (property.CanRead)
                {
                    _src.Append($"\t\t\tget => _obj.{property.TargetName ?? property.Name}");
                    if (property.IsReturnShim)
                    {
                        _src.Append($".Shim<{returnTypeFullName}>()");
                    }
                    _src.AppendLine(";");
                }
                if (property.CanWrite)
                {
                    _src.Append($"\t\t\tset => _obj.{property.TargetName ?? property.Name} = ");
                    if (property.IsReturnShim)
                    {
                        var targetReturnTypeFullName = property.TargetReturnType.FullName();
                        _src.AppendLine($"(value as {targetReturnTypeFullName}) ?? ({targetReturnTypeFullName})((IFY.Shimr.IShim)value).Unshim();");
                    }
                    else
                    {
                        _src.AppendLine("value;");
                    }
                }
                _src.AppendLine("\t\t}");
            }

            // Methods
            foreach (var method in shim.Members.Where(m => m.Kind == SymbolKind.Method))
            {
                var returnTypeFullName = method.ReturnType?.FullName();
                _src.Append($"\t\tpublic {returnTypeFullName ?? "void"} {method.Name}(");
                _src.Append(string.Join(", ", method.Parameters.Select(p => $"{p.Value.ParameterTypeFullName} {p.Key}")));
                _src.AppendLine(")");
                _src.AppendLine("\t\t{");

                var argList = method.Parameters.Values
                    .Select(p => p.TargetTypeFullName != null
                        ? $"({p.TargetTypeFullName})((IShim){p.Name}).Unshim()"
                        : p.Name)
                    .ToArray();

                if (method.ReturnType != null)
                {
                    _src.Append($"\t\t\treturn _obj.{method.TargetName ?? method.Name}(");
                    _src.Append(string.Join(", ", argList));
                    if (method.IsReturnShim)
                    {
                        _src.Append($").Shim<{returnTypeFullName}>(");
                    }
                    _src.AppendLine(");");
                }
                else
                {
                    _src.Append($"\t\t\t_obj.{method.TargetName ?? method.Name}(");
                    _src.Append(string.Join(", ", argList));
                    _src.AppendLine(");");
                }
                _src.AppendLine("\t\t}");
            }

            _src.AppendLine("\t\tpublic object Unshim()");
            _src.AppendLine("\t\t{");
            _src.AppendLine("\t\t\treturn _obj;");
            _src.AppendLine("\t\t}");

            _src.AppendLine("\t\tpublic string? ToString()");
            _src.AppendLine("\t\t{");
            _src.AppendLine("\t\t\treturn _obj.ToString();");
            _src.AppendLine("\t\t}");

            _src.AppendLine("\t}");
        }

        // Shim extension
        _src.AppendLine("\t[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"obj\")]");
        _src.AppendLine($"\tpublic static T? Shim<T>(this {shims[0].TargetName}? obj)");
        _src.AppendLine("\t{");

        _src.AppendLine("\t\tif (obj == null)");
        _src.AppendLine("\t\t{");
        _src.AppendLine("\t\t\treturn default;");
        _src.AppendLine("\t\t}");

        foreach (var shim in shims)
        {
            _src.AppendLine($"\t\telse if (typeof(T) == typeof({shim.ShimFullName}))");
            _src.AppendLine("\t\t{");
            _src.AppendLine($"\t\t\treturn (T)(object)new {shim.ShimrName}(obj);");
            _src.AppendLine("\t\t}");
        }

        _src.AppendLine($"\t\tthrow new Exception(\"Invalid shim target type for this object: \" + typeof(T).FullName);");
        _src.AppendLine("\t}");

        _src.AppendLine("}");
    }
}
