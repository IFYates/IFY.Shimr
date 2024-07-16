#if !SHIMR_CG
using IFY.Shimr.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE1006 // Naming Styles
namespace IFY.Shimr.Tests;

[TestClass]
public class AccessibilityTests
{
    interface IPrivateInterface
    {
        void Test();
    }
    public interface IPublicInterface : IShim
    {
        void Test();
    }

    [ExcludeFromCodeCoverage]
    class PrivateTestClass
    {
        public void Test()
        {
            // Test
        }
    }
    [ExcludeFromCodeCoverage]
    public class PublicTestClass
    {
        private void Test()
        {
            // Test
        }
    }

    [TestMethod]
    public void Can_always_shim_null()
    {
        var res = ((PrivateTestClass)null!).Shim<IPrivateInterface>();

        Assert.IsNull(res);
    }

    [TestMethod]
    public void Cannot_shim_to_private_interface()
    {
        var obj = new PrivateTestClass();

        var ex = Assert.ThrowsException<TypeLoadException>(() =>
        {
            obj.Shim<IPrivateInterface>();
        });

        Assert.IsTrue(ex.Message.Contains(" attempting to implement an inaccessible interface."), ex.Message);
    }

    [TestMethod]
    public void Cannot_use_shim_of_private_class()
    {
        var obj = new PrivateTestClass();

        var shim = obj.Shim<IPublicInterface>();

        var ex = Assert.ThrowsException<MethodAccessException>(() =>
        {
            shim.Test();
        });

        Assert.IsTrue(ex.Message.Contains(" access method 'IFY.Shimr.Tests.AccessibilityTests+PrivateTestClass.Test()' failed."), ex.Message);
    }

    [TestMethod]
    public void Cannot_shim_class_with_private_interface_member()
    {
        var obj = new PublicTestClass();

        var ex = Assert.ThrowsException<MissingMemberException>(() =>
        {
            obj.Shim<IPublicInterface>();
        });

        Assert.IsTrue(ex.Message.Contains(" missing method: Void Test()"), ex.Message);
    }

    [TestMethod]
    public void Result_is_IShim()
    {
        var obj = new PrivateTestClass();

        var shim = obj.Shim<IPublicInterface>();

        Assert.IsTrue(shim is IShim);
    }

    [TestMethod]
    public void Can_unshim_original_object()
    {
        var obj = new PrivateTestClass();

        var shim = obj.Shim<IPublicInterface>();

        Assert.AreSame(obj, shim.Unshim());
    }
}
#endif