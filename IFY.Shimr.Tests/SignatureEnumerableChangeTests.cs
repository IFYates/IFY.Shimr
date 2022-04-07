using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Shimterface.Extensions;

namespace Shimterface.Tests
{
	[TestClass]
	public class SignatureEnumerableChangeTests
	{
		public class ReturnTypeTest
		{
			private IEnumerable<string> _enum = new[] { "a", "b", "c" };
			public IEnumerable<string> GetEnum()
			{
				return _enum;
			}
			public void SetEnum(IEnumerable<string> arr)
			{
				_enum = arr;
			}
		}
		public interface IToString
		{
			string ToString();
		}
		public interface ICoveredEnumMethodTest
		{
			IEnumerable<IToString> GetEnum();
		}
		public interface ICoveredEnumSetMethodTest
		{
			void SetEnum([TypeShim(typeof(IEnumerable<string>))] IEnumerable<IToString> arr);
		}

		[TestMethod]
		public void Can_shim_method_with_covered_enum_return_type()
		{
			var obj = new ReturnTypeTest();

			var shim = (IShim)ShimBuilder.Shim<ICoveredEnumMethodTest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_get_enum_result_as_shims()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredEnumMethodTest>(obj);
			var res = shim.GetEnum();

			Assert.AreEqual(3, res.Count());
			var arr = res.Unshim<string>().ToArray();
			CollectionAssert.AreEqual(obj.GetEnum().ToArray(), arr);
		}

		// TODO: can shim, cannot set
		[TestMethod]
		public void Can_set_enum_parameter_as_appropriate_shims()
		{
			var obj = new ReturnTypeTest();
			var data = (IEnumerable<string>)new[] { "1", "2", "3" };

			var shim = ShimBuilder.Shim<ICoveredEnumSetMethodTest>(obj);
			var arr = ShimBuilder.Shim<IToString>(data);
			shim.SetEnum(arr);

			var res = obj.GetEnum();
			CollectionAssert.AreEqual(data.ToArray(), res.ToArray());
		}
	}
}
