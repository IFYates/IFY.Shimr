using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests
{
    /// <summary>
    /// Tests for an interface that is decorated with <see cref="StaticShimAttribute"/>.
    /// </summary>
    [TestClass]
    public class StaticInterfaceTests
    {
        [ExcludeFromCodeCoverage]
        public class StaticMemberClass
        {
            public static string Value { get; set; }

            public static bool _HasCalled = false;
            public static void Test()
            {
                _HasCalled = true;
            }

            public StaticMemberClass()
            {
            }
        }

        [ExcludeFromCodeCoverage]
        public class StaticMemberClass2
        {
            public static void Test()
            {
            }
        }

        [TestInitialize]
        public void ResetState()
        {
            ShimBuilder.ResetState();
        }

        [StaticShim(typeof(StaticMemberClass))]
        public interface IStaticInterface1
        {
            string Value { get; set; } // Property
            bool _HasCalled { get; set; } // Field
            void Test(); // Method
            [ConstructorShim(typeof(StaticMemberClass))] StaticMemberClass New(); // Constructor
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
}