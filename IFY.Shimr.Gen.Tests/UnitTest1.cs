using System.Diagnostics.CodeAnalysis;

namespace IFY.Shimr.Gen.Tests;

[TestClass]
public class UnitTest1
{
    public class TestClass
    {
        public bool Called;

        public string Value { get; set; }
        public string Value2 { get; set; }

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
    }

    [Shimr(typeof(TestClass))]
    public interface ITestShim
    {
        string Value { get; set; }
        [Shim(nameof(TestClass.Value2))]
        string ValueX { get; set; }
        [Shim(nameof(TestClass.Test))]
        void TestX();
        void Test2(string arg);
        int Test3(string arg, int id);
        int Test3(string arg, int id, bool test = true);
    }

    [Shimr(typeof(TestClass))]
    public interface ITestShim2
    {
        void Test();
    }

    [TestMethod]
    public void TestMethod1()
    {
        var obj = new TestClass();
        var s = obj.Shim<ITestShim>();

        s.Value = "A";
        s.Test3("arg", 1, false);

        Assert.AreEqual("A", obj.Value);
        Assert.IsTrue(obj.Called);
    }
}