#if ENABLE_DATA_MINER_EVENT_SOURCE // Not implemented yet
using System.Collections.Generic;

namespace NBG.Core.DataMining
{
    // Data sources implement this to provide data to the events view
    public interface IEventsProvider
    {
        public enum Type
        {
            Default,
            Scenes,
        }

        // A name for every data component
        IEnumerable<string> Names { get; }

        // A type for every data component
        IEnumerable<Type> Types { get; }

        public interface IData
        {
            // Value for a data component, defined by the index into the Names list
            string GetValue(uint index);
        }
    }
}
#endif //ENABLE_DATA_MINER_EVENT_SOURCE
