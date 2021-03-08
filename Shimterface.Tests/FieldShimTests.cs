using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Shimterface.Tests
{
    [TestClass]
    public class FieldShimTests
    {
        // TODO: static

        public interface IGetFieldTest
        {
            string Value { get; }
        }
        public interface ISetFieldTest
        {
            string Value { set; }
        }
        public interface IGetSetFieldTest
        {
            string Value { get; set; }
        }
        public interface IReadonlyFieldTest
        {
            string Immutable { get; set; }
        }

        public interface IOverrideFieldTest
        {
            [TypeShim(typeof(IGetSetFieldTest))]
            TestClass Child { get; set; }
        }

        public class TestClass
        {
            public string Value = "value";
            public readonly string Immutable = "readonly";

            public TestClass Child = new TestClass();
        }

        [TestMethod]
        public void Can_shim_a_field_as_a_get_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetFieldTest>(obj);

            var res = shim.Value;
            Assert.AreEqual("value", res);
        }

        [TestMethod]
        public void Can_shim_a_field_as_a_set_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<ISetFieldTest>(obj);
            shim.Value = "new_value";

            var res = obj.Value;
            Assert.AreEqual("new_value", res);
        }

        [TestMethod]
        public void Can_shim_a_field_as_a_get_set_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetSetFieldTest>(obj);
            shim.Value = "new_value";

            Assert.AreEqual(shim.Value, obj.Value);
        }

        [TestMethod]
        public void Can_shim_a_readonly_field_as_a_getset_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IReadonlyFieldTest>(obj);

            var res = shim.Immutable;
            Assert.AreEqual("readonly", res);
        }

        [TestMethod]
        public void Cannot_set_a_readonly_field_shimmed_as_a_set_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IReadonlyFieldTest>(obj);

            Assert.ThrowsException<Exception>(() =>
            {
                shim.Immutable = "new_value";
            });
        }

        [TestMethod]
        public void Shim_field_with_changed_return_type()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetFieldTest>(obj);

            var res = shim.Value;
            Assert.AreEqual("value", res);
        }

        [TestMethod]
        public void Shim_field_with_changed_set_type()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetFieldTest>(obj);

            var res = shim.Value;
            Assert.AreEqual("value", res);
        }
    }
}
