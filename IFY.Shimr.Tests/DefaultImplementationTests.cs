using IFY.Shimr.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable enable
namespace IFY.Shimr.Tests;

[TestClass]
public class DefaultImplementationTests
{
    [StaticShim(typeof(string))]
    public interface IMyShimBuilder
    {
        [ConstructorShim]
        string NewString(char[]? chars);

        public string? NewLowerString(char[]? chars) => NewString(chars)?.ToLower();
    }

    [TestMethod]
    public void Can_create_factory_with_default_implementation()
    {
        // Act
        var obj1 = ShimBuilder.Create<IMyShimBuilder>().NewString(['T', 'e', 's', 't', 'V', 'a', 'l', 'u', 'e']);
        var obj2 = ShimBuilder.Create<IMyShimBuilder>().NewLowerString(['T', 'e', 's', 't', 'V', 'a', 'l', 'u', 'e']);

        // Assert
        Assert.AreEqual("TestValue", obj1);
        Assert.AreEqual("testvalue", obj2);
    }

    public interface IMyShim
    {
        string? ToString();

        public string? ToLowerString() => ToString()?.ToLower();
    }

    [TestMethod]
    public void Can_create_shim_with_default_implementation()
    {
        // Act
        var obj = "TestValue".Shim<IMyShim>();

        // Assert
        Assert.AreEqual("TestValue", obj.ToString());
        Assert.AreEqual("testvalue", obj.ToLowerString());
    }
}
