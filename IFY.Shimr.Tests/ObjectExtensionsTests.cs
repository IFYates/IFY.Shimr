using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shimterface.Extensions;
using System.Collections.Generic;

namespace IFY.Shimr.Extensions.Tests
{
	[TestClass]
	public class ObjectExtensionsTests
	{
		public interface ITestShim
		{
			void Test();
		}

		[TestMethod]
		public void Shim__Null_enumerable__Null()
		{
			// Arrange
			IEnumerable<object> inst = null;

			// Act
			var shim = ObjectExtensions.Shim<ITestShim>(inst);

			// Assert
			Assert.IsNull(shim);
		}

		[TestMethod]
		public void Unshim__Instance_is_type__Return_instance()
		{
			// Arrange
			string obj = "";

			// Act
			var shim = ObjectExtensions.Unshim<string>(obj);

			// Assert
			Assert.AreSame(obj, shim);
		}
	}
}
