# Shimterface
Utility for creating a dynamic object facade/proxy to allow for using an object as an interface that it does not explicitly implement

## Description
I'm sure we've all been in the situation where we've had to make use of a class from an external library (including mscorlib) that either doesn't implement any interface or doesn't implement one that can be used for any kind of Inversion of Control usage.
One approach is to implement a series of proxy objects that handle all of the required functionality, including proxies around returned values, and this can even be scripted (Powershell, T4, etc.) to prevent the arduous task of proxying every class you need.

You might also be frustrated that classes can't have an identical interface applied to them post-design. e.g.,

    public class TestClass {
        public void Test() {
            ...
        }
    }
    public interface ITest {
        void Test();
    }
    
    ITest forcedCast = (ITest)new TestClass();

Given that TestClass implements all of ITest members, logically this would be a lovely language feature. Instead, we get a runtime InvalidCastException.

Shimterface solves and takes the effort out of this problem by compiling dynamic proxies to convert classes to the requested interface.
The above example can be fixed using Shimterface by doing:

    ITest forcedCast = new TestClass().Shim<ITest>();

If you ever needed the original back again, then you simply unshim it:

    TestClass originalObject = (TestClass)((IShim)forcedCast).Unshim();

## Overiding Types
For simple class methods, this will be enough to decouple from the concrete class; however, if the method returns or takes other concrete types, Inversion of Control and unit testability are not hugely improved.
The TypeShimAttribute is used here to put auto shim and unshim logic around the proxy implementation.

    public class TestClass {
        public OtherTestClass Convert(AnotherTestClass from) {
            ...
        }
    }
    public interface IClassConverter {
        [TypeShim(typeof(OtherTestClass))]
        IOtherTestClass Convert([TypeShim(typeof(AnotherTestClass))] IAnotherTestClass from);
    }

The TypeShimAttribute on the member (method or property) specifies the true return type of the method being proxied, with the return type of the interface member being the target shim type (code not shown) that will be used to proxy it to.
The TypeShimAttribute on the parameter does the same, but in reverse - the interface type will be unshimmed to the original type. Note that the passed in object must also implement IShim in order to be returned to the original class parameter type.

These concepts can automatically be applied to arrays and direct IEnumerable<?> usages.

## Factories (Static Proxies)
For situations where you need to also proxy static members, Shimterface provides a StaticShimAttribute. This allows for using proxies around static methods at runtime, but creating instance mocks at test.
A shim interface cannot contain a mix of static and instance member shims.

	public class TestClass {
		public static void Test() {
			...
		}

		// Instance members
	}
	public interface IStaticTest {
		[StaticShim(typeof(TestClass))]
		void Test();
	}

	public void DoTest() {
		IStaticTest factory = Shimterface.Create<IStaticTest>();
		factory.Test();
	}

## Examples
### DirectoryInfo and FileInfo
Filesystem work is obviously extremely important in a lot of applications, but the standard System.IO classes are not mockable nor IoC-supportive.
If we were searching for all text files in a directory, but wanted to be able to mock the implementation for testing, we could approach it as such:

    // Shim types
    public interface IFileInfo {
        string FullName { get; }
    }
    public interface IDirectoryInfo {
        [TypeShim(typeof(FileInfo[]))]
        IFileInfo[] GetFiles(string searchPattern);
    }
    
    // Implementation
    public string[] GetAllTextFiles(IDirectoryInfo dir) {
        IFileInfo[] files = dir.GetFiles("*.txt");
        string[] fileNames = files.Select(f => f.FullName).ToArray();
        return fileNames;
    }
    
    // Test
    [TestMethod]
    public void Test_can_get_all_text_files() {
        var fileMock = new Mock<IFileInfo>();
        fileMock.SetupPropertyGet(m => m.FullName).Returns("test");
        IFileInfo[] files = new [] {
            fileMock.Object
        };
        
        var dirMock = new Mock<IDirectoryInfo>(MockBehavior.Strict);
        dirMock.Setup(m => m.GetFiles("*.txt")).Returns(files);
        
        string[] result = impl.GetAllTextFiles(dirMock.Object);
        
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("test", result[0]);
    }

Obviously, you'll still need to find a way to construct the concrete types properly during implementation. For this example, the factory will likely look something like:

    public IDirectoryInfo GetDirectory(string path) {
        return new DirectoryInfo(path).Shim<IDirectoryInfo>();
    }
