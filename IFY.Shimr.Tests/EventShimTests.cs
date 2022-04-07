using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Shimr.Tests
{
    [TestClass]
    public class EventShimTests
    {
        public interface IEventTest
        {
            event EventHandler ConsoleCancelEvent;
        }

        public class EventTest
        {
            public event EventHandler ConsoleCancelEvent;

            public int _EventCount = 0;

            public EventTest()
            {
                ConsoleCancelEvent += (s, a) =>
                {
                    ++_EventCount;
                };
            }

            public void FireEvent()
            {
                ConsoleCancelEvent(this, new EventArgs());
            }
        }

        [TestMethod]
        public void Can_subscribe_to_event()
        {
            var obj = new EventTest();
            Assert.AreEqual(0, obj._EventCount);

            obj.FireEvent();
            Assert.AreEqual(1, obj._EventCount);

            var shim = ShimBuilder.Shim<IEventTest>(obj);

            var eventFired = false;
            shim.ConsoleCancelEvent += (s, a) =>
            {
                eventFired = true;
            };

            obj.FireEvent();
            Assert.AreEqual(2, obj._EventCount);
            Assert.IsTrue(eventFired);
        }
    }
}
