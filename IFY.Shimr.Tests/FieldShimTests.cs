using IFY.Shimr.Extensions;

namespace IFY.Shimr.Tests;

[TestClass]
public class FieldShimTests
{
    public interface IGetFieldTest
    {
        long LValue { get; }
        string RValue { get; }
    }
    public interface ISetFieldTest
    {
        long LValue { set; }
        string RValue { set; }
    }
    public interface IGetSetFieldTest
    {
        long LValue { get; set; }
        string RValue { get; set; }
    }
    public interface IReadonlyFieldTest
    {
        string Immutable { get; set; }
    }

    public interface IOverrideFieldTest
    {
        IGetSetFieldTest Child { get; set; }
    }

    public class TestClass
    {
        public long LValue = 12345L;
        public string RValue = "value";
        public readonly string Immutable = "readonly";

        public TestClass Child = null!;
    }

    [TestMethod]
    public void Can_shim_a_value_field_as_a_get_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IGetFieldTest>();

        Assert.AreEqual(12345L, shim.LValue);
    }

    [TestMethod]
    public void Can_shim_a_value_field_as_a_set_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<ISetFieldTest>();
        shim.LValue = 98765L;

        Assert.AreEqual(98765L, obj.LValue);
    }

    [TestMethod]
    public void Can_shim_a_value_field_as_a_get_set_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IGetSetFieldTest>();
        shim.LValue = 98765L;

        Assert.AreEqual(shim.LValue, obj.LValue);
    }

    [TestMethod]
    public void Can_shim_a_ref_field_as_a_get_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IGetFieldTest>();

        Assert.AreEqual("value", shim.RValue);
    }

    [TestMethod]
    public void Can_shim_a_ref_field_as_a_set_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<ISetFieldTest>();
        shim.RValue = "new_value";

        Assert.AreEqual("new_value", obj.RValue);
    }

    [TestMethod]
    public void Can_shim_a_ref_field_as_a_get_set_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IGetSetFieldTest>();
        shim.RValue = "new_value";

        Assert.AreEqual(shim.RValue, obj.RValue);
    }

#if !SHIMR_CG
    [TestMethod]
    public void Can_shim_a_readonly_field_as_a_getset_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IReadonlyFieldTest>();

        Assert.AreEqual("readonly", shim.Immutable);
    }

    [TestMethod]
    public void Cannot_set_a_readonly_field_shimmed_as_a_set_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IReadonlyFieldTest>();

        Assert.ThrowsException<System.InvalidOperationException>(() =>
        {
            shim.Immutable = "new_value";
        });
    }
#endif

    [TestMethod]
    public void Shim_field_with_changed_return_type()
    {
        var obj = new TestClass
        {
            Child = new TestClass()
        };

        var shim = obj.Shim<IOverrideFieldTest>();

        Assert.AreSame(obj.Child, ((IShim)shim.Child).Unshim());
    }

    [TestMethod]
    public void Shim_field_with_changed_set_type()
    {
        var obj = new TestClass();

        var newChild = new TestClass();
        var newChildShim = newChild.Shim<IGetSetFieldTest>();

        var shim = obj.Shim<IOverrideFieldTest>();
        shim.Child = newChildShim;

        Assert.AreSame(newChild, obj.Child);
    }
}
