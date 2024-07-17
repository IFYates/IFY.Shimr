using IFY.Shimr.Extensions;
using System;
using System.Diagnostics;

namespace IFY.Shimr.Tests;

/// <summary>
/// Not true tests
/// Very rough examples of overall performance
/// </summary>
[TestClass]
public class PerformanceTests
{
    public class Impl
    {
        public long Count { get; private set; }

        public void Exec()
        {
            ++Count;
        }
    }

    public interface IImplShim
    {
        long Count { get; }

        void Exec();
    }

    public class ImplProxy(PerformanceTests.Impl obj) : IImplShim
    {
        private readonly Impl _obj = obj;

        public long Count => _obj.Count;

        public void Exec()
        {
            _obj.Exec();
        }
    }

    [TestMethod]
    public void PerfTest__method_call_cost()
    {
        var its = 25000000;

        // Standard method call
        var obj1 = new Impl();
        var sw1 = Stopwatch.StartNew();
        for (var i = 1; i <= its; ++i)
        {
            obj1.Exec();
            Assert.AreEqual(i, obj1.Count);
        }
        sw1.Stop();

        // Custom proxy method call
        var obj2 = new ImplProxy(new Impl());
        var sw2 = Stopwatch.StartNew();
        for (var i = 1; i <= its; ++i)
        {
            obj2.Exec();
            Assert.AreEqual(i, obj2.Count);
        }
        sw2.Stop();

        // Shimterface method call
        var obj3 = new Impl().Shim<IImplShim>()!;
        var sw3 = Stopwatch.StartNew();
        for (var i = 1; i <= its; ++i)
        {
            obj3.Exec();
            Assert.AreEqual(i, obj3.Count);
        }
        sw3.Stop();

        // Typical results are that Shim is ~25% faster than Proxy (and sometimes ~3% than Standard) in Debug
        // but between 5% and 35% (rare) slower than Standard in Release mode, which is normally the same as Proxy (+/- a few %)
        Console.WriteLine("-- Performed {0} iterations --", its);
        Console.WriteLine("* Standard: " + sw1.Elapsed);
        Console.WriteLine("* Proxy:    " + sw2.Elapsed);
        Console.WriteLine("* Shim:     " + sw3.Elapsed);
        Console.WriteLine("* Proxy cost: " + ((double)sw2.ElapsedTicks / sw1.ElapsedTicks));
        Console.WriteLine("* Shim cost:  " + ((double)sw3.ElapsedTicks / sw1.ElapsedTicks));
    }
}
