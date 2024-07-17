namespace IFY.Shimr.Extensions.Tests;

[TestClass]
public class ObjectExtensionsTests
{
    interface IMyShim
    {
    }

    class MyType : IMyShim
    {
    }

    [TestMethod]
    public void Shim_enumerable__Null__Null()
    {
        // Arrange
        int[] arr = null!;

        // Act
        var res = ObjectExtensions.Shim<IMyShim>(arr);

        // Assert
        Assert.IsNull(res);
    }

    [TestMethod]
    public void Unshim_object__Same_type__Return_arg()
    {
        // Arrange
        var arg = new MyType();

        // Act
        var res = ObjectExtensions.Shim<IMyShim>(arg);

        // Assert
        Assert.AreSame(arg, res);
    }
}