using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Shimterface.Tests
{
    // Specific issues around these structures
    [TestClass]
    public class MultiLevelInheritanceTests
    {
        [TestInitialize]
        public void Reset()
        {
            ShimBuilder.ResetState();
        }

        // Low-level interface
        public interface IBase
        {
            IEnumerable<int> Values { get; set; }
            IEnumerable<int> GetValues();
        }

        // Explicitly implements interface and changes signature
        public class Impl : IBase
        {
            public int[] Values { get; set; }
            IEnumerable<int> IBase.Values { get => Values; set => Values = value.ToArray(); }

            public int[] GetValues() => Values;
            IEnumerable<int> IBase.GetValues() => GetValues();
        }

        // Extends Impl with further signature change
        public class Extr : Impl
        {
            new public string[] Values => base.Values.Select(v => v.ToString()).ToArray();

            new public string[] GetValues() => Values;
        }

        // Shim wants everything, including inherited
        public interface IFullShim : IBase
        {
            [Shim("Values")]
            int[] IntValues { get; set; } // From Impl
            [Shim("Values")]
            string[] StringValues { get; } // From Extr

            [Shim("GetValues")]
            int[] GetIntValues(); // From Impl
            [Shim("GetValues")]
            string[] GetStringValues(); // From Extr
        }

        [TestMethod]
        public void Can_shim_property_by_type()
        {
            var obj = new Extr();
            ((IBase)obj).Values = new[] { 1, 2, 3 };

            var shim = obj.Shim<IFullShim>();

            Assert.IsNotNull(shim);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.Values.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.IntValues);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.StringValues);
        }

        [TestMethod]
        public void Can_shim_method_by_type()
        {
            var obj = new Extr();
            ((IBase)obj).Values = new[] { 1, 2, 3 };

            var shim = obj.Shim<IFullShim>();

            Assert.IsNotNull(shim);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetValues().ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetIntValues());
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.GetStringValues());
        }

        // Shim wants everything, without inheritance
        public interface INotInheritedShim
        {
            [Shim(typeof(IBase), "Values")]
            IEnumerable<int> BaseValues { get; set; }
            [Shim("Values")]
            int[] IntValues { get; set; } // From Impl
            [Shim("Values")]
            string[] StringValues { get; } // From Extr

            [Shim(typeof(IBase))]
            IEnumerable<int> GetValues();
            [Shim("GetValues")]
            int[] GetIntValues(); // From Impl
            [Shim("GetValues")]
            string[] GetStringValues(); // From Extr
        }

        [TestMethod]
        public void Can_shim_property_by_type_without_inheritance()
        {
            var obj = new Extr();
            ((IBase)obj).Values = new[] { 1, 2, 3 };

            var shim = obj.Shim<INotInheritedShim>();

            Assert.IsNotNull(shim);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.BaseValues.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.IntValues);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.StringValues);
        }

        [TestMethod]
        public void Can_shim_method_by_type_inheritance()
        {
            var obj = new Extr();
            ((IBase)obj).Values = new[] { 1, 2, 3 };

            var shim = obj.Shim<INotInheritedShim>();

            Assert.IsNotNull(shim);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetValues().ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetIntValues());
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.GetStringValues());
        }
    }
}
