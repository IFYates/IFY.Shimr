﻿using IFY.Shimr.Gen.SyntaxParsing;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Text;

namespace IFY.Shimr.Gen;

internal class ShimWriter
{
    private readonly StringBuilder _src;
    private readonly string _fileVersion;

    public ShimWriter(StringBuilder src)
    {
        _src = src;

        var asmFile = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
        _fileVersion = asmFile.ProductVersion ?? "0.0.0.0";
    }

    internal void CreateShim(ShimTypeDefinition[] shims)
    {
        // TODO: better way to show target doesn't implement shim (if strict)
        // TODO: type checking

        _src.AppendLine($"namespace {shims[0].TargetNamespace};");
        _src.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"IFY.Shimr\", \"{_fileVersion}\")]");
        _src.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
        _src.AppendLine($"internal static class {shims[0].TargetSafeName}ShimrExtension");
        _src.AppendLine("{");

        // Implementation per shim
        foreach (var shim in shims)
        {
            // TODO: pass through some target attributes, like DebuggerDisplay
            _src.AppendLine($"\t// Shim of {shim.TargetFullName} as {shim.ShimFullName}");
            _src.AppendLine("\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
            _src.Append($"\tpublic class {shim.ShimrName} : {shim.ShimFullName}");
            if (!shim.IsStatic)
            {
                _src.Append(", IFY.Shimr.IShim");
            }
            _src.AppendLine().AppendLine("\t{");

            var refName = shim.IsStatic ? shim.TargetFullName : "_obj";
            if (!shim.IsStatic)
            {
                _src.AppendLine($"\t\tprivate readonly {shim.TargetName} _obj;");
                _src.AppendLine($"\t\tpublic {shim.ShimrName}({shim.TargetName} obj)");
                _src.AppendLine("\t\t{");
                _src.AppendLine("\t\t\t_obj = obj;");
                _src.AppendLine("\t\t}");
            }

            // Properties
            var properties = shim.Members.Where(m => m.Kind == SymbolKind.Property)
                .GroupBy(p => p.Name).ToArray();
            foreach (var group in properties)
            {
                var distinct = group.Count() == 1;
                foreach (var property in group)
                {
                    var memRefName = property.StaticType?.FullName ?? refName;
                    var implementor = !distinct && property.ParentTypeFullName != shim.ShimFullName ? property.ParentTypeFullName : null;
                    CreateProperty(2, property.ReturnType!, property.Name, property.CanRead, property.CanWrite, memRefName, property.TargetReturnType, property.TargetName, implementor);
                }
            }

            // Methods
            var methods = shim.Members.Where(m => m.Kind == SymbolKind.Method)
                .GroupBy(m => m.SignatureName).ToArray();
            foreach (var group in methods)
            {
                // Don't shim over the automated methods
                if (group.Key is "ToString()" or "Unshim()")
                {
                    continue;
                }

                var distinct = group.Count() == 1;
                foreach (var method in group)
                {
                    var memRefName = method.StaticType?.FullName ?? refName;
                    var constructorType = method.IsConstructor ? (method.StaticType?.FullName ?? shim.TargetFullName) : null;
                    var implementor = !distinct && method.ParentTypeFullName != shim.ShimFullName ? method.ParentTypeFullName : null;
                    CreateMethod(2, method.ReturnType, method.Name, method.Parameters.Values, memRefName, method.TargetReturnType, method.TargetName, constructorType, implementor);
                }
            }

            if (!shim.IsStatic)
            {
                _src.AppendLine("\t\tpublic object Unshim()");
                _src.AppendLine("\t\t{");
                _src.AppendLine("\t\t\treturn _obj;");
                _src.AppendLine("\t\t}");

                _src.AppendLine("\t\tpublic override string? ToString()");
                _src.AppendLine("\t\t{");
                _src.AppendLine("\t\t\treturn _obj.ToString();");
                _src.AppendLine("\t\t}");
            }

            _src.AppendLine("\t}");
        }

        // Shim extension
        var instShims = shims.Where(s => !s.IsStatic).ToArray();
        if (instShims.Any())
        {
            _src.AppendLine("\t[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"obj\")]");
            _src.AppendLine($"\tpublic static T? Shim<T>(this {shims[0].TargetName}? obj)");
            _src.AppendLine("\t{");

            _src.AppendLine($"\t\tif ({(shims[0].TargetType.IsValueType ? "!obj.HasValue" : "obj == null")})");
            _src.AppendLine("\t\t{");
            _src.AppendLine("\t\t\treturn default;");
            _src.AppendLine("\t\t}");

            foreach (var shim in instShims)
            {
                _src.AppendLine($"\t\telse if (typeof(T) == typeof({shim.ShimFullName}))");
                _src.AppendLine("\t\t{");
                _src.AppendLine($"\t\t\treturn (T)(object)new {shim.ShimrName}(obj{(shims[0].TargetType.IsValueType ? ".Value" : "")});");
                _src.AppendLine("\t\t}");
            }

            _src.AppendLine($"\t\tthrow new Exception(\"Invalid shim target type for this object: \" + typeof(T).FullName);");
            _src.AppendLine("\t}");
        }

        _src.AppendLine("}");
    }

    public void CreateProperty(int indent, TypeDef returnType, string name, bool canRead, bool canWrite, string targetRefName, TypeDef? shimToType, string? targetAlias, string? implementTypeName)
    {
        var pad = new string('\t', indent);
        if (implementTypeName != null)
        {
            _src.AppendLine($"{pad}{returnType.FullName} {implementTypeName}.{name}");
            targetRefName = $"(({implementTypeName}){targetRefName})";
        }
        else
        {
            _src.AppendLine($"{pad}public {returnType.FullName} {name}");
        }
        _src.AppendLine($"{pad}{{");
        if (canRead)
        {
            _src.Append($"{pad}\tget => ");
            if (shimToType != null)
            {
                _src.Append($"{shimToType.Namespace}.{shimToType.Name.MakeSafeName()}ShimrExtension.Shim<{returnType.FullName}>(");
            }
            _src.Append($"{targetRefName}.{targetAlias ?? name}");
            if (shimToType != null)
            {
                _src.Append(")");
            }
            _src.AppendLine(";");
        }
        if (canWrite)
        {
            _src.Append($"{pad}\tset => {targetRefName}.{targetAlias ?? name} = ");
            if (shimToType != null)
            {
                var targetReturnTypeFullName = shimToType.FullName;
                _src.AppendLine($"((object)value is {targetReturnTypeFullName} v) ? v : ({targetReturnTypeFullName})((IFY.Shimr.IShim)(object)value).Unshim();");
            }
            else
            {
                _src.AppendLine("value;");
            }
        }
        _src.AppendLine($"{pad}}}");
    }

    public void CreateMethod(int indent, TypeDef? returnType, string name, IEnumerable<MethodParameterDefinition> parameters, string targetRefName, TypeDef? shimToType, string? targetAlias, string? constructorTypeName, string? implementTypeName)
    {
        var pad = new string('\t', indent);
        if (implementTypeName != null)
        {
            _src.Append($"{pad}{returnType?.FullName ?? "void"} {implementTypeName}.{name}(");
            targetRefName = $"(({implementTypeName}){targetRefName})";
        }
        else
        {
            _src.Append($"{pad}public {returnType?.FullName ?? "void"} {name}(");
        }
        _src.Append(string.Join(", ", parameters.Select(p => $"{p.ParameterTypeFullName} {p.Name}")));
        _src.AppendLine(")");
        _src.AppendLine($"{pad}{{");

        var argList = parameters
            .Select(p => p.TargetTypeFullName != null
                ? p.ParameterType.Kind == TypeKind.Interface
                ? $"({p.TargetTypeFullName})((IFY.Shimr.IShim)(object){p.Name}).Unshim()"
                : $"({p.TargetTypeFullName}){p.Name}"
                : p.Name)
            .ToArray();

        if (returnType != null)
        {
            _src.Append($"{pad}\treturn ");
            if (shimToType != null)
            {
                _src.Append($"{shimToType.Namespace}.{shimToType.Name.MakeSafeName()}ShimrExtension.Shim<{returnType?.FullName}>(");
            }
            if (constructorTypeName != null)
            {
                _src.Append($"new {constructorTypeName}(");
            }
            else
            {
                _src.Append($"{targetRefName}.{targetAlias ?? name}(");
            }
            _src.Append(string.Join(", ", argList));
            if (shimToType != null)
            {
                _src.Append($")");
            }
            _src.AppendLine(");");
        }
        else
        {
            _src.Append($"{pad}\t{targetRefName}.{targetAlias ?? name}(");
            _src.Append(string.Join(", ", argList));
            _src.AppendLine(");");
        }
        _src.AppendLine($"{pad}}}");
    }

    public void CreateStaticShimCreator(ShimTypeDefinition[] shims)
    {
        _src.AppendLine("namespace IFY.Shimr;");
        _src.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"IFY.Shimr\", \"{_fileVersion}\")]");
        _src.AppendLine("public static class ShimBuilder");
        _src.AppendLine("{");

        _src.AppendLine($"\tpublic static T Create<T>()");
        _src.AppendLine("\t{");
        var first = true;
        foreach (var shim in shims)
        {
            _src.AppendLine($"\t\t{(first ? "if" : "else if")} (typeof(T) == typeof({shim.ShimFullName}))");
            _src.AppendLine("\t\t{");
            _src.AppendLine($"\t\t\treturn (T)(object)new {shim.TargetNamespace}.{shim.TargetSafeName}ShimrExtension.{shim.ShimrName}();");
            _src.AppendLine("\t\t}");
            first = false;
        }
        _src.AppendLine("\t\tthrow new System.NotImplementedException(\"Unable to create a static shim of type \" + typeof(T).FullName);");
        _src.AppendLine("\t}");

        _src.AppendLine("}");
    }
}
