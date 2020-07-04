using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Shimterface.Tests
{
	[TestClass]
	public class SignatureArrayChangeTests
	{
		public class ReturnTypeTest
		{
			private string[] _array = new[] { "a", "b", "c" };
			public string[] GetArray()
			{
				return _array;
			}
			public void SetArray(string[] arr)
			{
				_array = arr;
			}
		}
		public interface IToString
		{
			string ToString();
		}
		public interface ICoveredArrayMethodTest
		{
			[TypeShim(typeof(string[]))]
			IToString[] GetArray();
		}
		public interface ICoveredArraySetMethodTest
		{
			void SetArray([TypeShim(typeof(string[]))] IToString[] arr);
		}

		[TestMethod]
		public void Can_shim_method_with_covered_array_return_type()
		{
			var obj = new ReturnTypeTest();

			var shim = (IShim)ShimBuilder.Shim<ICoveredArrayMethodTest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_get_array_result_as_shims()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredArrayMethodTest>(obj);
			var res = shim.GetArray();

			Assert.AreEqual(3, res.Length);
			var arr = res.Cast<IShim>().Select(s => s.Unshim()).ToArray();
			CollectionAssert.AreEqual(obj.GetArray(), arr);
		}

		//TODO: can shim, cannot set
		[TestMethod]
		public void Can_set_array_parameter_as_appropriate_shims()
		{
			var obj = new ReturnTypeTest();
			var data = new[] { "1", "2", "3" };

			var shim = ShimBuilder.Shim<ICoveredArraySetMethodTest>(obj);
			var arr = ShimBuilder.Shim<IToString>(data);
			shim.SetArray(arr);

			var res = obj.GetArray();
			CollectionAssert.AreEqual(data, res);
		}
	}
}
