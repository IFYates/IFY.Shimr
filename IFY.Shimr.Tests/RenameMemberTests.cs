using IFY.Shimr.Extensions;
using System;

namespace IFY.Shimr.Tests;

#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable IDE1006 // Naming Styles
[TestClass]
public class RenameMemberTests
{
    public class TestClass
    {
        public static string StaticField = null!;
        public static string StaticValue { get; set; } = null!;
        public static string StaticTest() { return StaticValue; }

        public string InstanceField = null!;
        public string InstanceValue { get; set; } = null!;
        public string InstanceTest() { return InstanceValue; }
    }

    public interface IStaticMembers
    {
        [StaticShim(typeof(TestClass))]
        [Shim(nameof(TestClass.StaticField))]
        string Field { get; set; }
        [StaticShim(typeof(TestClass))]
        [Shim(nameof(TestClass.StaticValue))]
        string Value { get; set; }
        [StaticShim(typeof(TestClass))]
        [Shim(nameof(TestClass.StaticTest))]
        string Test();
    }

    public interface IInstanceMembers
    {
        [Shim(nameof(TestClass.InstanceField))]
        string Field { get; set; }
        [Shim(nameof(TestClass.InstanceValue))]
        string Value { get; set; }
        [Shim(nameof(TestClass.InstanceTest))]
        string Test();
    }

    [TestMethod]
    public void Can_get_from_renamed_field()
    {
        var obj = new TestClass
        {
            InstanceField = DateTime.UtcNow.ToString("o")
        };

        var shim = obj.Shim<IInstanceMembers>();

        Assert.AreSame(obj.InstanceField, shim.Field);
    }

    [TestMethod]
    public void Can_get_from_renamed_static_field()
    {
        TestClass.StaticField = DateTime.UtcNow.ToString("o");

        var factory = ShimBuilder.Create<IStaticMembers>();

        Assert.AreSame(TestClass.StaticField, factory.Field);
    }

    [TestMethod]
    public void Can_set_renamed_field()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IInstanceMembers>();
        shim.Field = DateTime.UtcNow.ToString("o");

        Assert.AreSame(shim.Field, obj.InstanceField);
    }

    [TestMethod]
    public void Can_set_renamed_static_field()
    {
        TestClass.StaticField = null!;

        var factory = ShimBuilder.Create<IStaticMembers>();
        factory.Field = DateTime.UtcNow.ToString("o");

        Assert.AreSame(factory.Field, TestClass.StaticField);
    }

    [TestMethod]
    public void Can_get_from_renamed_property()
    {
        var obj = new TestClass
        {
            InstanceValue = DateTime.UtcNow.ToString("o")
        };

        var shim = obj.Shim<IInstanceMembers>();

        Assert.AreSame(obj.InstanceValue, shim.Value);
    }

    [TestMethod]
    public void Can_get_from_renamed_static_property()
    {
        TestClass.StaticValue = DateTime.UtcNow.ToString("o");

        var factory = ShimBuilder.Create<IStaticMembers>();

        Assert.AreSame(TestClass.StaticValue, factory.Value);
    }

    [TestMethod]
    public void Can_set_renamed_property()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IInstanceMembers>();
        shim.Value = DateTime.UtcNow.ToString("o");

        Assert.AreSame(shim.Value, obj.InstanceValue);
    }

    [TestMethod]
    public void Can_set_renamed_static_property()
    {
        TestClass.StaticValue = null!;

        var factory = ShimBuilder.Create<IStaticMembers>();
        factory.Value = DateTime.UtcNow.ToString("o");

        Assert.AreSame(factory.Value, TestClass.StaticValue);
    }

    [TestMethod]
    public void Can_call_renamed_method()
    {
        var obj = new TestClass
        {
            InstanceValue = DateTime.UtcNow.ToString("o")
        };

        var shim = obj.Shim<IInstanceMembers>();

        Assert.AreSame(obj.InstanceTest(), shim.Test());
    }

    [TestMethod]
    public void Can_call_renamed_static_method()
    {
        TestClass.StaticValue = DateTime.UtcNow.ToString("o");

        var factory = ShimBuilder.Create<IStaticMembers>();

        Assert.AreSame(TestClass.StaticTest(), factory.Test());
    }
}
