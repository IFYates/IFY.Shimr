using System.Reflection;
using System.Reflection.Emit;

namespace IFY.Shimr.Internal.Tests;

[TestClass]
public class ILGeneratorExtensionsTests
{
    private static ILGenerator getGenerator()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("IFY.Shimr.Tests.dynamic"), AssemblyBuilderAccess.Run);
        var mod = asm.DefineDynamicModule("IFY.Shimr.Tests.dynamic");
        var tb = mod.DefineType($"TestClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null, null);
        return tb.DefinePublicMethod("TestMethod", typeof(bool), [typeof(string), typeof(int)]);
    }

    [TestMethod]
    [DataRow((byte)0, 1)]
    [DataRow((byte)1, 1)]
    [DataRow((byte)2, 1)]
    [DataRow((byte)3, 1)]
    [DataRow((byte)4, 2)]
    [DataRow((byte)5, 2)]
    public void Ldarg__Provides_efficient_bytecode(byte input, int expOffset)
    {
        // Arrange
        var impl = getGenerator();

        // Act
        ILGeneratorExtensions.Ldarg(impl, input);

        // Assert
        Assert.AreEqual(expOffset, impl.ILOffset);
    }

    [TestMethod]
    [DataRow((byte)0, 1)]
    [DataRow((byte)1, 1)]
    [DataRow((byte)2, 1)]
    [DataRow((byte)3, 1)]
    [DataRow((byte)4, 1)]
    [DataRow((byte)5, 1)]
    [DataRow((byte)6, 1)]
    [DataRow((byte)7, 1)]
    [DataRow((byte)8, 1)]
#if NETCOREAPP3_1
    [DataRow((byte)9, 5)]
    [DataRow((byte)10, 5)]
#else
    [DataRow((byte)9, 2)]
    [DataRow((byte)10, 2)]
#endif
    public void Ldc_I4__Provides_efficient_bytecode(byte input, int expOffset)
    {
        // Arrange
        var impl = getGenerator();

        // Act
        ILGeneratorExtensions.Ldc_I4(impl, input);

        // Assert
        Assert.AreEqual(expOffset, impl.ILOffset);
    }

    [TestMethod]
    [DataRow((byte)0, 1)]
    [DataRow((byte)1, 1)]
    [DataRow((byte)2, 1)]
    [DataRow((byte)3, 1)]
    [DataRow((byte)4, 6)]
    [DataRow((byte)5, 6)]
    public void Ldloc__Provides_efficient_bytecode(byte input, int expOffset)
    {
        // Arrange
        var impl = getGenerator();

        // Act
        ILGeneratorExtensions.Ldloc(impl, input);

        // Assert
        Assert.AreEqual(expOffset, impl.ILOffset);
    }

    [TestMethod]
    [DataRow((byte)0, 1)]
    [DataRow((byte)1, 1)]
    [DataRow((byte)2, 1)]
    [DataRow((byte)3, 1)]
    [DataRow((byte)4, 6)]
    [DataRow((byte)5, 6)]
    public void Stloc__Provides_efficient_bytecode(byte input, int expOffset)
    {
        // Arrange
        var impl = getGenerator();

        // Act
        ILGeneratorExtensions.Stloc(impl, input);

        // Assert
        Assert.AreEqual(expOffset, impl.ILOffset);
    }
}