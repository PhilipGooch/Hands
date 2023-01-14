using NBG.Core.Events;
using NBG.Core.GameSystems;
using NUnit.Framework;

namespace NBG.Core.Tests
{
    class EventBusTests
    {
        GameSystemWorld World = null;
        IEventBus Bus = null;
        int eventValueInt;

        [SetUp]
        public void Setup()
        {
            EventBus.Create();
            
            World = new GameSystemWorld();
            
            Bus = EventBus.Get();
            Bus.Register<int>(HandleTestEvent);
        }

        [TearDown]
        public void TearDown()
        {
            Bus = null;
            
            World.Dispose();
            World = null;
            
            EventBus.Destroy();
        }

        void HandleTestEvent(int value)
        {
            eventValueInt = value;
        }

        [Test]
        public void EventBus_Works()
        {
            this.eventValueInt = 0;
            Bus.Send<int>(7);
            Assert.IsTrue(this.eventValueInt == 7);
        }
    }
}
