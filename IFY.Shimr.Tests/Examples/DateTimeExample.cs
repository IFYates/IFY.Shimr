using IFY.Shimr.Extensions;

namespace IFY.Shimr.Examples;

[TestClass]
public class DateTimeExample
{
    public interface ITimeSpan
    {
        double TotalSeconds { get; }
    }
    public interface ITimeSpan2
    {
        double TotalSeconds { get; }
    }
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
    public void DateTime_can_be_wrapped_and_unwrapped()
    {
        // Arrange
        DateTime dt = DateTime.UtcNow;
        string exp = dt.ToString("o");

        // Act
#if SHIMR_CG
        IDateTime shim = dt.Shim<IDateTime>();
#else
        IDateTime shim = ShimBuilder.Shim<IDateTime>(dt)!;
#endif

        // Assert
        Assert.IsInstanceOfType<IDateTime>(shim);
        Assert.IsInstanceOfType<DateTime>(((IShim)shim).Unshim());
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
        Assert.IsInstanceOfType<IShim>(now);
    }
}
