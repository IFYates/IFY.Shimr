# Shimterface
Utility for creating a dynamic object facade/proxy to allow for using an object as an interface that it does not explicitly implement

[Available on nuget.](https://www.nuget.org/packages/Shimterface.Standard/)

## Breaking changes
**v1.4.0** drops the `ShimAttribute` and limits `TypeShimAttribute` to parameters only.
If the return type of a shimmed member does not match the implementation member, it must be an interface that can be auto-shimmed.

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

_Shimterface_ solves and takes the effort out of this problem by compiling dynamic proxies to convert classes to the requested interface.
The above example can be fixed using _Shimterface_ by doing:

```C#
ITest forcedCast = new TestClass().Shim<ITest>();
```

If you ever needed the original back again, then you simply unshim it:

```C#
TestClass originalObject = (TestClass)((IShim)forcedCast).Unshim();
```

## Overiding Return/Parameter Types
For simple class methods, the above will be enough to decouple from the concrete class; however, if the method returns or takes other concrete types, Inversion of Control and unit testability are not hugely improved.
The `TypeShimAttribute` can be used here to put auto shim and unshim logic around the proxy parameter implementation.

```C#
public class TestClass {
    public OtherTestClass Convert(AnotherTestClass from) {
        ...
    }
}
public interface IClassConverter {
    IOtherTestClass Convert([TypeShim(typeof(AnotherTestClass))] IAnotherTestClass from);
}
```

If the return type of the interface member is not the true return type of the method being proxied, it must be an interface that the return value will be auto-shimmed to.
The `TypeShimAttribute` on the parameter specifies the true type of the parameter in the method being proxied. The interface type will be unshimmed to the original type when passed in. Note that the passed in object must also implement `IShim` (all _Shimterface_ shims will do this) in order to be returned to the original class parameter type.

These concepts can automatically be applied to arrays and direct `IEnumerable<?>` usages.

## Renaming Members
The proxy/facade patterns can also make it easier to unify different implementations. _Shimterface_ can help by enabling renaming of implementation members using the `ShimAttribute`.

```C#
public class TestClass {
    public string LibrarySpecificName() {
        ...
    }
}
public interface IFacadeClass {
    string CommonName();
}
```

## Fields
_Shimterface_ will allow you to define a property in your proxy to cover a field in the implementation:
```C#
public class TestClass {
    public string Name;
}
public interface ITest {
    string Name { get; set; }
}
// Use: new TestClass().Shim<ITest>().Name
```

If the underlying field is readonly, defining the `set` will not fail on shim, but will fail on use.

The `ShimAttribute` works here for renaming and auto-shimming in the way you'd expect.

## Factories
For situations where you need to also proxy static members, _Shimterface_ provides a `ShimBuilder.Create<>()` method and the `StaticShimAttribute`. This allows for using proxies around static methods at runtime, but creating instance mocks at test.
A shim interface cannot contain a mix of static and instance member shims, but can contain static methods proxied from many different types.

### Static Methods
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
	IStaticTest factory = ShimBuilder.Create<IStaticTest>();
	factory.Test();
}
```

### Constructors
As with static methods, _Shimterface_ allows you to call instance constructors from a static factory, where the name of the factory method is unchecked and the return type is assumed to be the constructor provider or a shim of it.

```C#
public class TestClass {
	public TestClass(string arg1) {
		...
	}

	// Instance members
}
public interface ITestFactory {
	[StaticShim(typeof(TestClass), IsConstructor = true)]
	ITestClass CreateNew(string arg1);
}
public interface ITest {
	// Instance members
}

public void DoTest() {
	ITestFactory factory = ShimBuilder.Create<ITestFactory>();
	ITest inst = factory.CreateNew();
}
```

## Examples
### DirectoryInfo and FileInfo
File-system work is obviously extremely important in a lot of applications, but the standard `System.IO` classes are not mockable nor IoC-supportive.
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
	IDirectoryInfo Parent { get; }
	IEnumerable<IFileInfo> EnumerateFiles();
	IFileInfo[] GetFiles();
	string ToString();
}

// The bits we want from FileInfo
public interface IFileInfo
{
	bool Exists { get; }
	string FullName { get; }
	string Name { get; }
	IDirectoryInfo Directory { get; }
}

[TestMethod]
public void Test_file_system_shims()
{
	var assemblyFile = System.Reflection.Assembly.GetExecutingAssembly().Location;

	// We'll need to use the filesystem shim factory
	IFileSystem fileSystem = ShimBuilder.Create<IFileSystem>(); // In tests, this will be a mock

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
* Provide default functionality to shimmed method missing from target type
* Add concrete functionality to shimmed type (similar to extension methods)
* Combine multiple target types to single shim