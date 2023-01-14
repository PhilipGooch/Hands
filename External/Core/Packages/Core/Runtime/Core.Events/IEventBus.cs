using System;

namespace NBG.Core.Events
{
    public interface IEventBus
    {
        void Register<T>(Action<T> callback) where T : struct;
        void Unregister<T>(Action<T> callback) where T : struct;
        void Send<T>(T eventData) where T : struct;
    }
}
