using NUnit.Framework;
using Util;

namespace Tests.EditMode
{
    public class TimerTests
    {
        [Test]
        public void Timer_StartsNotRunning()
        {
            var timer = new Timer();
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void Timer_StartMakesItRun()
        {
            var timer = new Timer();
            timer.Start(1f);
            Assert.IsTrue(timer.IsRunning);
        }

        [Test]
        public void Timer_StartSetsDuration()
        {
            var timer = new Timer();
            timer.Start(2.5f);
            Assert.AreEqual(2.5f, timer.Duration);
            Assert.AreEqual(2.5f, timer.TimeRemaining);
        }

        [Test]
        public void Timer_TickCountsDown()
        {
            var timer = new Timer();
            timer.Start(1f);
            timer.Tick(0.3f);
            Assert.AreEqual(0.7f, timer.TimeRemaining, 0.001f);
            Assert.IsTrue(timer.IsRunning);
        }

        [Test]
        public void Timer_StopsWhenExpired()
        {
            var timer = new Timer();
            timer.Start(1f);
            timer.Tick(1f);
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.TimeRemaining);
        }

        [Test]
        public void Timer_StopsWhenOvershot()
        {
            var timer = new Timer();
            timer.Start(0.5f);
            timer.Tick(1f);
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.TimeRemaining);
        }

        [Test]
        public void Timer_StopForceStops()
        {
            var timer = new Timer();
            timer.Start(5f);
            timer.Stop();
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.TimeRemaining);
        }

        [Test]
        public void Timer_TickDoesNothingWhenStopped()
        {
            var timer = new Timer();
            timer.Tick(1f);
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.TimeRemaining);
        }

        [Test]
        public void Timer_CanBeRestarted()
        {
            var timer = new Timer();
            timer.Start(1f);
            timer.Tick(1f);
            Assert.IsFalse(timer.IsRunning);

            timer.Start(2f);
            Assert.IsTrue(timer.IsRunning);
            Assert.AreEqual(2f, timer.Duration);
            Assert.AreEqual(2f, timer.TimeRemaining);
        }
    }
}
