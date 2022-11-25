using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests;

[TestClass]
public class MethodShimTests
{
#if SHIMRGEN
    class ShimBuilder
    {
        public static T Shim<T>(TestClass obj) => obj.Shim<T>();
    }
#endif

#if SHIMRGEN
    [ShimOf(typeof(TestClass))]
#endif
    public interface IVoidMethodTest
    {
        void VoidMethod();
    }
#if SHIMRGEN
    [ShimOf(typeof(TestClass))]
#endif
    public interface IVoidMethodArgsTest
    {
        void VoidMethodArgs(string arg1, int arg2);
    }
#if SHIMRGEN
    [ShimOf(typeof(TestClass))]
#endif
    public interface IStringMethodTest
    {
        string StringMethod();
    }
#if SHIMRGEN
    [ShimOf(typeof(TestClass))]
#endif
    public interface IStringMethodArgsTest
    {
        string StringMethodArgs(string arg1, int arg2);
    }
#if SHIMRGEN
    [ShimOf(typeof(TestClass))]
#endif
    public interface IAddedMethod
    {
        bool AddedMethod(string arg1)
        {
            return true;
        }
    }

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
            VoidMethodArgsCalled = new object[] { arg1, arg2 };
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

        var shim = ShimBuilder.Shim<IVoidMethodTest>(obj);
        shim!.VoidMethod();

        Assert.IsTrue(obj.VoidMethodCalled);
    }

    [TestMethod]
    public void VoidMethod_with_args_callable()
    {
        var obj = new TestClass();
        Assert.IsNull(obj.VoidMethodArgsCalled);

        var shim = ShimBuilder.Shim<IVoidMethodArgsTest>(obj);
        shim!.VoidMethodArgs("arg1", 2);

        CollectionAssert.AreEquivalent(new object[] { "arg1", 2 }, obj.VoidMethodArgsCalled);
    }

    [TestMethod]
    public void StringMethod_callable()
    {
        var obj = new TestClass();

        var shim = ShimBuilder.Shim<IStringMethodTest>(obj);
        var res = shim!.StringMethod();

        Assert.AreEqual("result", res);
    }

    [TestMethod]
    public void StringMethod_with_args_callable()
    {
        var obj = new TestClass();

        var shim = ShimBuilder.Shim<IStringMethodArgsTest>(obj);
        var res = shim!.StringMethodArgs("arg1", 2);

        Assert.AreEqual("arg1-2", res);
    }

#if !SHIMRGEN // Not possible with ShimrGen
    public interface IDifferentMethodSig
    {
        void DifferentMethodSig(string arg1);
    }

    [TestMethod]
    public void Method_signatures_must_match()
    {
        var obj = new TestClass();

        Assert.ThrowsException<MissingMemberException>(() =>
        {
            ShimBuilder.Shim<IDifferentMethodSig>(obj);
        });
    }
#endif

    [TestMethod]
    public void Multiple_calls_to_same_shim_returns_same_type()
    {
        var obj1 = new TestClass();
        var shim1 = ShimBuilder.Shim<IStringMethodTest>(obj1);
        var obj2 = new TestClass();
        var shim2 = ShimBuilder.Shim<IStringMethodTest>(obj2);

        Assert.AreSame(shim1!.GetType(), shim2!.GetType());
    }

    [TestMethod]
    public void Method_defined_in_facade_is_used()
    {
        var obj = new TestClass();
        var shim = ShimBuilder.Shim<IAddedMethod>(obj);

        Assert.IsTrue(shim!.AddedMethod("TEST"));
    }
}
