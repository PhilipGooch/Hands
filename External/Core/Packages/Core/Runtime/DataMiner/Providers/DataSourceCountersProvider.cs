using System.Collections.Generic;

namespace NBG.Core.DataMining
{
    // Data sources implement this to provide data to the counters graph
    public interface ICountersProvider
    {
        // A name for every data component
        IEnumerable<string> Names { get; }

        public interface IData
        {
            // Value for a data component, defined by the index into the Names list
            long GetValue(uint index);
        }
    }
}
