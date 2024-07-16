using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests;

[TestClass]
public class ConstructorTests
{
    public interface IInstanceInterface
    {
        string Result { get; }
    }
    public interface IFactoryInterface
    {
        [ConstructorShim(typeof(TestClass))]
        IInstanceInterface Create(string arg);
        [ConstructorShim(typeof(TestClass))]
        IInstanceInterface New(int arg);
        [ConstructorShim(typeof(TestClass))]
        IInstanceInterface New(bool arg1, int arg2);
    }

    [ExcludeFromCodeCoverage]
    public class TestClass
    {
        public string Result { get; private set; }

        public TestClass(string arg)
        {
            Result = $"A_{arg}";
        }
        public TestClass(int arg)
        {
            Result = $"B_{arg}";
        }
        public TestClass(bool arg)
        {
            Result = $"C_{arg}";
        }
        public TestClass(bool arg1, int arg2)
        {
            Result = $"D_{arg1}_{arg2}";
        }
    }

    [TestMethod]
    public void Can_shim_to_constructor()
    {
        var shim = ShimBuilder.Create<IFactoryInterface>();
        var instA = shim.Create("one");
        var instB = shim.New(2);
        var instC = shim.New(true, 3);

        Assert.AreEqual("A_one", instA.Result);
        Assert.AreEqual("B_2", instB.Result);
        Assert.AreEqual("D_True_3", instC.Result);
        Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass));
        Assert.IsInstanceOfType(((IShim)instB).Unshim(), typeof(TestClass));
        Assert.IsInstanceOfType(((IShim)instC).Unshim(), typeof(TestClass));
    }

#if !SHIMR_CG
    public interface IFactoryInterface2
    {
        [ConstructorShim(typeof(TestClass))]
        void New(int arg);
    }
    [TestMethod]
    public void Can_not_shim_to_constructor_with_void_return_type()
    {
        var ex = Assert.ThrowsException<System.ArgumentException>(() =>
        {
            ShimBuilder.Create<IFactoryInterface2>();
        });

        Assert.AreEqual("The type 'System.Void' may not be used as a type argument.", ex.Message);
    }
#endif

#if !SHIMR_CG
    public interface IFactoryInterface3
    {
        [ConstructorShim(typeof(TestClass))]
        int New(int arg);
    }
    [TestMethod]
    public void Can_not_shim_to_constructor_with_incorrect_return_type()
    {
        var ex = Assert.ThrowsException<System.ArgumentException>(() =>
         {
             ShimBuilder.Create<IFactoryInterface3>();
         });

        Assert.IsTrue(ex.Message.Contains(" violates the constraint of type 'TInterface'."), ex.Message);
    }
#endif

    [StaticShim(typeof(TestClass))]
    public interface IFactoryInterface4
    {
        [ConstructorShim]
        IInstanceInterface New(int arg);
    }
    [TestMethod]
    public void Constructor_shim_will_inherit_target_type_from_interface()
    {
        var shim = ShimBuilder.Create<IFactoryInterface4>();
        var inst = shim.New(4);

        Assert.AreEqual("B_4", inst.Result);
        Assert.IsInstanceOfType(((IShim)inst).Unshim(), typeof(TestClass));
    }

#if !SHIMR_CG
    public interface IFactoryInterface5
    {
        [ConstructorShim(typeof(TestClass))]
        int New(decimal arg);
    }
    [TestMethod]
    public void Can_not_shim_to_missing_constructor()
    {
        var ex = Assert.ThrowsException<System.MissingMemberException>(() =>
        {
            ShimBuilder.Create<IFactoryInterface5>();
        });

        Assert.IsTrue(ex.Message.Contains(" missing method:"), ex.Message);
    }
#endif
}
