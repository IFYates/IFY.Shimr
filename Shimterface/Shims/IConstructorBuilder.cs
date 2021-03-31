using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Shimterface.Shims
{
    public interface IConstructorBuilder
    {
        ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string strParamName);
		IILGenerator GetILGenerator();
    }
}