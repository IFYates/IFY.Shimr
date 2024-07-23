using IFY.Shimr.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests;

/// <summary>
/// Tests around extending/replacing shim functionality
/// https://github.com/IFYates/Shimterface/issues/3
/// </summary>
[TestClass]
public class ExtendedFunctionalityTests_Property
{
    [ExcludeFromCodeCoverage]
    public class TestClass_NoProperty
    {
    }

    [ExcludeFromCodeCoverage]
    public class TestClass_HasProperty
    {
        public string Property { get; set; } = null!;
    }

    public interface ITestShim_AddProperty
    {
        [ShimProxy(typeof(ProxyImpl_AddProperty), ProxyBehaviour.Add)]
        string Property { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_AddProperty
    {
        public static string Property { get; set; } = null!;
    }

    [TestMethod]
    public void Can_add_property_proxy()
    {
        // Arrange
        var obj = new TestClass_NoProperty();
        var shim = obj.Shim<ITestShim_AddProperty>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.AreEqual("test", ProxyImpl_AddProperty.Property);
    }

    public interface ITestShim_AddPropertyDefault
    {
        [ShimProxy(typeof(ProxyImpl_AddPropertyDefault))]
        string Property { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_AddPropertyDefault
    {
        public static string Property { get; set; } = null!;
    }

    [TestMethod]
    public void Can_add_property_proxy_by_default()
    {
        // Arrange
        var obj = new TestClass_NoProperty();
        var shim = obj.Shim<ITestShim_AddPropertyDefault>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.AreEqual("test", ProxyImpl_AddPropertyDefault.Property);
    }

#if !SHIMR_CG
    [TestMethod]
    public void Cannot_add_existing_property()
    {
        // Arrange
        var obj = new TestClass_HasProperty();

        // Act
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            obj.Shim<ITestShim_AddProperty>();
        });
    }
#endif

    public interface ITestShim_OverrideProperty
    {
        [ShimProxy(typeof(ProxyImpl_OverrideProperty), ProxyBehaviour.Override)]
        string Property { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_OverrideProperty
    {
        public static string Property { get; set; } = null!;
    }

    [TestMethod]
    public void Can_override_property_proxy()
    {
        // Arrange
        var obj = new TestClass_HasProperty();
        var shim = obj.Shim<ITestShim_OverrideProperty>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.IsNull(obj.Property);
        Assert.AreEqual("test", ProxyImpl_OverrideProperty.Property);
    }

    public interface ITestShim_OverridePropertyDefault
    {
        [ShimProxy(typeof(ProxyImpl_OverridePropertyDefault))]
        string Property { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_OverridePropertyDefault
    {
        public static string Property { get; set; } = null!;
    }

    [TestMethod]
    public void Can_override_property_proxy_by_default()
    {
        // Arrange
        var obj = new TestClass_HasProperty();
        var shim = obj.Shim<ITestShim_OverridePropertyDefault>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.IsNull(obj.Property);
        Assert.AreEqual("test", ProxyImpl_OverridePropertyDefault.Property);
    }

#if !SHIMR_CG
    [TestMethod]
    public void Cannot_override_missing_property()
    {
        // Arrange
        var obj = new TestClass_NoProperty();

        // Act
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            obj.Shim<ITestShim_OverrideProperty>();
        });
    }
#endif

    public interface ITestShim_OverridePropertyAlias
    {
        [Shim("Property")]
        [ShimProxy(typeof(ProxyImpl_OverridePropertyAlias), "PropertyProxy", ProxyBehaviour.Override)]
        string PropertyShim { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ProxyImpl_OverridePropertyAlias
    {
        public static string PropertyProxy { get; set; } = null!;
    }

    [TestMethod]
    public void Can_override_property_proxy_with_aliases()
    {
        // Arrange
        var obj = new TestClass_HasProperty();
        var shim = obj.Shim<ITestShim_OverridePropertyAlias>();

        // Act
        shim.PropertyShim = "test";

        // Assert
        Assert.IsNull(obj.Property);
        Assert.AreEqual("test", ProxyImpl_OverridePropertyAlias.PropertyProxy);
    }

    // TODO: CG can support this
#if !SHIMR_CG
    public interface ITestShim_PropertyMethods
    {
        [ShimProxy(typeof(ProxyImpl_PropertyMethods))]
        string Property { get; set; }
    }
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Style", "IDE1006:Naming Styles")]
    public class ProxyImpl_PropertyMethods
    {
        private static bool _inProxy;
        public static string PropertyValue { get; private set; } = null!;

        public static string get_Property(ITestShim_PropertyMethods inst)
        {
            if (_inProxy) { return PropertyValue; }
            try
            {
                _inProxy = true;
                return inst.Property;
            }
            finally
            {
                _inProxy = false;
            }
        }
        public static void set_Property(ITestShim_PropertyMethods inst, string value)
        {
            if (_inProxy) { PropertyValue = value; return; }
            try
            {
                _inProxy = true;
                inst.Property = value;
            }
            finally
            {
                _inProxy = false;
            }
        }
    }
    [TestMethod]
    public void Can_override_property_using_methods()
    {
        // Arrange
        var obj = new TestClass_HasProperty();
        var shim = obj.Shim<ITestShim_PropertyMethods>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.AreEqual("test", obj.Property);
    }

    [TestMethod]
    public void Can_add_property_using_methods()
    {
        // Arrange
        var obj = new TestClass_NoProperty();
        var shim = obj.Shim<ITestShim_PropertyMethods>();

        // Act
        shim.Property = "test";

        // Assert
        Assert.AreEqual("test", ProxyImpl_PropertyMethods.PropertyValue);
    }
#endif
}
