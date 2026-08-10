using IFY.Shimr.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Shimr.Tests;

[TestClass]
public sealed class MultishimTests
{
    class TestClass
    {
        public void Test()
        {
        }
        public void Test2()
        {
        }
    }

    public interface ITestShim
    {
        void Test();
    }
    public interface ITestShim2
    {
        void Test2();
    }

    [TestMethod]
    public void Shim__Multiple_interfaces__Can_resolve_to_any()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var shim = (object)obj.Shim<ITestShim, ITestShim2>();

        // Assert
        Assert.IsTrue(shim is ITestShim);
        Assert.IsTrue(shim is ITestShim2);
    }

    public interface ITestShim3
    {
        void Test();
    }

    [TestMethod]
    public void Shim__Multiple_interfaces__Member_collisions_ignored()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var shim = (object)obj.Shim<ITestShim, ITestShim2, ITestShim3>();

        // Assert
        Assert.IsTrue(shim is ITestShim);
        Assert.IsTrue(shim is ITestShim2);
        Assert.IsTrue(shim is ITestShim3);
    }
}
