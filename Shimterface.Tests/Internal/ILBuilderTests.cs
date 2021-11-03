using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shimterface.Internal.Tests
{
    // NOTE: Not aiming at full coverage here, due to the complexity of what ILBuilder does.
    // Full coverage is provided when combined with the rest of the test suite.
    [TestClass]
    public class ILBuilderTests
    {
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
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Shimterface.Tests.dynamic"), AssemblyBuilderAccess.Run);
            var mod = asm.DefineDynamicModule("Shimterface.Tests.dynamic");
            var tb = mod.DefineType($"TestClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null, null);

            // Act
            var impl = tb.DefinePublicMethod("TestMethod", typeof(bool), new List<Type> { typeof(string), typeof(int) });
            ILBuilder.Ldarg(impl, input);

            // Assert
            Assert.AreEqual(expOffset, impl.ILOffset);
        }

        [TestMethod]
        public void DefinePublicMethod__With_paramTypes__Creates_method()
        {
            // Arrange
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Shimterface.Tests.dynamic"), AssemblyBuilderAccess.Run);
            var mod = asm.DefineDynamicModule("Shimterface.Tests.dynamic");
            var tb = mod.DefineType($"TestClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null, null);

            // Act
            var impl = tb.DefinePublicMethod("TestMethod", typeof(bool), new List<Type> { typeof(string), typeof(int) });
            impl.Emit(OpCodes.Ret);

            var type = tb.CreateType();

            // Assert
            var method = type.GetMethod("TestMethod", new[] { typeof(string), typeof(int) });
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(bool), method.ReturnType);
        }

        [TestMethod]
        public void WrapMethod__Proxy_without_method_implementation__NRE()
        {
            // Arrange
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Shimterface.Tests.dynamic"), AssemblyBuilderAccess.Run);
            var mod = asm.DefineDynamicModule("Shimterface.Tests.dynamic");
            var tb = mod.DefineType($"TestClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null, null);

            var method = typeof(ILBuilderTests).GetMethod(nameof(WrapMethod__Proxy_without_method_implementation__NRE));

            var binding = new ShimBinding(method)
            {
                ProxyImplementationMember = typeof(ShimBinding).GetProperty(nameof(ShimBinding.ProxyImplementationMember))
            };

            // Act
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                tb.WrapMethod(null, binding, method);
            });
        }
    }
}
