using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	/// <summary>
	/// Tests around extending/replacing shim functionality
	/// https://github.com/IanYates83/Shimterface/issues/3
	/// </summary>
	[TestClass]
	public class ExtendedFunctionalityTests_Field
	{
		[ExcludeFromCodeCoverage]
		public class TestClass_NoField
		{
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_HasField
		{
			public string Field;
		}

		public interface ITestShim_AddField
		{
			[ShimProxy(typeof(TestImpl_AddField), ProxyBehaviour.Add)]
			string Field { get; set; }
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_AddField
		{
			public static string Field { get; set; }
		}

		[TestMethod]
		public void Can_add_field_proxy()
		{
			// Arrange
			var obj = new TestClass_NoField();
			var shim = obj.Shim<ITestShim_AddField>();

			// Act
			shim.Field = "test";

			// Assert
			Assert.AreEqual("test", TestImpl_AddField.Field);
		}
		
		public interface ITestShim_AddFieldDefault
		{
			[ShimProxy(typeof(TestImpl_AddFieldDefault))]
			string Field { get; set; }
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_AddFieldDefault
		{
			public static string Field { get; set; }
		}

		[TestMethod]
		public void Can_add_field_proxy_by_default()
		{
			// Arrange
			var obj = new TestClass_NoField();
			var shim = obj.Shim<ITestShim_AddFieldDefault>();

			// Act
			shim.Field = "test";

			// Assert
			Assert.AreEqual("test", TestImpl_AddFieldDefault.Field);
		}

		[TestMethod]
		public void Cannot_add_existing_field()
		{
			// Arrange
			var obj = new TestClass_HasField();

			// Act
			Assert.ThrowsException<InvalidCastException>(() =>
			{
				obj.Shim<ITestShim_AddField>();
			});
		}
		
		public interface ITestShim_OverrideField
		{
			[ShimProxy(typeof(TestImpl_OverrideField), ProxyBehaviour.Override)]
			string Field { get; set; }
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_OverrideField
		{
			public static string Field { get; set; }
		}

		[TestMethod]
		public void Can_override_field_proxy()
		{
			// Arrange
			var obj = new TestClass_HasField();
			var shim = obj.Shim<ITestShim_OverrideField>();

			// Act
			shim.Field = "test";

			// Assert
			Assert.IsNull(obj.Field);
			Assert.AreSame("test", TestImpl_OverrideField.Field);
		}

		public interface ITestShim_OverrideFieldDefault
		{
			[ShimProxy(typeof(TestImpl_OverrideFieldDefault))]
			string Field { get; set; }
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_OverrideFieldDefault
		{
			public static string Field { get; set; }
		}

		[TestMethod]
		public void Can_override_field_proxy_by_default()
		{
			// Arrange
			var obj = new TestClass_HasField();
			var shim = obj.Shim<ITestShim_OverrideFieldDefault>();

			// Act
			shim.Field = "test";

			// Assert
			Assert.IsNull(obj.Field);
			Assert.AreSame("test", TestImpl_OverrideFieldDefault.Field);
		}

		[TestMethod]
		public void Cannot_override_missing_field()
		{
			// Arrange
			var obj = new TestClass_NoField();

			// Act
			Assert.ThrowsException<InvalidCastException>(() =>
			{
				obj.Shim<ITestShim_OverrideField>();
			});
		}

		public interface ITestShim_OverrideFieldAlias
		{
			[Shim("Field")]
			[ShimProxy(typeof(TestImpl_OverrideFieldAlias), "FieldProxy", ProxyBehaviour.Override)]
			string FieldShim { get; set; }
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_OverrideFieldAlias
		{
			public static string FieldProxy { get; set; }
		}

		[TestMethod]
		public void Can_override_field_proxy_with_aliases()
		{
			// Arrange
			var obj = new TestClass_HasField();
			var shim = obj.Shim<ITestShim_OverrideFieldAlias>();

			// Act
			shim.FieldShim = "test";

			// Assert
			Assert.IsNull(obj.Field);
			Assert.AreSame("test", TestImpl_OverrideFieldAlias.FieldProxy);
		}
	}
}
