using IFY.Shimr.Extensions;

namespace IFY.Shimr.Examples;

[TestClass]
public class DateTimeExample
{
    [Shimgen(typeof(TimeSpan))]
    public interface ITimeSpan
    {
        double TotalSeconds { get; }
    }
    [Shimgen(typeof(TimeSpan))]
    public interface ITimeSpan2
    {
        double TotalSeconds { get; }
    }
    [Shimgen(typeof(DateTime))]
    public interface IDateTime
    {
        ITimeSpan Subtract([TypeShim(typeof(DateTime))] IDateTime value);
        ITimeSpan TimeOfDay { get; }
        string ToString(string dateFormat); // Arg rename ignored, nullability ignored
    }

    [StaticShim(typeof(DateTime))]
    public interface IDateTimeFactory
    {
        IDateTime Now { get; }
    }

    [TestMethod]
    public void DateTime_can_be_wrapped()
    {
        DateTime dt = DateTime.UtcNow;
        string exp = dt.ToString("o");

#if SHIMR_CG
        IDateTime shim = dt.Shim<IDateTime>()!;
#else
        IDateTime shim = ShimBuilder.Shim<IDateTime>(dt)!;
#endif

        string res = shim.ToString("o");
        Assert.AreEqual(exp, res);
    }

    [TestMethod]
    public void DateTime_shim_can_return_ITimeSpan()
    {
        DateTime dt = DateTime.UtcNow;
#if SHIMR_CG
        IDateTime shim = dt.Shim<IDateTime>()!;
#else
        IDateTime shim = ShimBuilder.Shim<IDateTime>(dt)!;
#endif

        var res = shim.Subtract(DateTime.Today.Shim<IDateTime>());
        Assert.AreEqual(shim.TimeOfDay.TotalSeconds, res.TotalSeconds);
    }

    [TestMethod]
    public void Factory_can_redefine_Now()
    {
        IDateTimeFactory dt = ShimBuilder.Create<IDateTimeFactory>();
        IDateTime now = dt.Now;

        Assert.IsNotInstanceOfType<DateTime>(now);
        Assert.IsInstanceOfType<IDateTime>(now);
        Assert.IsInstanceOfType<IShim>(now);
        Assert.IsInstanceOfType<DateTime>(((IShim)now).Unshim());
    }
}
