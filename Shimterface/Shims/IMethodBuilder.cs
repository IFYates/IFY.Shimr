using System;
using System.Reflection.Emit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Shimterface.Shims
{
	public interface IMethodBuilder
	{
        GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names);
		IILGenerator GetILGenerator();
		void SetParameters(params Type[] parameterTypes);
		void SetReturnType(Type returnType);
    }
}
