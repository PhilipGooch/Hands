using System.Collections.Generic;

namespace NBG.Core.DataMining
{
    // Data sources implement this to provide response to a frame being selected in the viewer
    public interface IFrameSelectHandler
    {
        // Name to show in the UI
        string HandlerName { get; }

        // Use the last known state when selecting a frame with no data
        bool UsePreviousState { get; }

        // <frameNo> is the selected frame
        // <dataFrameNo> is the frame for which this specific set of data was retrieved.
        //               It might be older when <UsePreviousState> is true.
        void OnFrameSelect(uint frameNo, uint dataFrameNo, IDataBlob blob);

        // <frameNo> is the selected frame
        // Called when no frame data could be retrieved:
        //     If <UsePreviousState> is false, and current frame has no corresponding data.
        //     If <UsePreviousState> is true, but nothing was recorded prior to the current frame.
        void OnFrameSelect(uint frameNo);

        // Called when the select handler gets disabled, or when the UI is closed.
        void OnReset();
    }
}
