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
            [TypeShim(typeof(int))]
            IToString Value { get; set; }
        }
        public interface ICoveredMethodTest
        {
            [TypeShim(typeof(string))]
            IToString GetValue();
        }
        public interface IBadCoveredMethodTest
        {
            [TypeShim(typeof(string))]
            object GetValue();
        }
        public interface ICoveredParametersTest
        {
            void SetValue([TypeShim(typeof(string))] IToString str);
        }

        [TestMethod]
        public void Can_shim_property_with_covered_type()
        {
            var obj = new ReturnTypeTest();

            var shim = (IShim)Shimterface.Shim<ICoveredPropertyTest>(obj);

            Assert.AreSame(obj, shim.Unshim());
        }

        [TestMethod]
        public void Can_shim_method_with_covered_return_type()
        {
            var obj = new ReturnTypeTest();

            var shim = (IShim)Shimterface.Shim<ICoveredMethodTest>(obj);

            Assert.AreSame(obj, shim.Unshim());
        }

        [TestMethod]
        public void Can_shim_method_with_covered_parameter()
        {
            var obj = new ReturnTypeTest();

            var shim = (IShim)Shimterface.Shim<ICoveredParametersTest>(obj);

            Assert.AreSame(obj, shim.Unshim());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Covered_return_type_must_be_interface()
        {
            var obj = new ReturnTypeTest();

            Shimterface.Shim<IBadCoveredMethodTest>(obj);
        }

        [TestMethod]
        public void Can_get_result_of_covered_method()
        {
            var obj = new ReturnTypeTest();

            var shim = Shimterface.Shim<ICoveredMethodTest>(obj);
            var res = shim.GetValue();

            Assert.AreEqual("Test", ((IShim)res).Unshim());
        }

        [TestMethod]
        public void Can_call_method_with_covered_parameter_and_appropriate_underyling_type()
        {
            var obj = new ReturnTypeTest();

            var shim = Shimterface.Shim<ICoveredParametersTest>(obj);
            var res = Shimterface.Shim<IToString>("abc123");

            shim.SetValue(res);
            Assert.AreEqual("abc123", obj.GetValue());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Cannot_call_method_with_covered_parameter_and_inappropriate_underyling_type()
        {
            var obj = new ReturnTypeTest();

            var shim = Shimterface.Shim<ICoveredParametersTest>(obj);
            var res = Shimterface.Shim<IToString>(45876);

            shim.SetValue(res);
        }

        [TestMethod]
        public void Can_get_result_of_covered_property()
        {
            var obj = new ReturnTypeTest
            {
                Value = 12345
            };

            var shim = Shimterface.Shim<ICoveredPropertyTest>(obj);
            var res = shim.Value;

            Assert.AreEqual("12345", res.ToString());
            Assert.AreEqual(12345, ((IShim)res).Unshim());
        }

        [TestMethod]
        public void Can_set_covered_property_with_appropriate_underlying_value()
        {
            var obj = new ReturnTypeTest();

            var shim = Shimterface.Shim<ICoveredPropertyTest>(obj);
            var shim2 = Shimterface.Shim<IToString>(12345);
            shim.Value = shim2;

            Assert.AreEqual(12345, obj.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Cannot_set_covered_property_with_inappropriate_underlying_value()
        {
            var obj = new ReturnTypeTest();

            var shim = Shimterface.Shim<ICoveredPropertyTest>(obj);
            var shim2 = Shimterface.Shim<IToString>("test");
            shim.Value = shim2;
        }
    }
}
