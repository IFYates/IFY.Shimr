# Shimterface
Utility for creating a dynamic object facade/proxy to allow for using an object as an interface that it does not explicitly implement.

[![Build & Test](https://github.com/IanYates83/Shimterface/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IanYates83/Shimterface/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/IanYates83/Shimterface/badge.svg?branch=master)](https://coveralls.io/github/IanYates83/Shimterface?branch=master)

[![NuGet Version](https://img.shields.io/nuget/v/Shimterface.Standard)](https://www.nuget.org/packages/Shimterface.Standard/)
![NuGet Downloads](https://img.shields.io/nuget/dt/Shimterface.Standard)

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

Given that `TestClass` implements all of `ITest` members, logically this would be a lovely language feature. Instead, we get a runtime `InvalidCastException`.

_Shimterface_ solves and takes the effort out of this problem by compiling dynamic proxies to convert classes to the requested interface.
The above example can be fixed using _Shimterface_ by doing:

```C#
ITest forcedCast = new TestClass().Shim<ITest>();
```

If you ever needed the original back again, then you simply unshim it:

```C#
TestClass originalObject = ShimBuilder.Unshim<TestClass>(forcedCast);
```

The wiki contains lots more examples and detailed coverage of usage.

## Breaking changes
See the [breaking change wiki](https://github.com/IanYates83/Shimterface/wiki/Breaking-changes).

## Design principal
The purpose of Shimterface is to improve testability and inversion-of-control; therefore, all behavioural decisions are designed to be implemented as application design-time.
This is not a mocking library.

Outside of setting up your DI/IOC container and facades, if you're referencing Shimterface directly, it's likely that you're thinking about a different problem domain to the one solved by this library.

## Known Issues
* If the compilation of the proxy type fails but the application handles it, the Type-Interface combination is now not usable

## Future Ideas
* Generate assembly of compiled shims for direct reference
* Combine multiple target types to single shim
* Behavioural configuration to attributes (e.g., [Shim(IgnoreMissingMembers = true)])