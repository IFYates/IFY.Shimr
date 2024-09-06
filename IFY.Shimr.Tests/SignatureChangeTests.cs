using IFY.Shimr.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests;

[TestClass]
public class SignatureChangeTests
{
    [ExcludeFromCodeCoverage]
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
    public interface IBadCoveredParametersTest
    {
        void SetValue([TypeShim(typeof(string))] string str);
    }

#if !SHIMR_SG
    [TestInitialize]
    public void ResetState()
    {
        ShimBuilder.ResetState();
    }
#endif

    [TestMethod]
    public void Can_shim_property_with_covered_type()
    {
        var obj = new ReturnTypeTest();

        var shim = (IShim)obj.Shim<ICoveredPropertyTest>()!;

        Assert.AreSame(obj, shim.Unshim());
    }

    [TestMethod]
    public void Can_shim_method_with_covered_return_type()
    {
        var obj = new ReturnTypeTest();

        var shim = (IShim)obj.Shim<ICoveredMethodTest>()!;

        Assert.AreSame(obj, shim.Unshim());
    }

    [TestMethod]
    public void Can_shim_method_with_covered_parameter()
    {
        var obj = new ReturnTypeTest();

        var shim = (IShim)obj.Shim<ICoveredParametersTest>()!;

        Assert.AreSame(obj, shim.Unshim());
    }

#if !SHIMR_SG
    [TestMethod]
    public void Covered_return_type_must_be_interface()
    {
        // Arrange
        var obj = new ReturnTypeTest();

        // Act
        var ex = Assert.ThrowsException<System.NotSupportedException>(() =>
        {
            ShimBuilder.Shim<IBadCoveredMethodTest>(obj);
        });

        // Assert
        Assert.AreEqual("Shimmed return type (System.Object) must be an interface, on member: IFY.Shimr.Tests.SignatureChangeTests+IBadCoveredMethodTest.GetValue", ex.Message);
    }
#endif

    [TestMethod]
    public void Can_get_result_of_covered_method()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredMethodTest>()!;
        var res = shim.GetValue();

        Assert.AreEqual("Test", ((IShim)res).Unshim());
    }

    [TestMethod]
    public void Can_call_method_with_covered_parameter_and_appropriate_underyling_type()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredParametersTest>()!;
        var res = "abc123".Shim<IToString>()!;

        shim.SetValue(res);
        Assert.AreEqual("abc123", obj.GetValue());
    }

#if !SHIMR_SG
    [TestMethod]
    public void Covered_parameter_type_must_be_interface()
    {
        // Arrange
        var obj = new ReturnTypeTest();

        // Act
        var ex = Assert.ThrowsException<System.NotSupportedException>(() =>
        {
            ShimBuilder.Shim<IBadCoveredParametersTest>(obj);
        });

        // Assert
        Assert.AreEqual("Shimmed parameter type must be an interface: IFY.Shimr.Tests.SignatureChangeTests+IBadCoveredParametersTest", ex.Message);
    }
#endif

    public interface IShimOverloadMethod
    {
        void SetValue([TypeShim(typeof(string))] IToString str);
        void SetValue(string str);
    }
    [TestMethod]
    public void Can_call_method_with_covered_parameter_overload_and_original()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<IShimOverloadMethod>()!;
        var res = "abc123".Shim<IToString>()!;

        shim.SetValue(res);
        Assert.AreEqual("abc123", obj.GetValue());

        shim.SetValue("def456");
        Assert.AreEqual("def456", obj.GetValue());
    }

    [TestMethod]
    public void Cannot_call_method_with_covered_parameter_and_inappropriate_underyling_type()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredParametersTest>()!;
        var res = 45876.Shim<IToString>()!;

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            shim.SetValue(res);
        });
    }

    [TestMethod]
    public void Can_get_result_of_covered_property()
    {
        var obj = new ReturnTypeTest
        {
            Value = 12345
        };

        var shim = obj.Shim<ICoveredPropertyTest>()!;
        var res = shim.Value;

        Assert.AreEqual("12345", res.ToString());
        Assert.AreEqual(12345, ((IShim)res).Unshim());
    }

    [TestMethod]
    public void Can_set_covered_property_with_appropriate_underlying_value()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredPropertyTest>()!;
        var shim2 = 12345.Shim<IToString>()!;
        shim.Value = shim2;

        Assert.AreEqual(12345, obj.Value);
    }

    [TestMethod]
    public void Cannot_set_covered_property_with_inappropriate_underlying_value()
    {
        var obj = new ReturnTypeTest();

        var shim = obj.Shim<ICoveredPropertyTest>()!;
        var shim2 = "test".Shim<IToString>()!;

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            shim.Value = shim2;
        });
    }
}
