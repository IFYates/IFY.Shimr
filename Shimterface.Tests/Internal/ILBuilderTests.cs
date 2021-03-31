using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shimterface.Shims;
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
		public void DefinePublicMethod__With_paramTypes__Creates_method()
		{
			// Arrange
			var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Shimterface.Tests.dynamic"), AssemblyBuilderAccess.Run);
			var mod = asm.DefineDynamicModule("Shimterface.Tests.dynamic");
			var tb = mod.DefineType($"TestClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null, null);

			// Act
			var impl = tb.Shim<ITypeBuilder>().DefinePublicMethod("TestMethod", typeof(bool), new List<Type> { typeof(string), typeof(int) });
			impl.Emit(OpCodes.Ret);

			var type = tb.CreateType();

			// Assert
			var method = type.GetMethod("TestMethod", new [] { typeof(string), typeof(int) });
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
				tb.Shim<ITypeBuilder>().WrapMethod(null, binding, method);
			});
		}
	}
}
