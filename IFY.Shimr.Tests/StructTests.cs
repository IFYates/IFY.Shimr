using IFY.Shimr.Extensions;

namespace IFY.Shimr.Tests;

[TestClass]
public class StructTests
{
    public interface IToString
    {
        string ToString();
    }

    [TestMethod]
    public void Can_unshim_Int32()
    {
        var num = 12345;
        var shim = num.Shim<IToString>()!;
        var sh2 = (IShim)shim;
        var num2 = (int)sh2.Unshim();
        Assert.AreEqual(num, num2);
    }

    [TestMethod]
    public void Can_shim_Int32()
    {
        var num = 12345;
        var shim = num.Shim<IToString>()!;
        Assert.AreEqual(num.ToString(), shim.ToString());
    }
}
