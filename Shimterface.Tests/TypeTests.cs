using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class TypeTests
	{
		public interface ITest : IShim
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
		public void Result_is_IShim()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);

			Assert.IsTrue(shim is IShim);
		}

		[TestMethod]
		public void Can_unshim_original_object()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_unshim_original_object_with_explicit_cast()
		{
			var obj = new TestClass();

			var shim = ShimBuilder.Shim<ITest>(obj);
			
			var x = (TestClass)shim; // Not possible

			Assert.AreSame(obj, (TestClass)shim);
		}
	}
}
