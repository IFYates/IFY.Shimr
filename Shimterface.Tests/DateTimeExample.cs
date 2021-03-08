using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shimterface.Tests
{
	[TestClass]
	public class DateTimeExample
	{
		public interface ITimeSpan
		{
			double TotalSeconds { get; }
		}
		public interface IDateTime
		{
			[Shim(typeof(TimeSpan))]
			ITimeSpan Subtract([TypeShim(typeof(DateTime))] IDateTime value);
			[Shim(typeof(TimeSpan))]
			ITimeSpan TimeOfDay { get; }
			string ToString(string format);
		}

		public interface IDateTimeFactory
		{
			[StaticShim(typeof(DateTime))]
			[Shim(typeof(DateTime))]
			IDateTime Now { get; }
		}
		
		[TestMethod]
		public void DateTime_can_be_wrapped()
		{
			DateTime dt = DateTime.UtcNow;
			string exp = dt.ToString("o");

			IDateTime shim = ShimBuilder.Shim<IDateTime>(dt);

			string res = shim.ToString("o");
			Assert.AreEqual(exp, res);
		}

		[TestMethod]
		public void DateTime_shim_can_return_ITimeSpan()
		{
			DateTime dt = DateTime.UtcNow;
			IDateTime shim = ShimBuilder.Shim<IDateTime>(dt);

			var res = shim.Subtract(DateTime.Today.Shim<IDateTime>());
			Assert.AreEqual(shim.TimeOfDay.TotalSeconds, res.TotalSeconds);
		}

		[TestMethod]
		public void Factory_can_redefine_Now()
		{
			IDateTimeFactory dt = ShimBuilder.Create<IDateTimeFactory>();
			IDateTime now = dt.Now;

			Assert.IsFalse(now is DateTime);
			Assert.IsTrue(now is IDateTime);
			Assert.IsTrue(now is IShim);
			Assert.IsTrue(((IShim)now).Unshim() is DateTime);
		}
	}
}
