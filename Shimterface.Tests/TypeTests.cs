using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class TypeTests
	{
		public interface ITest
		{
			void Test();
		}

		public interface ITestInt : IShim
		{
			void Test();
		}

		public interface ITestIntT : IShim<TestClass>
		{
			void Test();
		}

		public class TestClass
		{
			public void Test()
			{
			}
		}

		[TestInitialize]
		public void init()
		{
			ShimBuilder.ResetState();
		}
		
		[TestMethod]
		public void Result_is_always_IShim()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);

			Assert.IsTrue(shim is IShim);
		}

		[TestMethod]
		public void Result_is_always_IShimT()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);

			Assert.IsTrue(shim is IShim<TestClass>);
		}
		
		[TestMethod]
		public void Can_unshim_original_object_by_cast()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);

			Assert.AreSame(obj, ((IShim)shim).Unshim());
		}

		[TestMethod]
		public void Can_unshim_original_object_by_int()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITestInt>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_unshim_original_object_by_IntT()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITestIntT>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod, Ignore]
		public void Can_unshim_original_object_with_explicit_cast()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);
			
			var x = (TestClass)shim; // Not possible

			Assert.AreSame(obj, (TestClass)shim);
		}
	}
}
