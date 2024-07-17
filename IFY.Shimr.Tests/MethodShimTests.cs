using IFY.Shimr.Extensions;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE1006 // Naming Styles
namespace IFY.Shimr.Tests;

[TestClass]
public class MethodShimTests
{
    public interface IVoidMethodTest
    {
        void VoidMethod();
    }
    public interface IVoidMethodArgsTest
    {
        void VoidMethodArgs(string arg1, int arg2);
    }
    public interface IStringMethodTest
    {
        string StringMethod();
    }
    public interface IStringMethodArgsTest
    {
        string StringMethodArgs(string arg1, int arg2);
    }
    public interface IDifferentMethodSig
    {
        void DifferentMethodSig(string arg1);
    }

#if NET
    public interface IAddedMethod
    {
        bool AddedMethod(string arg1)
        {
            return true;
        }
    }
#endif

    [ExcludeFromCodeCoverage]
    public abstract class TestParentClass
    {
        internal bool VoidMethodCalled;
        public void VoidMethod()
        {
            VoidMethodCalled = true;
        }

        public abstract string StringMethod();

        public virtual string StringMethodArgs(string arg1, int arg2)
        {
            return "bad_result";
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestClass : TestParentClass
    {
        internal object[] VoidMethodArgsCalled = null!;
        public void VoidMethodArgs(string arg1, int arg2)
        {
            VoidMethodArgsCalled = [arg1, arg2];
        }

        public override string StringMethod()
        {
            return "result";
        }

        public override string StringMethodArgs(string arg1, int arg2)
        {
            return arg1 + "-" + arg2;
        }

        public void DifferentMethodSig(int arg1)
        {
            _ = arg1;
        }
    }

    [TestMethod]
    public void VoidMethod_callable()
    {
        var obj = new TestClass();
        Assert.IsFalse(obj.VoidMethodCalled);

        var shim = obj.Shim<IVoidMethodTest>();
        shim!.VoidMethod();

        Assert.IsTrue(obj.VoidMethodCalled);
    }

    [TestMethod]
    public void VoidMethod_with_args_callable()
    {
        var obj = new TestClass();
        Assert.IsNull(obj.VoidMethodArgsCalled);

        var shim = obj.Shim<IVoidMethodArgsTest>();
        shim.VoidMethodArgs("arg1", 2);

        CollectionAssert.AreEquivalent(new object[] { "arg1", 2 }, obj.VoidMethodArgsCalled);
    }

    [TestMethod]
    public void StringMethod_callable()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IStringMethodTest>();
        var res = shim.StringMethod();

        Assert.AreEqual("result", res);
    }

    [TestMethod]
    public void StringMethod_with_args_callable()
    {
        var obj = new TestClass();

        var shim = obj.Shim<IStringMethodArgsTest>();
        var res = shim.StringMethodArgs("arg1", 2);

        Assert.AreEqual("arg1-2", res);
    }

#if !SHIMR_CG
    [TestMethod]
    public void Method_signatures_must_match()
    {
        var obj = new TestClass();

        Assert.ThrowsException<System.MissingMemberException>(() =>
        {
            ShimBuilder.Shim<IDifferentMethodSig>(obj);
        });
    }
#endif

    [TestMethod]
    public void Multiple_calls_to_same_shim_returns_same_type()
    {
        var obj1 = new TestClass();
        var shim1 = obj1.Shim<IStringMethodTest>();
        var obj2 = new TestClass();
        var shim2 = obj2.Shim<IStringMethodTest>();

        Assert.AreSame(shim1.GetType(), shim2.GetType());
    }

#if NET
    [TestMethod]
    public void Method_defined_in_facade_is_used()
    {
        var obj = new TestClass();
        var shim = obj.Shim<IAddedMethod>();

        Assert.IsTrue(shim.AddedMethod("TEST"));
    }
#endif
}
