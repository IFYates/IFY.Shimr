namespace IFY.Shimr.Gen.Tests;

[TestClass]
public class UnitTest1
{
    public class SubClass
    {
        public string Value { get; set; }
    }

    public class TestClass
    {
        public bool Called;

        public string Value;
        public string Value2 { get; set; }
        public SubClass Value3 { get; set; }

        public void Test()
        {
            Called = true;
        }
        public void Test2(string arg)
        {
        }
        public int Test3(string arg, int id, bool test = false)
        {
            return 1;
        }
        public SubClass Test4(string inp)
        {
            return new SubClass { Value = inp };
        }
        public void Test5(SubClass v)
        {
        }
    }

    [Shimr(typeof(TestClass))]
    public interface ITestSub
    {
        ISubTest? Value3 { get; set; }
        ISubTest Test4(string inp = "test");
        public void TestZ() { }
    }
    public interface ISubTest
    {
        [Shim(nameof(SubClass.Value))]
        string Name { get; }
    }

    [TestMethod]
    public void MyTestMethod()
    {
        var obj = new TestClass
        {
            Value3 = new SubClass { Value = "X" }
        };
        var shim = obj.Shim<ITestSub>();

        var r = shim.Test4();
        var s1 = shim.Value3;
        shim.Value3 = new SubClass { Value = "Y" }.Shim<ISubTest>();
        var s2 = shim.Value3;

        Assert.AreEqual("test", r.Name);
        Assert.AreEqual("X", s1.Name);
        Assert.AreEqual("Y", s2.Name);
    }

    //[Shimr(typeof(TestClass))]
    //public interface ITestShim
    //{
    //    string Value { get; set; }
    //    [Shim(nameof(TestClass.Value2))]
    //    string ValueX { get; set; }
    //    [Shim(nameof(TestClass.Test))]
    //    void TestX();
    //    void Test2(string arg);
    //    int Test3(string arg, int id);
    //    int Test3(string arg, int id, bool test = true);
    //}

    [Shimr(typeof(TestClass))]
    public interface ITestShim2
    {
        void Test();
        void Test5([TypeShim(typeof(SubClass))] ISubTest v);
    }

    [TestMethod]
    public void TestMethod1()
    {
        var obj = new TestClass();
        var s = obj.Shim<ITestShim2>();
        s.Test5(new SubClass { Value = "A" }.Shim<ISubTest>());
    }
}