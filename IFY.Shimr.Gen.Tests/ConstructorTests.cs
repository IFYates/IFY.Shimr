namespace IFY.Shimr.Gen.Tests;

[TestClass]
public class ConstructorTests
{
    public class TestClass
    {
        public TestClass(string arg1)
        {
        }

        // Instance members
        public string Value { get; set; }
    }
    [StaticShim(typeof(TestClass))]
    public interface ITestFactory
    {
        [ConstructorShim]
        ITestClass CreateNew2(string arg1);
        [ConstructorShim(typeof(TestClass))]
        ITestClass CreateNew(string arg1);
    }
    public interface ITestClass
    {
        // Instance members
        string Value { get; set; }
    }

    [TestMethod]
    public void DoTest()
    {
        ITestFactory factory = ShimBuilder.Create<ITestFactory>();
        ITestClass inst = factory.CreateNew("");
    }
}
