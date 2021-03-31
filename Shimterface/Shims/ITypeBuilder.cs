using System;
using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Shimterface.Shims
{
	public interface ITypeBuilder
	{
		Type CreateType();
        IConstructorBuilder DefineConstructor(MethodAttributes methodAttributes, CallingConventions standard, Type[] types);
		FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes);
		IMethodBuilder DefineMethod(string name, MethodAttributes methodAttributes, Type returnType, Type[] types);
    }
}
