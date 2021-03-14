using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class InheritanceTests
	{
		public interface IRootShim : IChildShim
		{
			int TestA();
		}
		public interface IChildShim : IGrandchildShim
		{
			int TestB();
		}
		public interface IGrandchildShim
		{
			int TestC();
		}

		public class TestClass
		{
			public int TestA()
			{
				return 1;
			}
			public int TestB()
			{
				return 2;
			}
			public int TestC()
			{
				return 3;
			}
		}

		[TestMethod]
		public void Can_shim_full_hierarchy()
		{
			var obj = new TestClass();

			var shim = obj.Shim<IRootShim>();

			Assert.AreEqual(1, shim.TestA());
			Assert.AreEqual(2, shim.TestB());
			Assert.AreEqual(3, shim.TestC());
		}
	}
}
