using System.Collections.Generic;

namespace NBG.Core.DataMining
{
    // Data sources implement this to provide timing data to the timing graph
    public interface ITimingProvider
    {
        // A name for every data component
        IEnumerable<string> Names { get; }

        public interface IData
        {
            // Value for a data component, defined by the index into the Names list
            // This will be in milliseconds
            float GetValue(uint index);
        }
    }
}
