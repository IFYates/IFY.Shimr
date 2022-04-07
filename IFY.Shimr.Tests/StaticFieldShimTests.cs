using IFY.Shimr.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Tests
{
    [TestClass]
    public class StaticFieldShimTests
    {
        public interface IGetFieldTest
        {
            [StaticShim(typeof(TestClass))]
            long VValue { get; }
            [StaticShim(typeof(TestClass))]
            string RValue { get; }
        }
        public interface ISetFieldTest
        {
            [StaticShim(typeof(TestClass))]
            long VValue { set; }
            [StaticShim(typeof(TestClass))]
            string RValue { set; }
        }
        public interface IGetSetFieldTest
        {
            [StaticShim(typeof(TestClass))]
            long VValue { get; set; }
            [StaticShim(typeof(TestClass))]
            string RValue { get; set; }
        }
        public interface IReadonlyFieldTest
        {
            [StaticShim(typeof(TestClass))]
            string Immutable { get; set; }
        }

        public interface IStaticOverrideFieldTest
        {
            [StaticShim(typeof(TestClass))]
            IGetSetFieldTest Child { get; set; }
        }
        public interface IOverrideFieldTest
        {
            [StaticShim(typeof(TestClass))]
            IChildTest Child { get; set; }
        }
        public interface IChildTest
        {
            long InstanceValue { get; set; }
        }

        [ExcludeFromCodeCoverage]
        public class TestClass
        {
            public static long VValue;
            public static string RValue;
            public static readonly string Immutable = "readonly";

            public long InstanceValue;

            public static TestClass Child;
        }

        [TestInitialize]
        public void ResetClass()
        {
            TestClass.VValue = 12345L;
            TestClass.RValue = "value";
            TestClass.Child = null;
        }

        [TestMethod]
        public void Can_shim_a_static_value_field_as_a_get_property()
        {
            var shim = ShimBuilder.Create<IGetFieldTest>();

            Assert.AreEqual(12345L, shim.VValue);
        }

        [TestMethod]
        public void Can_shim_a_static_value_field_as_a_set_property()
        {
            var shim = ShimBuilder.Create<ISetFieldTest>();
            shim.VValue = 98765L;

            Assert.AreEqual(98765L, TestClass.VValue);
        }

        [TestMethod]
        public void Can_shim_a_static_value_field_as_a_get_set_property()
        {
            var shim = ShimBuilder.Create<IGetSetFieldTest>();
            shim.VValue = 98765L;

            Assert.AreEqual(98765L, TestClass.VValue);
        }

        [TestMethod]
        public void Can_shim_a_static_ref_field_as_a_get_property()
        {
            var shim = ShimBuilder.Create<IGetFieldTest>();

            Assert.AreEqual("value", shim.RValue);
        }

        [TestMethod]
        public void Can_shim_a_static_ref_field_as_a_set_property()
        {
            var shim = ShimBuilder.Create<ISetFieldTest>();
            shim.RValue = "new_value";

            Assert.AreEqual("new_value", TestClass.RValue);
        }

        [TestMethod]
        public void Can_shim_a_static_ref_field_as_a_get_set_property()
        {
            var shim = ShimBuilder.Create<IGetSetFieldTest>();
            shim.RValue = "new_value";

            Assert.AreEqual(shim.RValue, TestClass.RValue);
        }

        [TestMethod]
        public void Can_shim_a_static_readonly_field_as_a_getset_property()
        {
            var shim = ShimBuilder.Create<IReadonlyFieldTest>();

            Assert.AreEqual("readonly", shim.Immutable);
        }

        [TestMethod]
        public void Cannot_set_a_static_readonly_field_shimmed_as_a_set_property()
        {
            var shim = ShimBuilder.Create<IReadonlyFieldTest>();

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            {
                shim.Immutable = "new_value";
            });

            Assert.IsTrue(ex.Message.Contains("Operation is not valid "), ex.Message);
        }

        [TestMethod]
        public void Shim_static_field_cannot_shim_return_as_static()
        {
            TestClass.Child = new TestClass();

            var shim = ShimBuilder.Create<IStaticOverrideFieldTest>();

            var ex = Assert.ThrowsException<InvalidCastException>(() =>
            {
                shim.Child.ToString(); // Throws exception on auto-shim
            });

            Assert.AreEqual("Instance shim cannot implement static member: IFY.Shimr.Tests.StaticFieldShimTests+IGetSetFieldTest get_VValue", ex.Message);
        }

        [TestMethod]
        public void Shim_static_field_with_changed_return_type()
        {
            TestClass.Child = new TestClass();

            var shim = ShimBuilder.Create<IOverrideFieldTest>();

            Assert.AreSame(TestClass.Child, ((IShim)shim.Child).Unshim());
        }

        [TestMethod]
        public void Shim_static_field_with_changed_set_type()
        {
            var newChild = new TestClass();
            var newChildShim = newChild.Shim<IChildTest>();

            var shim = ShimBuilder.Create<IOverrideFieldTest>();
            shim.Child = newChildShim;

            Assert.AreSame(newChild, TestClass.Child);
        }
    }
}
