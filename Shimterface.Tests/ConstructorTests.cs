using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Shimterface.Tests
{
	[TestClass]
	public class ConstructorTests
	{
		public interface IInstanceInterface
		{
			string Result { get; }
		}
		public interface IFactoryInterface
		{
			[ConstructorShim(typeof(TestClass))]
			IInstanceInterface Create(string arg);
			[ConstructorShim(typeof(TestClass))]
			IInstanceInterface New(int arg);
		}

		[ExcludeFromCodeCoverage]
		public class TestClass
		{
			public string Result { get; private set; }

			public TestClass(string arg)
			{
				Result = $"A_{arg}";
			}
			public TestClass(int arg)
			{
				Result = $"B_{arg}";
			}
			public TestClass(bool arg)
			{
				Result = $"C_{arg}";
			}
		}

		[TestMethod]
		public void Can_shim_to_constructor()
		{
			var shim = ShimBuilder.Create<IFactoryInterface>();
			var instA = shim.Create("one");
			var instB = shim.New(2);

			Assert.AreEqual("A_one", instA.Result);
			Assert.AreEqual("B_2", instB.Result);
			Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass));
			Assert.IsInstanceOfType(((IShim)instB).Unshim(), typeof(TestClass));
		}

		public interface IFactoryInterface2
		{
			[ConstructorShim(typeof(TestClass))]
			void New(int arg);
		}
		[TestMethod]
		public void Can_not_shim_to_constructor_with_void_return_type()
		{
			Assert.ThrowsException<ArgumentException>(() =>
			{
				ShimBuilder.Create<IFactoryInterface2>();
			});
		}

		public interface IFactoryInterface3
		{
			[ConstructorShim(typeof(TestClass))]
			int New(int arg);
		}
		[TestMethod]
		public void Can_not_shim_to_constructor_with_incorrect_return_type()
		{
			Assert.ThrowsException<ArgumentException>(() =>
			{
				ShimBuilder.Create<IFactoryInterface3>();
			});
		}
		
		[StaticShim(typeof(TestClass))]
		public interface IFactoryInterface4
		{
			[ConstructorShim]
			IInstanceInterface New(int arg);
		}
		[TestMethod]
		public void Constructor_shim_will_inherit_target_type_from_interface()
		{
			var shim = ShimBuilder.Create<IFactoryInterface4>();
			var inst = shim.New(4);

			Assert.AreEqual("B_4", inst.Result);
			Assert.IsInstanceOfType(((IShim)inst).Unshim(), typeof(TestClass));
		}
	}
}
