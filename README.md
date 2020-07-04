# Shimterface
Utility for creating a dynamic object facade/proxy to allow for using an object as an interface that it does not explicitly implement

## Description
I'm sure we've all been in the situation where we've had to make use of a class from an external library (including mscorlib) that either doesn't implement any interface or doesn't implement one that can be used for any kind of Inversion of Control usage.
One approach is to implement a series of proxy objects that handle all of the required functionality, including proxies around returned values, and this can even be scripted (Powershell, T4, etc.) to prevent the arduous task of proxying every class you need.

You might also be frustrated that classes can't have an identical interface applied to them post-design. e.g.,

```C#
public class TestClass {
    public void Test() {
        ...
    }
}
public interface ITest {
    void Test();
}

ITest forcedCast = (ITest)new TestClass();
```

Given that `TestClass` implements all of `ITest` members, logically this would be a lovely language feature. Instead, we get a runtime InvalidCastException.

Shimterface solves and takes the effort out of this problem by compiling dynamic proxies to convert classes to the requested interface.
The above example can be fixed using Shimterface by doing:

```C#
ITest forcedCast = new TestClass().Shim<ITest>();
```

If you ever needed the original back again, then you simply unshim it:

```C#
TestClass originalObject = (TestClass)((IShim)forcedCast).Unshim();
```

## Overiding Types
For simple class methods, this will be enough to decouple from the concrete class; however, if the method returns or takes other concrete types, Inversion of Control and unit testability are not hugely improved.
The `TypeShimAttribute` is used here to put auto shim and unshim logic around the proxy implementation.

```C#
public class TestClass {
    public OtherTestClass Convert(AnotherTestClass from) {
        ...
    }
}
public interface IClassConverter {
    [TypeShim(typeof(OtherTestClass))]
    IOtherTestClass Convert([TypeShim(typeof(AnotherTestClass))] IAnotherTestClass from);
}
```

The `TypeShimAttribute` on the member (method or property) specifies the true return type of the method being proxied, with the return type of the interface member being the target shim type (code not shown) that will be used to proxy it to.
The `TypeShimAttribute` on the parameter does the same, but in reverse - the interface type will be unshimmed to the original type. Note that the passed in object must also implement IShim in order to be returned to the original class parameter type.

These concepts can automatically be applied to arrays and direct `IEnumerable<?>` usages.

## Factories (Static Proxies)
For situations where you need to also proxy static members, Shimterface provides a `StaticShimAttribute`. This allows for using proxies around static methods at runtime, but creating instance mocks at test.
A shim interface cannot contain a mix of static and instance member shims.

```C#
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
```

## Examples
### DirectoryInfo and FileInfo
Filesystem work is obviously extremely important in a lot of applications, but the standard `System.IO` classes are not mockable nor IoC-supportive.
If we were searching for all text files in a directory, but wanted to be able to mock the implementation for testing, we could approach it as such:

```C#
// Static filesystem-related methods as a "factory"
public interface IFileSystem
{
	[StaticShim(typeof(Directory))]
	IDirectoryInfo GetParent(string path);
}

// The bits we want from DirectoryInfo
public interface IDirectoryInfo
{
	bool Exists { get; }
	string FullName { get; }
	[TypeShim(typeof(DirectoryInfo))]
	IDirectoryInfo Parent { get; }
	[TypeShim(typeof(IEnumerable<FileInfo>))]
	IEnumerable<IFileInfo> EnumerateFiles();
	[TypeShim(typeof(FileInfo[]))]
	IFileInfo[] GetFiles();
	string ToString();
}

// The bits we want from FileInfo
public interface IFileInfo
{
	bool Exists { get; }
	string FullName { get; }
	string Name { get; }
	[TypeShim(typeof(DirectoryInfo))]
	IDirectoryInfo Directory { get; }
}

[TestMethod]
public void Test_file_system_shims()
{
	var assemblyFile = System.Reflection.Assembly.GetExecutingAssembly().Location;

	// We'll need to use the filesystem shim factory
	var fileSystem = ShimBuilder.Create<IFileSystem>(); // In tests, this will be a mock

	// Make use of various shimmed methods
	IDirectoryInfo dir = fileSystem.GetParent(assemblyFile);
	IFileInfo[] files = dir.GetFiles();
	IFileInfo[] fileEnum = dir.EnumerateFiles().ToArray();

	// Check it returns what we expect
	Assert.IsTrue(dir.Exists);
	Assert.IsTrue(dir.Parent is IDirectoryInfo);
	Assert.AreEqual(Directory.GetParent(assemblyFile).FullName, dir.FullName);

	Assert.IsTrue(files.Length > 0);
	CollectionAssert.AreEqual(files.Select(f => f.FullName).OrderBy(f => f).ToArray(), fileEnum.Select(f => f.FullName).OrderBy(f => f).ToArray());
}
```

## Known Issues
* If the compilation of the proxy type fails but the application handles it, the Type-Interface combination is now not usable

## Future Ideas
* Generate assembly of compiled shims for direct reference
* Use shim factory to call constructor of target type
* Rename of shimmed member
* Provide default functionality to shimmed method missing from target type
* Add concrete functionality to shimmed type (similar to extension methods)
* Combine multiple target types to single shim