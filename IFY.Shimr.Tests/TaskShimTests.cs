using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFY.Shimr;
using IFY.Shimr.Extensions;

namespace IFY.Shimr.Tests
{
    [TestClass]
    public class TaskShimTests
    {
        public interface ITaskShim
        {
            Task<string> GetStringAsync();
            ValueTask<int> GetIntAsync();
        }

        public class TaskImpl
        {
            public Task<string> GetStringAsync() => Task.FromResult("shimmed");
            public ValueTask<int> GetIntAsync() => new ValueTask<int>(42);
        }

        [TestMethod]
        public async Task Shim_Task_and_ValueTask_return_types()
        {
            var impl = new TaskImpl();
            var shim = ShimBuilder.Shim<ITaskShim>(impl);

            var str = await shim.GetStringAsync();
            var num = await shim.GetIntAsync();

            Assert.AreEqual("shimmed", str);
            Assert.AreEqual(42, num);
        }
    }
}
