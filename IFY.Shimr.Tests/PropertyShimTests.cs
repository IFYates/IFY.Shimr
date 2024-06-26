﻿using IFY.Shimr.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE1006 // Naming Styles
namespace IFY.Shimr.Tests
{
    [TestClass]
    public class PropertyShimTests
    {
        public interface IGetPropertyTest
        {
            string GetProperty { get; }
        }
        public interface IGetPropertyWithSetTest
        {
            string GetSetProperty { get; }
        }
        public interface ISetPropertyTest
        {
            string SetProperty { set; }
        }
        public interface ISetPropertyWithGetTest
        {
            string GetSetProperty { set; }
        }
        public interface IGetSetPropertyTest
        {
            string GetSetProperty { get; set; }
        }

        [ExcludeFromCodeCoverage]
        public class TestClass
        {
            public string GetProperty => "value";

            public string SetProperty { set => _SetPropertyValue = value; }
            public string _SetPropertyValue = null;

            public string GetSetProperty { get; set; }
        }

        [TestMethod]
        public void Can_use_a_get_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetPropertyTest>(obj);

            var res = shim.GetProperty;
            Assert.AreEqual("value", res);
        }

        [TestMethod]
        public void Can_use_a_set_property()
        {
            var obj = new TestClass();
            Assert.IsNull(obj._SetPropertyValue);

            var shim = ShimBuilder.Shim<ISetPropertyTest>(obj);
            shim.SetProperty = "test";

            Assert.AreEqual("test", obj._SetPropertyValue);
        }

        [TestMethod]
        public void Can_use_a_set_property_with_real_get()
        {
            var obj = new TestClass();
            Assert.IsNull(obj.GetSetProperty);

            var shim = ShimBuilder.Shim<ISetPropertyWithGetTest>(obj);
            shim.GetSetProperty = "test";

            Assert.AreEqual("test", obj.GetSetProperty);
        }

        [TestMethod]
        public void Can_use_a_get_property_with_real_set()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetPropertyWithSetTest>(obj);
            Assert.IsNull(shim.GetSetProperty);
            obj.GetSetProperty = "test";
            Assert.AreEqual("test", shim.GetSetProperty);
        }

        [TestMethod]
        public void Can_use_a_getset_property()
        {
            var obj = new TestClass();

            var shim = ShimBuilder.Shim<IGetSetPropertyTest>(obj);

            Assert.IsNull(obj.GetSetProperty);
            shim.GetSetProperty = "test_getset";
            Assert.AreEqual("test_getset", shim.GetSetProperty);
        }

        #region Tricky method name

        public class TrickyMethodClass
        {
            private string _value = null;
            public string get_Method() => _value;
            public void set_Method(string value) { _value = value; }
        }
        public interface ITrickyMethodShim
        {
            string get_Method();
            void set_Method(string value);
        }
        public interface ITrickyPropertyShim
        {
            string Method { get; set; }
        }

        [TestMethod]
        public void Not_tricked_by_method_naming()
        {
            var obj = new TrickyMethodClass();

            var shim = obj.Shim<ITrickyMethodShim>();

            Assert.IsNull(obj.get_Method());
            shim.set_Method("test");
            Assert.AreEqual("test", shim.get_Method());
        }

        [TestMethod]
        public void Cannot_force_property_over_methods()
        {
            var obj = new TrickyMethodClass();

            Assert.ThrowsException<MissingMemberException>(() =>
            {
                obj.Shim<ITrickyPropertyShim>();
            });
        }

        #endregion Tricky method name

        #region Issue 12 - Hidden property causes ambiguous exception

        public abstract class Issue12BaseClass
        {
            public string Value { get; set; } = "base";
        }

        public class Issue12Class : Issue12BaseClass
        {
            new public int Value { get; set; } = 12;
        }

        public interface IShimIssue12
        {
            string Value { get; }
        }

        [TestMethod]
        public void Issue11()
        {
            var obj = new Issue12Class();
            Assert.AreEqual(12, obj.Value);

            var shim = obj.Shim<IShimIssue12>();
            Assert.AreEqual("base", shim.Value);
        }

        #endregion Issue 12

        #region Issue 28 - Cannot auto-shim IEnumerable return type

        public sealed class ClassWithCollectionProperty
        {
            public List<string> Values { get; } = new();
        }

        public interface IShimWithICollectionAutoshim
        {
            ICollection<string> Values { get; }
        }

        [TestMethod]
        public void Can_autoshim_using_IEnumerable_return_type()
        {
            // Arrange
            var obj = new ClassWithCollectionProperty();
            var shim = obj.Shim<IShimWithICollectionAutoshim>();

            // Act
            obj.Values.Add("A");
            shim.Values.Add("B");

            // Assert
            CollectionAssert.AreEqual(new[] { "A", "B" }, shim.Values.ToArray());
            Assert.AreSame(shim.Values, obj.Values);
        }

        #endregion
    }
}
