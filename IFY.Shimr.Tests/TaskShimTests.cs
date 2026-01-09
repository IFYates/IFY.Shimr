using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace IFY.Shimr.Tests;

[TestClass]
public class TaskShimTests
{
    public interface IToString
    {
        string ToString();
    }

    public interface ITaskShim
    {
        Task<IToString> GetStringAsync();
    }
    public interface IBadTaskShim
    {
        Task<int> GetStringAsync();
    }

    public class TaskImpl
    {
#pragma warning disable CA1822 // Mark members as static
        public Task<string> GetStringAsync() => Task.FromResult("shimmed");
#pragma warning restore CA1822 // Mark members as static
    }

    [TestMethod]
    public async Task Shim_Task_return_types()
    {
        var impl = new TaskImpl();
        var shim = ShimBuilder.Shim<ITaskShim>(impl);

        var str = await shim.GetStringAsync();

        Assert.AreEqual("shimmed", str.ToString());
    }

    [TestMethod]
    public async Task Shim_Task_with_invalid_shim_fails()
    {
        var impl = new TaskImpl();

        Assert.ThrowsException<NotSupportedException>
            (() => ShimBuilder.Shim<IBadTaskShim>(impl));
    }

    public interface IValueTaskShim
    {
        ValueTask<IToString> GetIntAsync();
    }
    public interface IBadValueTaskShim
    {
        ValueTask<string> GetIntAsync();
    }

    public class ValueTaskImpl
    {
#pragma warning disable CA1822 // Mark members as static
        public ValueTask<int> GetIntAsync() => new(42);
#pragma warning restore CA1822 // Mark members as static
    }

    [TestMethod]
    public async Task Shim_ValueTask_return_types()
    {
        var impl = new ValueTaskImpl();
        var shim = ShimBuilder.Shim<IValueTaskShim>(impl);

        var num = await shim.GetIntAsync();

        Assert.AreEqual("42", num.ToString());
    }

    [TestMethod]
    public async Task Shim_ValueTask_with_invalid_shim_fails()
    {
        var impl = new ValueTaskImpl();

        Assert.ThrowsException<NotSupportedException>
            (() => ShimBuilder.Shim<IBadValueTaskShim>(impl));
    }
}
