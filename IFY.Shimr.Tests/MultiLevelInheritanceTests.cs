namespace IFY.Shimr.Tests;

// Specific issues around these structures
[TestClass]
public class MultiLevelInheritanceTests
{
    //#if SHIMRGEN
    //    class ShimBuilder
    //    {
    //        public static T Shim<T>(Extr obj) => obj.Shim<T>();
    //    }
    //#endif

#if !SHIMRGEN
    [TestInitialize]
    public void Reset()
    {
        ShimBuilder.ResetState();
    }
#endif

    // Original interfaces
    public interface IProperty1
    {
        int Value { get; }
    }
    public interface IProperty2
    {
        double Value { get; }
    }

    // Combine properties
#if SHIMRGEN
    [ShimOf<BothPropertiesClass>]
#endif
    public interface IBothProperties : IProperty2, IProperty1
    {
    }

    public class BothPropertiesClass : IBothProperties
    {
        int IProperty1.Value { get; } = 1;
        public double Value { get; } = 2.0;
    }

    [TestMethod]
    public void BothProperties()
    {
        var obj = new BothPropertiesClass();

        var shim = obj.Shim<IBothProperties>();
        var cast1 = (IProperty1)shim;
        var cast2 = (IProperty2)shim;

        //Assert.IsInstanceOfType(shim.Value, typeof(double)); // Not compilable
        Assert.IsInstanceOfType(cast1.Value, typeof(int));
        Assert.IsInstanceOfType(cast2.Value, typeof(double));
    }

    // Change property signature
#if SHIMRGEN
    [ShimOf<PropertySigClass>]
#endif
    public interface INewPropertySignature : IProperty2, IProperty1
    {
        new string Value { get; }
    }

    public class PropertySigClass : INewPropertySignature
    {
        public string Value { get; } = nameof(PropertySigClass);
        int IProperty1.Value { get; } = 1;
        double IProperty2.Value { get; } = 2.0;
    }

    [TestMethod]
    public void PropertySigChange()
    {
        var obj = new PropertySigClass();

        var shim = obj.Shim<INewPropertySignature>();
        var cast1 = (IProperty1)shim;
        var cast2 = (IProperty2)shim;

        Assert.IsInstanceOfType(shim.Value, typeof(string));
        Assert.IsInstanceOfType(cast1.Value, typeof(int));
        Assert.IsInstanceOfType(cast2.Value, typeof(double));
    }

    // Base method definition
    public interface IMethod
    {
        int GetValue();
    }

    // Change method signature
#if SHIMRGEN
    [ShimOf<MethodSigClass>]
#endif
    public interface INewMethodSignature : IMethod
    {
        new string GetValue();
    }

    public class MethodSigClass : INewMethodSignature
    {
        int IMethod.GetValue() => 1;
        public string GetValue() => nameof(MethodSigClass);
    }

    [TestMethod]
    public void MethodSigChange()
    {
        var obj = new MethodSigClass();

        var shim = obj.Shim<INewMethodSignature>();

        Assert.IsInstanceOfType(shim.GetValue(), typeof(string));
    }

    //    // Low-level interface
    //    public interface IBase
    //    {
    //        IEnumerable<int> Values { get; set; }
    //        IEnumerable<int> GetValues();
    //    }

    //    // Explicitly implements interface and changes signature
    //    public class Impl : IBase
    //    {
    //        public int[] Values { get; set; }
    //        IEnumerable<int> IBase.Values { get => Values; set => Values = value.ToArray(); }

    //        public int[] GetValues() => Values;
    //        IEnumerable<int> IBase.GetValues() => GetValues();
    //    }

    //    // Extends Impl with further signature change
    //    public class Extr : Impl
    //    {
    //        new public string[] Values => base.Values.Select(v => v.ToString()).ToArray();

    //        new public string[] GetValues() => Values;
    //    }

    //    // Shim wants everything, including inherited
    //#if SHIMRGEN
    //    [ShimOf(typeof(Extr))]
    //#endif
    //    public interface IFullShim : IBase
    //    {
    //        [Shim("Values")]
    //        int[] IntValues { get; set; } // From Impl
    //        [Shim("Values")]
    //        string[] StringValues { get; } // From Extr

    //        [Shim("GetValues")]
    //        int[] GetIntValues(); // From Impl
    //        [Shim("GetValues")]
    //        string[] GetStringValues(); // From Extr
    //    }

    //    [TestMethod]
    //    public void Can_shim_property_by_type()
    //    {
    //        var obj = new Extr();
    //        ((IBase)obj).Values = new[] { 1, 2, 3 };

    //        var shim = obj.Shim<IFullShim>();

    //        Assert.IsNotNull(shim);
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.Values.ToArray());
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.IntValues);
    //        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.StringValues);
    //    }

    //    [TestMethod]
    //    public void Can_shim_method_by_type()
    //    {
    //        var obj = new Extr();
    //        ((IBase)obj).Values = new[] { 1, 2, 3 };

    //        var shim = obj.Shim<IFullShim>();

    //        Assert.IsNotNull(shim);
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetValues().ToArray());
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetIntValues());
    //        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.GetStringValues());
    //    }

    //    // Shim wants everything, without inheritance
    //#if SHIMRGEN
    //    [ShimOf(typeof(Extr))]
    //#endif
    //    public interface INotInheritedShim
    //    {
    //        [Shim(typeof(IBase), "Values")]
    //        IEnumerable<int> BaseValues { get; set; }
    //        [Shim("Values")]
    //        int[] IntValues { get; set; } // From Impl
    //        [Shim("Values")]
    //        string[] StringValues { get; } // From Extr

    //        [Shim(typeof(IBase))]
    //        IEnumerable<int> GetValues();
    //        [Shim("GetValues")]
    //        int[] GetIntValues(); // From Impl
    //        [Shim("GetValues")]
    //        string[] GetStringValues(); // From Extr
    //    }

    //    [TestMethod]
    //    public void Can_shim_property_by_type_without_inheritance()
    //    {
    //        var obj = new Extr();
    //        ((IBase)obj).Values = new[] { 1, 2, 3 };

    //        var shim = obj.Shim<INotInheritedShim>();

    //        Assert.IsNotNull(shim);
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.BaseValues.ToArray());
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.IntValues);
    //        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.StringValues);
    //    }

    //    [TestMethod]
    //    public void Can_shim_method_by_type_inheritance()
    //    {
    //        var obj = new Extr();
    //        ((IBase)obj).Values = new[] { 1, 2, 3 };

    //        var shim = obj.Shim<INotInheritedShim>();

    //        Assert.IsNotNull(shim);
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetValues().ToArray());
    //        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, shim.GetIntValues());
    //        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, shim.GetStringValues());
    //    }
}
