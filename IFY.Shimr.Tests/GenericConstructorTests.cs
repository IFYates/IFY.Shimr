using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IFY.Shimr.Tests;

[TestClass]
public class GenericConstructorTests
{
    public interface IInstanceInterface<T>
    {
        T Value { get; }
        string Text { get; }
        int Count { get; }
    }
    public interface IFactoryInterface
    {
        [ConstructorShim(typeof(TestClass<>))]
        IInstanceInterface<T> New<T>();
    }
    public interface IFactoryInterfaceWithArg
    {
        [ConstructorShim(typeof(TestClass<>))]
        IInstanceInterface<T> New<T>(T value);
    }
    public interface IFactoryInterfaceWithDeepGeneric
    {
        [ConstructorShim(typeof(TestClass<>))]
        IInstanceInterface<T> New<T>(IEnumerable<T> value);
    }
    public interface IFactoryInterfaceManyArgs
    {
        [ConstructorShim(typeof(TestClass<>))]
        IInstanceInterface<T> New<T>(int a, int b, int c, int d, int e);
    }

    [ExcludeFromCodeCoverage]
    public class TestClass<T>
    {
        public T Value { get; private set; } = default!;
        public string Text { get; set; } = null!;
        public int Count { get; set; }

        public TestClass()
        {
            Count = 0;
        }
        public TestClass(T value)
        {
            Value = value;
            Count = 1;
        }
        public TestClass(IEnumerable<T> value)
        {
            Value = value.First();
            Count = value.Count();
        }
        public TestClass(int a, int b, int c, int d, int e)
        {
            Value = default!;
            Count = a + b + c + d + e;
        }
    }

    public interface ITest
    {
        object Exec();
    }

    [TestMethod]
    public void Can_shim_to_constructor_without_args()
    {
        var shim = ShimBuilder.Create<IFactoryInterface>();

        var inst = shim.New<string>();

        Assert.AreEqual(0, inst.Count);
        Assert.IsInstanceOfType(((IShim)inst).Unshim(), typeof(TestClass<string>));
    }

    [TestMethod]
    public void Can_shim_to_constructor()
    {
        var shim = ShimBuilder.Create<IFactoryInterfaceWithArg>();

        var instA = shim.New("one");
        var instB = shim.New(2);

        Assert.AreEqual("one", instA.Value);
        Assert.AreEqual(2, instB.Value);
        Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass<string>));
    }

    [TestMethod]
    public void Can_shim_to_constructor_with_deep_generic()
    {
        var shim = ShimBuilder.Create<IFactoryInterfaceWithDeepGeneric>();

#pragma warning disable IDE0001 // Name can be simplified
        var instA = shim.New<string>(["one", "two"]);
        var instB = shim.New<int>([2, 3, 4, 5]);
#pragma warning restore IDE0001 // Name can be simplified

        Assert.AreEqual("one", instA.Value);
        Assert.AreEqual(2, instA.Count);
        Assert.AreEqual(2, instB.Value);
        Assert.AreEqual(4, instB.Count);
        Assert.IsInstanceOfType(((IShim)instA).Unshim(), typeof(TestClass<string>));
    }

    [TestMethod]
    public void Can_shim_to_constructor_with_multiple_args()
    {
        var shim = ShimBuilder.Create<IFactoryInterfaceManyArgs>();

        var inst = shim.New<string>(1, 2, 3, 4, 5);

        Assert.IsNull(inst.Value);
        Assert.AreEqual(1 + 2 + 3 + 4 + 5, inst.Count);
        Assert.IsInstanceOfType(((IShim)inst).Unshim(), typeof(TestClass<string>));
    }
}