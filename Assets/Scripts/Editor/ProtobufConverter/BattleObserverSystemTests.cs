using NUnit.Framework;

namespace Rogue.Editor.Tests
{
    public class BattleObserverSystemTests
    {
        [Test]
        public void Notify_WhenObserverDetachesDuringCallback_KeepsOtherObserversAndSkipsDetachedOnNextNotify()
        {
            BattleObserverSystem system = new BattleObserverSystem();
            TestBattleObserverParams observerParams = new TestBattleObserverParams(BattleExecuteTiming.BulletHit);

            CountingObserver first = new CountingObserver();
            DetachingObserver second = new DetachingObserver(system);

            system.Attach(BattleExecuteTiming.BulletHit, first);
            system.Attach(BattleExecuteTiming.BulletHit, second);

            system.Notify(BattleExecuteTiming.BulletHit, observerParams);

            Assert.AreEqual(1, first.NotifyCount);
            Assert.AreEqual(1, second.NotifyCount);

            system.Notify(BattleExecuteTiming.BulletHit, observerParams);

            Assert.AreEqual(2, first.NotifyCount);
            Assert.AreEqual(1, second.NotifyCount);
        }

        private sealed class CountingObserver : IBattleObserverHandle
        {
            public int NotifyCount { get; private set; }

            public void OnNotify(IBattleObserverParams param)
            {
                NotifyCount++;
            }
        }

        private sealed class DetachingObserver : IBattleObserverHandle
        {
            private readonly BattleObserverSystem _system;

            public DetachingObserver(BattleObserverSystem system)
            {
                _system = system;
            }

            public int NotifyCount { get; private set; }

            public void OnNotify(IBattleObserverParams param)
            {
                NotifyCount++;
                _system.Detach(param.BattleExecuteTiming, this);
            }
        }

        private sealed class TestBattleObserverParams : IBattleObserverParams
        {
            public TestBattleObserverParams(BattleExecuteTiming battleExecuteTiming)
            {
                BattleExecuteTiming = battleExecuteTiming;
            }

            public BattleExecuteTiming BattleExecuteTiming { get; }

            public void Clear()
            {
            }
        }
    }
}
