using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	/// <summary>
	/// Tests around extending/replacing shim functionality
	/// https://github.com/IanYates83/Shimterface/issues/3
	/// </summary>
	[TestClass]
	public class ExtendedFunctionalityTests_Method
	{
		public interface ITestShim
		{
			void MethodA();
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_NoMethodB
		{
			public bool MethodACalled { get; private set; }
			public void MethodA()
			{
				MethodACalled = true;
			}
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_HasMethodB
		{
			public bool MethodACalled { get; private set; }
			public void MethodA()
			{
				MethodACalled = true;
			}

			public bool MethodBCalled { get; private set; }
			public void MethodB()
			{
				MethodBCalled = true;
			}
		}

        #region Override

		public interface ITestShim_MethodOverride : ITestShim
		{
			[ShimProxy(typeof(TestImpl_MethodOverride), ProxyBehaviour.Override)]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodOverride
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_MethodOverride obj)
			{
				MethodBCalledObj = obj;
			}
		}
		
		[TestMethod]
		public void Shim_can_define_proxy_to_override_member()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_MethodOverride>();

			// Act
			shim.MethodB();

			// Assert
			Assert.IsFalse(obj.MethodBCalled);
			Assert.AreSame(obj, TestImpl_MethodOverride.MethodBCalledObj);
		}
		
		public interface ITestShim_DefaultOverride : ITestShim
		{
			[ShimProxy(typeof(TestImpl_DefaultOverride))]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_DefaultOverride
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_DefaultOverride obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_override_member_by_default()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_DefaultOverride>();

			// Act
			shim.MethodB();

			// Assert
			Assert.IsFalse(obj.MethodBCalled);
			Assert.AreSame(obj, TestImpl_DefaultOverride.MethodBCalledObj);
		}
		
		public interface ITestShim_MethodOverrideAlias : ITestShim
		{
			[Shim("MethodB")]
			[ShimProxy(typeof(TestImpl_MethodOverrideAlias), "MethodB", ProxyBehaviour.Override)]
			void MethodC();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodOverrideAlias
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_MethodOverrideAlias obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_override_member_with_alias_and_autoshim()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_MethodOverrideAlias>();

			// Act
			shim.MethodC(); // Actually TestImpl_MethodOverrideAlias.MethodB

			// Assert
			Assert.IsFalse(obj.MethodBCalled);
			Assert.AreSame(obj, TestImpl_MethodOverrideAlias.MethodBCalledObj);
		}
		
		public interface ITestShim_CallBase : ITestShim
		{
			[ShimProxy(typeof(TestImpl_CallBase))]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_CallBase
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_CallBase obj)
			{
				MethodBCalledObj = obj;
				obj.MethodB();
			}
		}

		[TestMethod]
		public void Override_member_can_call_shimmed_implementation()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_CallBase>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(obj, TestImpl_MethodOverrideAlias.MethodBCalledObj);
			Assert.IsTrue(obj.MethodBCalled);
		}
		
		public interface ITestShim_MissingBase : ITestShim
		{
			[ShimProxy(typeof(TestImpl_MissingBase), ProxyBehaviour.Override)]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MissingBase
		{
			public static void MethodB(ITestShim_MissingBase obj)
			{
				obj.ToString();
			}
		}

		[TestMethod]
		public void Override_member_must_exist_in_shimmed_type()
		{
			// Arrange
			var obj = new TestClass_NoMethodB();
			
			// Act
			obj.Shim<ITestShim_MissingBase>();
		}
		
		public interface ITestShim_BadImpl : ITestShim
		{
			[ShimProxy(typeof(TestImpl_BadImpl))]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_BadImpl
		{
			public static void MethodB()
			{
			}
		}

		[TestMethod]
		public void Override_implementation_must_take_compatible_first_param()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			
			// Act
			obj.Shim<ITestShim_BadImpl>();
		}

        #endregion Override

        #region Add
		
		public interface ITestShim_MethodAdd : ITestShim
		{
			[ShimProxy(typeof(TestImpl_MethodAdd), ProxyBehaviour.Add)]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodAdd
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_MethodAdd obj)
			{
				MethodBCalledObj = obj;
			}
		}
		
        [TestMethod]
		public void Shim_can_define_proxy_to_add_member()
		{
			// Arrange
			var obj = new TestClass_NoMethodB();
			var shim = obj.Shim<ITestShim_MethodAdd>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(obj, TestImpl_MethodAdd.MethodBCalledObj);
		}
		
		public interface ITestShim_DefaultAdd : ITestShim
		{
			[ShimProxy(typeof(TestImpl_DefaultAdd))]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_DefaultAdd
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_DefaultAdd obj)
			{
				MethodBCalledObj = obj;
			}
		}
		
		[TestMethod]
		public void Shim_can_define_proxy_to_add_member_by_default()
		{
			// Arrange
			var obj = new TestClass_NoMethodB();
			var shim = obj.Shim<ITestShim_DefaultAdd>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(obj, TestImpl_DefaultAdd.MethodBCalledObj);
		}
		
		public interface ITestShim_MethodAddAlias : ITestShim
		{
			[ShimProxy(typeof(TestImpl_MethodAddAlias), "MethodB", ProxyBehaviour.Add)]
			void MethodC();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodAddAlias
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim_MethodAddAlias obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_add_member_with_alias()
		{
			// Arrange
			var obj = new TestClass_NoMethodB();
			var shim = obj.Shim<ITestShim_MethodAddAlias>();

			// Act
			shim.MethodC(); // Actually TestImpl_MethodOverrideAlias.MethodB

			// Assert
			Assert.AreSame(obj, TestImpl_MethodAddAlias.MethodBCalledObj);
		}
		
		[TestMethod]
		public void Added_member_must_not_exist_in_shimmed_type()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();

			// Act
			obj.Shim<ITestShim_MethodAdd>();
		}
		
		[TestMethod]
		public void Added_implementation_must_take_compatible_first_param()
		{
			// Arrange
			var obj = new TestClass_NoMethodB();

			// Act
			obj.Shim<ITestShim_BadImpl>();
		}

        #endregion Add
		
		public interface ITestShim_MissingImpl : ITestShim
		{
			[ShimProxy(typeof(TestImpl_DefaultOverride))]
			void MethodC(); // Not in TestImpl_Default
		}

		[TestMethod]
		public void Proxy_member_must_exist_in_impl_type()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			
			// Act
			obj.Shim<ITestShim_MissingImpl>();
		}
	}
}
