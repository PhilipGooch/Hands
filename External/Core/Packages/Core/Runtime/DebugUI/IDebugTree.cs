using System.Collections.Generic;

namespace NBG.DebugUI
{
    public interface IDebugTree
    {
        IEnumerable<string> Categories { get; }

        IEnumerable<IDebugItem> GetItems(string category);
    }
}
