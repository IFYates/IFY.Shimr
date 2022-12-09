using IFY.Shimr.Gen.SyntaxParsing;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Text;

namespace IFY.Shimr.Gen;

internal class ShimWriter
{
    private readonly StringBuilder _src;
    private readonly string _fileVersion;
    private static readonly Random R = new();

    public ShimWriter(StringBuilder src)
    {
        _src = src;

        var asmFile = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
        _fileVersion = asmFile.ProductVersion ?? "0.0.0.0";
    }

    private static string getGenericArgList(TypeDef type)
    {
        return type.GenericArgs.Any()
            ? "<" + string.Join(", ", type.GenericArgs.Select(a => ((ITypeParameterSymbol)a).Name)) + ">"
            : string.Empty;
    }

    internal void CreateTargetShims(TypeDef targetType, ShimTypeDefinition[] shims)
    {
        // TODO: better way to show target doesn't implement shim (if strict)
        // TODO: type checking

        _src.AppendLine($"namespace {targetType.Namespace};");
        _src.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"IFY.Shimr\", \"{_fileVersion}\")]");
        _src.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
        // TODO: option to make public
        _src.AppendLine($"internal static class {targetType.Name.MakeSafeName()}ShimrExtension");
        _src.AppendLine("{");

        // Implementation per shim
        foreach (var shim in shims)
        {
            var shimGenArgs = getGenericArgList(shim.ShimType);
            var targetDef = targetType.FullGenericName.Trim('<', '>') + shimGenArgs;

            // TODO: pass through some target attributes, like DebuggerDisplay
            _src.AppendLine($"\t// Shim of {targetType.FullName} as {shim.ShimFullName}");
            _src.AppendLine("\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]");
            _src.Append($"\tpublic class {shim.ShimSafeName}{shimGenArgs} : {shim.ShimFullName}");
            if (!shim.IsStatic)
            {
                _src.Append(", IFY.Shimr.IShim");
            }
            _src.AppendLine().AppendLine("\t{");

            var refName = shim.IsStatic ? targetDef : "_obj";
            if (!shim.IsStatic)
            {
                _src.AppendLine($"\t\tprivate readonly {targetDef} _obj;");
                _src.AppendLine($"\t\tpublic {shim.ShimSafeName}({targetDef} obj)");
                _src.AppendLine("\t\t{");
                _src.AppendLine("\t\t\t_obj = obj;");
                _src.AppendLine("\t\t}");
            }

            // Events
            var evs = shim.Members.Where(m => m.Kind == SymbolKind.Event)
                .GroupBy(p => p.Name).ToArray();
            foreach (var group in evs)
            {
                var distinct = group.Count() == 1; // TODO: how and do what?
                foreach (var ev in group)
                {
                    _src.AppendLine($"\t\tpublic event {ev.ReturnType!.FullName} {ev.Name}");
                    _src.AppendLine("\t\t{");
                    _src.AppendLine($"\t\t\tadd => _obj.{ev.Name} += value;");
                    _src.AppendLine($"\t\t\tremove => _obj.{ev.Name} -= value;");
                    _src.AppendLine("\t\t}");
                }
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
                    var constructorType = method.IsConstructor ? (method.StaticType?.FullName ?? targetType.FullName) : null;
                    var implementor = !distinct && method.ParentTypeFullName != shim.ShimFullName ? method.ParentTypeFullName : null;
                    CreateMethod(2, method.ReturnType, method.Name, method.Parameters.Values, memRefName, method.TargetReturnType, method.TargetName, constructorType, implementor, method.GenericContraints);
                }
            }

            if (!shim.IsStatic)
            {
                _src.AppendLine("\t\tpublic object Unshim() => _obj;");
                _src.AppendLine("\t\tpublic override string? ToString() => _obj.ToString();");
            }

            _src.AppendLine("\t}").AppendLine();
        }

        // Shim builder extension
        var instTypes = shims.Where(s => !s.IsStatic).ToArray();
        if (instTypes.Any())
        {
            foreach (var group in instTypes.GroupBy(s => s.ShimType.GenericArgs.Length))
            {
                var genArgs = getGenericArgList(group.First().ShimType);
                var targetDef = targetType.FullGenericName.Trim('<', '>') + genArgs;
                var classRawName = $"_Shimr_{R.Next(1000, 10000)}";
                var className = $"{classRawName}{genArgs}";
                _src.AppendLine($"\t// Shim builder extension for {group.Key} typeargs");
                _src.AppendLine($"\tpublic class {className}");
                _src.AppendLine("\t{");
                _src.AppendLine($"\t\tprivate readonly {targetDef}? _obj;");
                _src.AppendLine($"\t\tpublic {classRawName}({targetDef}? obj) => _obj = obj;");
                _src.AppendLine("\t\tpublic TShim? As<TShim>()");
                _src.AppendLine("\t\t{");
                _src.AppendLine($"\t\t\tif ({(targetType.IsValueType ? "!_obj.HasValue" : "_obj == null")})");
                _src.AppendLine("\t\t\t{");
                _src.AppendLine("\t\t\t\treturn default;");
                _src.AppendLine("\t\t\t}");
                _src.AppendLine("\t\t\tvar shimType = typeof(TShim).IsGenericType ? typeof(TShim).GetGenericTypeDefinition() : typeof(TShim);");

                foreach (var shim in group)
                {
                    _src.AppendLine($"\t\t\tif (shimType == typeof({shim.ShimType.FullGenericName}))");
                    _src.AppendLine("\t\t\t{");
                    _src.AppendLine($"\t\t\t\treturn (TShim)(object)new {shim.ShimSafeName}{genArgs}(_obj{(targetType.IsValueType ? ".Value" : "")});");
                    _src.AppendLine("\t\t\t}");
                }

                _src.AppendLine($"\t\t\tthrow new Exception(\"Invalid shim target type for this object: \" + typeof(TShim).FullName);");
                _src.AppendLine("\t\t}");
                _src.AppendLine("\t}");

                _src.AppendLine("\t[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"obj\")]");
                _src.AppendLine($"\tpublic static {className} Shim{genArgs}(this {targetDef}? obj)");
                _src.AppendLine($"\t\t=> new {className}(obj);");

                if (group.Key == 0)
                {
                    _src.AppendLine("\t[return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"obj\")]");
                    _src.AppendLine($"\tpublic static TShim? Shim{genArgs}<TShim>(this {targetDef}? obj)");
                    _src.AppendLine($"\t\t=> new {className}(obj).As<TShim>();");
                }
            }
        }

        _src.AppendLine("}");
    }

    public void CreateProperty(int indent, TypeDef returnType, string name, bool canRead, bool canWrite, string targetRefName, TypeDef? shimToType, string? targetAlias, string? implementTypeName)
    {
        var pad = new string('\t', indent);
        if (implementTypeName != null)
        {
            _src.Append($"{pad}{returnType.FullName} {implementTypeName}.{name}");
            targetRefName = $"(({implementTypeName}){targetRefName})";
        }
        else
        {
            _src.Append($"{pad}public {returnType.FullName} {name}");
        }
        if (canRead && !canWrite)
        {
            _src.Append($" => ");
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
            return;
        }
        _src.AppendLine();
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

    public void CreateMethod(int indent, TypeDef? returnType, string name, IEnumerable<MethodParameterDefinition> parameters, string targetRefName, TypeDef? shimToType, string? targetAlias, string? constructorTypeName, string? implementTypeName, string[]? genericConstraints)
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

        if (genericConstraints?.Any() == true)
        {
            foreach (var constraint in genericConstraints)
            {
                _src.AppendLine($"{pad}    {constraint}");
            }
        }

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
            var retvar = "obj";
            _src.Append($"{pad}\tvar obj = ");
            if (constructorTypeName != null)
            {
                _src.Append($"new {constructorTypeName}(");
            }
            else
            {
                _src.Append($"{targetRefName}.{targetAlias ?? name}(");
            }
            _src.Append(string.Join(", ", argList));
            _src.AppendLine(");");
            if (shimToType != null)
            {
                retvar = "shim";
                _src.AppendLine($"{pad}\tvar shim = {shimToType.Namespace}.{shimToType.Name.MakeSafeName()}ShimrExtension.Shim{returnType.GenericArgList}(obj).As<{returnType.FullName}>();");
            }
            _src.AppendLine($"{pad}\treturn {retvar};");
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
        if (!shims.Any())
        {
            return;
        }

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
            _src.AppendLine($"\t\t\treturn (T)(object)new {shim.TargetNamespace}.{shim.TargetSafeName}ShimrExtension.{shim.ShimSafeName}();");
            _src.AppendLine("\t\t}");
            first = false;
        }
        _src.AppendLine("\t\tthrow new System.NotImplementedException(\"Unable to create a static shim of type \" + typeof(T).FullName);");
        _src.AppendLine("\t}");

        _src.AppendLine("}");
    }
}
