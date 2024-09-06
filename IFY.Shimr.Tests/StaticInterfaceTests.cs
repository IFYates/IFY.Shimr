using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable IDE1006 // Naming Styles
namespace IFY.Shimr.Tests;

/// <summary>
/// Tests for an interface that is decorated with <see cref="StaticShimAttribute"/>.
/// </summary>
[TestClass]
public class StaticInterfaceTests
{
    [ExcludeFromCodeCoverage]
    public class StaticMemberClass
    {
        public static string Value { get; set; } = null!;

        public static bool _HasCalled;
        public static void Test()
        {
            _HasCalled = true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class StaticMemberClass2
    {
        public static void Test()
        {
            // Test
        }
    }

#if !SHIMR_SG
    [TestInitialize]
    public void ResetState()
    {
        ShimBuilder.ResetState();
    }
#endif

    [StaticShim(typeof(StaticMemberClass))]
    public interface IStaticInterface1
    {
        string Value { get; set; } // Property
        bool _HasCalled { get; set; } // Field
        void Test(); // Method
        [ConstructorShim] StaticMemberClass New(); // Constructor
    }
    [TestMethod]
    public void Can_define_StaticShim_interface_where_all_members_are_on_class()
    {
        var factory = ShimBuilder.Create<IStaticInterface1>();

        Assert.IsFalse(StaticMemberClass._HasCalled);
        factory.Test();
        Assert.IsTrue(StaticMemberClass._HasCalled);
    }

    [StaticShim(typeof(StaticMemberClass))]
    public interface IStaticInterface2
    {
        [StaticShim(typeof(StaticMemberClass))]
        void Test();
    }
    [TestMethod]
    public void Can_define_StaticShim_interface_where_members_also_have_attribute_of_same_type()
    {
        var factory = ShimBuilder.Create<IStaticInterface2>();

        factory.Test();
    }

    [StaticShim(typeof(StaticMemberClass))]
    public interface IStaticInterface3
    {
        [StaticShim(typeof(StaticMemberClass2))]
        void Test();
    }
    [TestMethod]
    public void Can_define_StaticShim_interface_where_members_also_have_attribute_of_different_type()
    {
        var factory = ShimBuilder.Create<IStaticInterface3>();

        factory.Test();
    }
}