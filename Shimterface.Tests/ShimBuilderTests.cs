using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Shimterface.Tests
{
    // NOTE: Not aiming at full coverage here, due to the complexity of what TypeHelpers does.
    // Full coverage is provided when combined with the rest of the test suite.
    [TestClass]
	public class ShimBuilderTests
	{
		[ExcludeFromCodeCoverage]
		public class TestClass
		{
		}
		[ExcludeFromCodeCoverage]
		public class ShimClass : ITestShim
		{
		}

		public interface ITestShim
		{
		}

		[StaticShimAttribute(typeof(TestClass))]
		public interface IStaticShim
		{
		}

		[TestInitialize]
		public void ResetState()
		{
			ShimBuilder.ResetState();
		}

		[TestMethod]
		public void Shim__Builds_valid_type_with_unique_name()
		{
			// Arrange
			var obj = new TestClass();

			// Act
			var shim = obj.Shim<ITestShim>();

			// Assert
			Assert.IsTrue(shim.GetType().FullName.StartsWith($"Shim_TestClass_{typeof(TestClass).GetHashCode()}_ITestShim_{typeof(ITestShim).GetHashCode()}"));
			Assert.IsTrue((shim.GetType().Attributes & (TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout)) > 0);
		}

		[TestMethod]
		public void Shim__Builds_type_shim_combination_only_once()
		{
			// Arrange
			var obj = new TestClass();

			// Act
			var shim1 = obj.Shim<ITestShim>();
			var shim2 = obj.Shim<ITestShim>();

			// Assert
			Assert.AreEqual(shim1.GetType().GetHashCode(), shim2.GetType().GetHashCode());
		}

		[TestMethod]
		public void Shim__Non_interface_type__Fail()
		{
			// Act
			Assert.ThrowsException<NotSupportedException>(() =>
			{
				ShimBuilder.Shim(typeof(object), new object());
			});
		}
		
		[TestMethod]
		public void Shim__Instance_of_interface__Returns_instance()
		{
			// Arrange
			var obj = new ShimClass();

			// Act
			var shim = ShimBuilder.Shim(typeof(ITestShim), obj);

			// Assert
			Assert.AreSame(obj, shim);
		}

		[TestMethod]
		public void Shim__Null_enumerable__Null()
		{
			// Arrange
			IEnumerable<object> inst = null;

			// Act
			var shim = inst.Shim<ITestShim>();

			// Assert
			Assert.IsNull(shim);
		}

		[TestMethod]
		public void Create__Builds_valid_type_with_unique_name()
		{
			// Act
			var shim = ShimBuilder.Create<IStaticShim>();

			// Assert
			Assert.IsTrue(shim.GetType().FullName == $"IStaticShim_{typeof(IStaticShim).GetHashCode()}");
			Assert.IsTrue((shim.GetType().Attributes & (TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout)) > 0);
		}

		[TestMethod]
		public void Create__Builds_type_shim_combination_only_once()
		{
			// Act
			var shim1 = ShimBuilder.Create<IStaticShim>();
			var shim2 = ShimBuilder.Create<IStaticShim>();

			// Assert
			Assert.AreEqual(shim1.GetType().GetHashCode(), shim2.GetType().GetHashCode());
		}

		[TestMethod]
		public void getFactoryType__StaticShimAttribute_IsConstructor__Fail()
		{
			// Arrange
			var typeMock = new Mock<Type>(MockBehavior.Strict);
			typeMock.SetupGet(m => m.FullName)
				.Returns("TestClass");
			typeMock.SetupGet(m => m.Name)
				.Returns("TestClass");
			typeMock.Setup(m => m.GetHashCode())
				.Returns(1);
			typeMock.SetupGet(m => m.MemberType)
				.Returns(MemberTypes.TypeInfo);
			typeMock.Setup(m => m.GetCustomAttributes(typeof(StaticShimAttribute), false))
				.Returns(new object[] { new StaticShimAttribute(typeof(object)) { IsConstructor = true } });

			// Act
			Assert.ThrowsException<NotSupportedException>(() =>
			{
				ShimBuilder.getFactoryType(typeMock.Object);
			});
		}
	}
}
