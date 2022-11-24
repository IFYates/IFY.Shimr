#if !NETCOREAPP3_1_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage] // Not our code
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

#endif
