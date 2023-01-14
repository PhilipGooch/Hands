using NBG.Core.Streams;

namespace NBG.Core.Events
{
    public interface IEventSerializer<T>
    {
        void Serialize(IStreamWriter writer, T data);
        T Deserialize(IStreamReader reader);
    }
}
