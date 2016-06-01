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
			[TypeShim(typeof(TimeSpan))]
			ITimeSpan Subtract([TypeShim(typeof(DateTime))] IDateTime value);
			[TypeShim(typeof(TimeSpan))]
			ITimeSpan TimeOfDay { get; }
			string ToString(string format);
		}

		public interface IDateTimeFactory
		{
			[StaticShim(typeof(DateTime))]
			[TypeShim(typeof(DateTime))]
			IDateTime Now { get; }
		}

		[TestMethod]
		public void DateTime_shim_can_return_ITimeSpan()
		{
			var dt = DateTime.UtcNow;
			var shim = Shimterface.Shim<IDateTime>(dt);

			var fmt1 = dt.ToString("yyyy-MM-dd HH:mm");
			var fmt2 = shim.ToString("yyyy-MM-dd HH:mm");
			Assert.AreEqual(fmt1, fmt2);

			var res = shim.Subtract(DateTime.Today.Shim<IDateTime>());
			Assert.AreEqual(shim.TimeOfDay.TotalSeconds, res.TotalSeconds);
		}

		[TestMethod]
		public void Factory_can_redefine_Now()
		{
			var dt = Shimterface.Create<IDateTimeFactory>();
			var now = dt.Now;
			Assert.IsTrue(now is IDateTime);
			Assert.IsTrue(now is IShim);
			Assert.IsTrue(((IShim)now).Unshim() is DateTime);
		}
	}
}
