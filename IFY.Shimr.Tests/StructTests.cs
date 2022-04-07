using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class StructTests
	{
		public interface IToString
		{
			string ToString();
		}

		[TestMethod]
		public void Can_unshim_Int32()
		{
			var num = 12345;
			var shim = ShimBuilder.Shim<IToString>(num);
			var sh2 = (IShim)shim;
			var num2 = (Int32)sh2.Unshim();
			Assert.AreEqual(num, num2);
		}

		[TestMethod]
		public void Can_shim_Int32()
		{
			var num = 12345;
			var shim = ShimBuilder.Shim<IToString>(num);
			Assert.AreEqual(num.ToString(), shim.ToString());
		}
	}
}
