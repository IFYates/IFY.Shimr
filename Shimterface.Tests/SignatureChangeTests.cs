using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Shimterface.Tests
{
	[TestClass]
	public class SignatureChangeTests
	{
		public class ReturnTypeTest
		{
			public int Value { get; set; }

			private string _value = "Test";
			public void SetValue(string str)
			{
				_value = str;
			}
			public string GetValue()
			{
				return _value;
			}
		}
		public interface IToString
		{
			string ToString();
		}
		public interface ICoveredPropertyTest
		{
			IToString Value { get; set; }
		}
		public interface ICoveredMethodTest
		{
			IToString GetValue();
		}
		public interface IBadCoveredMethodTest
		{
			object GetValue();
		}
		public interface ICoveredParametersTest
		{
			void SetValue([TypeShim(typeof(string))] IToString str);
		}
		
		[TestInitialize]
		public void ResetState()
		{
			ShimBuilder.ResetState();
		}

		[TestMethod]
		public void Can_shim_property_with_covered_type()
		{
			var obj = new ReturnTypeTest();

			var shim = (IShim)ShimBuilder.Shim<ICoveredPropertyTest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_shim_method_with_covered_return_type()
		{
			var obj = new ReturnTypeTest();

			var shim = (IShim)ShimBuilder.Shim<ICoveredMethodTest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		public void Can_shim_method_with_covered_parameter()
		{
			var obj = new ReturnTypeTest();

			var shim = (IShim)ShimBuilder.Shim<ICoveredParametersTest>(obj);

			Assert.AreSame(obj, shim.Unshim());
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public void Covered_return_type_must_be_interface()
		{
			var obj = new ReturnTypeTest();

			ShimBuilder.Shim<IBadCoveredMethodTest>(obj);
		}

		[TestMethod]
		public void Can_get_result_of_covered_method()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredMethodTest>(obj);
			var res = shim.GetValue();

			Assert.AreEqual("Test", ((IShim)res).Unshim());
		}

		[TestMethod]
		public void Can_call_method_with_covered_parameter_and_appropriate_underyling_type()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredParametersTest>(obj);
			var res = ShimBuilder.Shim<IToString>("abc123");

			shim.SetValue(res);
			Assert.AreEqual("abc123", obj.GetValue());
		}
		
		public interface IShimOverloadMethod
		{
			void SetValue([TypeShim(typeof(string))] IToString str);
			void SetValue(string str);
		}
		[TestMethod]
		public void Can_call_method_with_covered_parameter_overload_and_original()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<IShimOverloadMethod>(obj);
			var res = ShimBuilder.Shim<IToString>("abc123");

			shim.SetValue(res);
			Assert.AreEqual("abc123", obj.GetValue());

			shim.SetValue("def456");
			Assert.AreEqual("def456", obj.GetValue());
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void Cannot_call_method_with_covered_parameter_and_inappropriate_underyling_type()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredParametersTest>(obj);
			var res = ShimBuilder.Shim<IToString>(45876);

			shim.SetValue(res);
		}

		[TestMethod]
		public void Can_get_result_of_covered_property()
		{
			var obj = new ReturnTypeTest
			{
				Value = 12345
			};

			var shim = ShimBuilder.Shim<ICoveredPropertyTest>(obj);
			var res = shim.Value;

			Assert.AreEqual("12345", res.ToString());
			Assert.AreEqual(12345, ((IShim)res).Unshim());
		}

		[TestMethod]
		public void Can_set_covered_property_with_appropriate_underlying_value()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredPropertyTest>(obj);
			var shim2 = ShimBuilder.Shim<IToString>(12345);
			shim.Value = shim2;

			Assert.AreEqual(12345, obj.Value);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidCastException))]
		public void Cannot_set_covered_property_with_inappropriate_underlying_value()
		{
			var obj = new ReturnTypeTest();

			var shim = ShimBuilder.Shim<ICoveredPropertyTest>(obj);
			var shim2 = ShimBuilder.Shim<IToString>("test");
			shim.Value = shim2;
		}
	}
}
