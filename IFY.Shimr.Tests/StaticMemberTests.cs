using IFY.Shimr.Extensions;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE1006 // Naming Styles
namespace IFY.Shimr.Tests;

[TestClass]
public class StaticMemberTests
{
    [ExcludeFromCodeCoverage]
    public static class StaticMemberClass
    {
        public static string Value { get; set; } = null!;

        internal static bool _HasCalled;
        public static void Test()
        {
            _HasCalled = true;
        }
    }

    public interface IStaticMethod
    {
        [StaticShim(typeof(StaticMemberClass))]
        void Test();
    }

    public interface IStaticAliasedMethod
    {
        [Shim("Test"), StaticShim(typeof(StaticMemberClass))]
        void AnotherTest();
    }

    public interface IStaticProperty
    {
        [StaticShim(typeof(StaticMemberClass))]
        string Value { get; set; }
    }

    public interface IBadStaticMethod
    {
        void Test();
    }

    [TestInitialize]
    public void ResetState()
    {
#if !SHIMR_CG
        ShimBuilder.ResetState();
#endif
        StaticMemberClass._HasCalled = false;
    }

    [TestMethod]
    public void Can_define_static_method()
    {
        var factory = ShimBuilder.Create<IStaticMethod>();

        Assert.IsFalse(StaticMemberClass._HasCalled);
        factory.Test();
        Assert.IsTrue(StaticMemberClass._HasCalled);
    }

    [TestMethod]
    public void Can_alias_static_method()
    {
        var factory = ShimBuilder.Create<IStaticAliasedMethod>();

        Assert.IsFalse(StaticMemberClass._HasCalled);
        factory.AnotherTest();
        Assert.IsTrue(StaticMemberClass._HasCalled);
    }

    [TestMethod]
    public void Can_define_static_property()
    {
        var factory = ShimBuilder.Create<IStaticProperty>();

        Assert.IsNull(StaticMemberClass.Value);
        Assert.IsNull(factory.Value);
        StaticMemberClass.Value = "one";
        Assert.AreEqual("one", factory.Value);
        factory.Value = "two";
        Assert.AreEqual("two", StaticMemberClass.Value);
    }

#if !SHIMR_CG
    [TestMethod]
    public void Works_from_type_extension()
    {
        var factory = (IStaticMethod)typeof(IStaticMethod).CreateProxy();

        Assert.IsFalse(StaticMemberClass._HasCalled);
        factory.Test();
        Assert.IsTrue(StaticMemberClass._HasCalled);
    }
#endif

#if !SHIMR_CG
    [TestMethod]
    public void Must_only_define_static_methods()
    {
        var ex = Assert.ThrowsException<System.InvalidCastException>(() =>
        {
            ShimBuilder.Create<IBadStaticMethod>();
        });

        Assert.AreEqual("Factory shim cannot implement non-static member: IFY.Shimr.Tests.StaticMemberTests+IBadStaticMethod Test", ex.Message);
    }
#endif

#if !SHIMR_CG
    [TestMethod]
    public void Normal_shims_cannot_use_StaticShimAttribute()
    {
        var ex = Assert.ThrowsException<System.InvalidCastException>(() =>
        {
            ShimBuilder.Shim<IStaticMethod>(new object());
        });

        Assert.AreEqual("Instance shim cannot implement static member: IFY.Shimr.Tests.StaticMemberTests+IStaticMethod Test", ex.Message);
    }
#endif
}
