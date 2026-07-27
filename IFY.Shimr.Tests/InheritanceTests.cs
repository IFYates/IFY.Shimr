using IFY.Shimr.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable CA1822 // Mark members as static
namespace IFY.Shimr.Tests;

[TestClass]
public class InheritanceTests
{
    public interface IRootShim : IChildShim
    {
        int TestA();
    }
    public interface IChildShim : IGrandchildShim
    {
        int TestB();
    }
    public interface IGrandchildShim
    {
        int TestC();

        // Implemented interface member
        public int TestD() => -TestC();
    }

    public class TestClass
    {
        public int TestA()
        {
            return 1;
        }
        public int TestB()
        {
            return 2;
        }
        public int TestC()
        {
            return 3;
        }
    }

    [TestMethod]
    public void Can_shim_full_hierarchy()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IRootShim>();

        Assert.AreEqual(1, shim.TestA());
        Assert.AreEqual(2, shim.TestB());
        Assert.AreEqual(3, shim.TestC());
    }

    [TestMethod]
    public void Implemented_interface_member_supported()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IGrandchildShim>();

        Assert.AreEqual(3, shim.TestC());
        Assert.AreEqual(-3, shim.TestD());
    }

    #region Issue 11 - Unable to shim hidden method
    // https://github.com/IFYates/Shimterface/issues/11

    public abstract class Issue11BaseClass
    {
        public string GetValue() { return "value"; }
    }

    public class Issue11Class : Issue11BaseClass
    {
        new public int GetValue() { return 1; }
    }

    public interface IShimIssue11
    {
        string GetValue();
    }

    [TestMethod]
    public void Issue11__Can_shim_hidden_method()
    {
        var obj = new Issue11Class();
        Assert.IsInstanceOfType<int>(obj.GetValue()); // New method takes precedence

        var shim = obj.Shim<IShimIssue11>();

        var result = shim.GetValue();
        Assert.AreEqual("value", result);
    }

    #endregion Issue 11

    public interface IBase
    {
        string Method();
    }
    public class Method2 : IBase
    {
        public int Method() => 1;
        string IBase.Method() => "value";
    }
    public class Method3 : Method2
    {
        new public byte Method() => 2;
    }
    public interface IMethodShim
    {
        [Shim(typeof(IBase))] string Method();
    }

    [TestMethod]
    public void Can_expose_explicit_interface_method()
    {
        var obj = new Method3();
        Assert.AreEqual((byte)2, obj.Method());
        Assert.AreEqual(1, ((Method2)obj).Method());

        var shim = obj.Shim<IMethodShim>();
        Assert.AreEqual("value", shim.Method());
    }
}
