using System;
using System.Threading;
using DNMPLibrary.Util;

namespace DNMPLibrary.Tests.Util
{
    using NUnit.Framework;
    using DNMPLibrary.Util;

    [TestFixture]
    public class EventQueueTest
    {
        [Test]
        public void Usual2Events() 
        {
            int val1 = -1, val2 = -2;
            EventQueue.AddEvent(x => val1 = 0, null, DateTime.Now.AddMilliseconds(200));
            EventQueue.AddEvent(x => val2 = 0, null, DateTime.Now.AddMilliseconds(400));
            Thread.Sleep(300);
            Assert.AreEqual(0, val1);
            Assert.AreEqual(-2, val2);
            Thread.Sleep(200);
            Assert.AreEqual(0, val1);
            Assert.AreEqual(0, val2);
        }

        [Test]
        public void UsualEventRemove()
        {
            var val = -1;
            var guid = EventQueue.AddEvent(x => val = 0, null, DateTime.Now.AddMilliseconds(200));
            Thread.Sleep(100);
            Assert.AreEqual(-1, val);
            EventQueue.RemoveEvent(guid);
            Thread.Sleep(150);
            Assert.AreEqual(-1, val);

        }

        [Test]
        public void ConcurrentRemoving()
        {
            var guid = EventQueue.AddEvent(x => { }, null, DateTime.Now.AddMilliseconds(100));
            Thread.Sleep(100);
            EventQueue.RemoveEvent(guid);
        }

        [Test]
        public void MultiplyAddRemove()
        {
            var guid = new Guid();
            var val = -1;
            EventQueue.AddEvent(x => val = 0, null, DateTime.Now.AddMilliseconds(50), guid);
            for (var i = 0; i < 100; i++)
            {
                Assert.AreEqual(-1, val);
                Thread.Sleep(25);
                EventQueue.RemoveEvent(guid);
                EventQueue.AddEvent(x => val = 0, null, DateTime.Now.AddMilliseconds(50), guid);
            }
            Thread.Sleep(100);
            Assert.AreEqual(0, val);
        }

        [Test]
        public void EqualEvents()
        {
            var val1 = -1;
            var val2 = -2;
            var calltime = DateTime.Now.AddMilliseconds(200);
            EventQueue.AddEvent(x => val1 = 0, null, calltime);
            EventQueue.AddEvent(x => val1 = 0, null, calltime);
            EventQueue.AddEvent(x => val2 = 0, null, calltime);
            Assert.AreEqual(-2, val2);
            Assert.AreEqual(-1, val1);
            Thread.Sleep(calltime.AddMilliseconds(50) - DateTime.Now);
            Assert.AreEqual(0, val2);
            Assert.AreEqual(0, val1);
        }

        [Test]
        public void ParameterPass()
        {
            var val = 0;
            EventQueue.AddEvent(x => val = (int) x, 42, DateTime.Now.AddMilliseconds(50));
            Thread.Sleep(100);
            Assert.AreEqual(42, val);
        }
    }
}
