using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	/// <summary>
	/// Tests around extending/replacing shim functionality
	/// https://github.com/IFYates/Shimterface/issues/3
	/// </summary>
	[TestClass]
	public class ExtendedFunctionalityTests_Method
	{
		[TestInitialize]
		public void ResetState()
		{
			ShimBuilder.ResetState();
		}

		public interface ITestShim
		{
			void MethodA();
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_OnlyMethodA
		{
			public TestClass_OnlyMethodA() { }

			public bool MethodACalled { get; private set; }
			public void MethodA()
			{
				MethodACalled = true;
			}
		}
		
		[ExcludeFromCodeCoverage]
		public class TestClass_HasMethodB : TestClass_OnlyMethodA
		{
			public bool MethodBCalled { get; private set; }
			public void MethodB()
			{
				MethodBCalled = true;
			}
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_HasMethodC : TestClass_OnlyMethodA
		{
			public string MethodCCalledWith { get; private set; }
			public void MethodC(string arg)
			{
				MethodCCalledWith = arg;
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
			Assert.AreSame(shim, TestImpl_MethodOverride.MethodBCalledObj);
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
			Assert.AreSame(shim, TestImpl_DefaultOverride.MethodBCalledObj);
		}

		public interface ITestShim_MethodOverrideAlias : ITestShim
		{
			[Shim("MethodB")]
			[ShimProxy(typeof(TestImpl_MethodOverrideAlias), "MethodD", ProxyBehaviour.Override)]
			void MethodC();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodOverrideAlias
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodD(ITestShim_MethodOverrideAlias obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_override_member_with_alias()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_MethodOverrideAlias>();

			// Act
			shim.MethodC(); // Actually TestImpl_MethodOverrideAlias.MethodB

			// Assert
			Assert.IsFalse(obj.MethodBCalled);
			Assert.AreSame(shim, TestImpl_MethodOverrideAlias.MethodBCalledObj);
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
				if (MethodBCalledObj != null)
				{
					throw new InvalidOperationException("recursion");
				}

				MethodBCalledObj = obj;
				obj.MethodB();
			}
		}

		[TestMethod]
		[Timeout(1_000)] // Incase of recursion
		public void Override_member_can_call_shimmed_implementation()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_CallBase>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(shim, TestImpl_CallBase.MethodBCalledObj);
			Assert.IsTrue(obj.MethodBCalled);
		}

		[ExcludeFromCodeCoverage]
		public class TestClass_MethodFails
		{
#pragma warning disable CA1822 // Mark members as static
			public void Fail()
			{
				throw new InvalidOperationException();
			}
#pragma warning restore CA1822 // Mark members as static
		}
		public interface ITestShim_MethodFails
		{
			[ShimProxy(typeof(TestImpl_MethodFails), ProxyBehaviour.Override)]
			void Fail();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodFails
		{
			public static int CallPre { get; private set; }
			public static int CallPost { get; private set; }
			public static int CallEnd { get; private set; }

			public static void Fail(ITestShim_MethodFails inst)
			{
				++CallPre;
				try
				{
					inst.Fail();
					++CallPost;
				}
				finally
				{
					++CallEnd;
				}
			}
		}

		[TestMethod]
		[Timeout(1_000)] // Incase of recursion
		public void Failure_during_underlying_call_does_not_break_proxy()
		{
			// Arrange
			var obj = new TestClass_MethodFails();
			var shim = obj.Shim<ITestShim_MethodFails>();

			var fails = 0;

			// Act
			try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }
			try { shim.Fail(); } catch (InvalidOperationException) { ++fails; }

			// Assert
			Assert.AreEqual(2, fails);
			Assert.AreEqual(2, TestImpl_MethodFails.CallPre);
			Assert.AreEqual(0, TestImpl_MethodFails.CallPost);
			Assert.AreEqual(2, TestImpl_MethodFails.CallEnd);
		}

		public interface ITestShim_ArgImpl : ITestShim
		{
			[ShimProxy(typeof(TestImpl_ArgImpl))]
			void MethodB();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_ArgImpl
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodB(ITestShim obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Override_implementation_can_invoke_on_compatible_arg()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();
			var shim = obj.Shim<ITestShim_ArgImpl>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(shim, TestImpl_ArgImpl.MethodBCalledObj);
		}
		
		public interface ITestShim_WithArg : ITestShim
		{
			[ShimProxy(typeof(TestImpl_WithArg))]
			void MethodC(string arg);
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_WithArg
		{
			public static string MethodCCalledWith { get; set; }
			public static void MethodC(ITestShim obj, string arg)
			{
				obj.ToString();
				MethodCCalledWith = arg;
			}
		}

		[TestMethod]
		public void Override_implementation_can_invoke_with_args()
		{
			// Arrange
			var obj = new TestClass_HasMethodC();
			var shim = obj.Shim<ITestShim_WithArg>();

			// Act
			obj.MethodC("test1");
			shim.MethodC("test2");

			// Assert
			Assert.AreEqual("test1", obj.MethodCCalledWith);
			Assert.AreEqual("test2", TestImpl_WithArg.MethodCCalledWith);
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
			var obj = new TestClass_OnlyMethodA();

			// Act
			Assert.ThrowsException<InvalidCastException>(() =>
			{
				obj.Shim<ITestShim_MissingBase>();
			});
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
			Assert.ThrowsException<MissingMemberException>(() =>
			{
				obj.Shim<ITestShim_BadImpl>();
			});
		}
		
		public interface ITestShim_ChangeShim : ITestShim
		{
			[ShimProxy(typeof(TestImpl_ChangeShim))]
			new void MethodA();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_ChangeShim
		{
			public static ITestShim_ChangeShim MethodACalledObj { get; set; }
			public static void MethodA(ITestShim_ChangeShim inst)
			{
				MethodACalledObj = inst;
			}
		}

		[TestMethod]
		public void Can_change_shim_in_hierarchy()
		{
			// Arrange
			var obj = new TestClass_OnlyMethodA();
			var shim = obj.Shim<ITestShim_ChangeShim>();

			// Act
			shim.MethodA();

			// Assert
			Assert.IsFalse(obj.MethodACalled);
			Assert.AreSame(shim, TestImpl_ChangeShim.MethodACalledObj);
		}

		public interface ITestShim_Constructor
		{
			[ConstructorShim(typeof(TestClass_OnlyMethodA))]
			[ShimProxy(typeof(TestImpl_DefaultOverride))]
			ITestShim MethodB();
		}
		[TestMethod]
		public void Cannot_override_constructor()
		{
			// Act
			Assert.ThrowsException<InvalidCastException>(() =>
			{
				ShimBuilder.Create<ITestShim_Constructor>();
			});
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
			var obj = new TestClass_OnlyMethodA();
			var shim = obj.Shim<ITestShim_MethodAdd>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(shim, TestImpl_MethodAdd.MethodBCalledObj);
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
			var obj = new TestClass_OnlyMethodA();
			var shim = obj.Shim<ITestShim_DefaultAdd>();

			// Act
			shim.MethodB();

			// Assert
			Assert.AreSame(shim, TestImpl_DefaultAdd.MethodBCalledObj);
		}

		public interface ITestShim_MethodAddAlias : ITestShim
		{
			[ShimProxy(typeof(TestImpl_MethodAddAlias), "MethodD", ProxyBehaviour.Add)]
			void MethodC();
		}
		[ExcludeFromCodeCoverage]
		public class TestImpl_MethodAddAlias
		{
			public static ITestShim MethodBCalledObj { get; set; }
			public static void MethodD(ITestShim_MethodAddAlias obj)
			{
				MethodBCalledObj = obj;
			}
		}

		[TestMethod]
		public void Shim_can_define_proxy_to_add_member_to_alias_impl()
		{
			// Arrange
			var obj = new TestClass_OnlyMethodA();
			var shim = obj.Shim<ITestShim_MethodAddAlias>();

			// Act
			shim.MethodC(); // Actually TestImpl_MethodOverrideAlias.MethodB

			// Assert
			Assert.AreSame(shim, TestImpl_MethodAddAlias.MethodBCalledObj);
		}

		[TestMethod]
		public void Added_member_must_not_exist_in_shimmed_type()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();

			// Act
			Assert.ThrowsException<InvalidCastException>(() =>
			{
				obj.Shim<ITestShim_MethodAdd>();
			});
		}

		[TestMethod]
		public void Added_implementation_must_take_compatible_first_param()
		{
			// Arrange
			var obj = new TestClass_OnlyMethodA();

			// Act
			Assert.ThrowsException<MissingMemberException>(() =>
			{
				obj.Shim<ITestShim_BadImpl>();
			});
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
			Assert.ThrowsException<MissingMemberException>(() =>
			{
				obj.Shim<ITestShim_MissingImpl>();
			});
		}

		[TestMethod]
		public void Factory_cannot_define_proxy()
		{
			// Arrange
			var obj = new TestClass_HasMethodB();

			// Act
			Assert.ThrowsException<MissingMemberException>(() =>
			{
				obj.Shim<ITestShim_MissingImpl>();
			});
		}
	}
}
