using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
			[StaticShim(typeof(TestClass), IsConstructor = true)]
			IInstanceInterface Create(string arg);
			[ConstructorShim(typeof(TestClass))]
			IInstanceInterface New(int arg);
		}

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
		[ExpectedException(typeof(ArgumentException))]
		public void Can_not_shim_to_constructor_with_void_return_type()
		{
			ShimBuilder.Create<IFactoryInterface2>();
		}
		
		public interface IFactoryInterface3
		{
			[ConstructorShim(typeof(TestClass))]
			int New(int arg);
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Can_not_shim_to_constructor_with_incorrect_return_type()
		{
			ShimBuilder.Create<IFactoryInterface3>();
		}
	}
}
