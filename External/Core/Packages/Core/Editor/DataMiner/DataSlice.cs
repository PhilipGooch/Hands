using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NBG.Core.Editor;

namespace NBG.Core.DataMining
{
    public struct DataID
    {
        public IDataSource dataSource;
        public int dataSourceId;
        public uint index;
        public string name;
        public DataID(IDataSource dataSource, int dataSourceId, uint index, string name)
        {
            this.dataSource = dataSource;
            this.dataSourceId = dataSourceId;
            this.index = index;
            this.name = name;
        }
    }

    public class Slice
    {
        DataReader reader;

        public Dictionary<DataID, DatasetTimer> timerData;
        public Dictionary<DataID, DatasetCounters> counterData;
        public Dictionary<IFrameSelectHandler, string> selectedSelectionHandlers;
        public Dictionary<IFrameSelectHandler, string> allSelectionHandlers;

        IEnumerable<IDataSource> timingProviders;
        IEnumerable<IDataSource> counterProviders;
        IEnumerable<IDataSource> sourcesWithSelectionHandlers;

        public int dataPointsCount;
        public int framesFrom;
        public int framesTo;
        public Slice(DataReader reader, int from, int to, int count)
        {
            this.reader = reader;

            sourcesWithSelectionHandlers = reader.Sources.Where(s => s as IFrameSelectHandler != null);
            timingProviders = reader.Sources.Where(s => s as ITimingProvider != null);
            counterProviders = reader.Sources.Where(s => s as ICountersProvider != null);

            allSelectionHandlers = new Dictionary<IFrameSelectHandler, string>();
            selectedSelectionHandlers = new Dictionary<IFrameSelectHandler, string>();
            timerData = new Dictionary<DataID, DatasetTimer>();
            counterData = new Dictionary<DataID, DatasetCounters>();

            dataPointsCount = count;
            framesFrom = from;
            framesTo = to;

            int frameCount = to - from;
            int countPerColumn = frameCount / count;
            int remainder = frameCount % count;

            // Timing
            foreach (var cs in timingProviders)
            {
                var provider = cs as ITimingProvider;
                var values = reader.GetFrameUniqueValuesForDataSource(cs.Id);
                var lastFrameAt = reader.LastFrameNo;
                var providerElements = provider.Names.Count();
                uint elementID = 0;

                foreach (var item in provider.Names)
                {
                    DataID dataID = new DataID(cs, cs.Id, elementID, item);
                    DatasetTimer dataset = new DatasetTimer(values, remainder, from, to, count, countPerColumn, elementID, item, lastFrameAt);
                    timerData.Add(dataID, dataset);
                    elementID++;
                }

            }

            // Counters
            foreach (var cs in counterProviders)
            {
                var provider = cs as ICountersProvider;
                var values = reader.GetFrameUniqueValuesForDataSource(cs.Id);
                var lastFrameAt = reader.LastFrameNo;
                var providerElements = provider.Names.Count();
                uint elementID = 0;

                foreach (var item in provider.Names)
                {
                    DataID dataID = new DataID(cs, cs.Id, elementID, item);
                    DatasetCounters dataset = new DatasetCounters(values, remainder, from, to, count, countPerColumn, elementID, item, lastFrameAt);
                    counterData.Add(dataID, dataset);
                    elementID++;
                }
            }

            foreach (var item in sourcesWithSelectionHandlers)
            {
                IFrameSelectHandler selectionHandler = item as IFrameSelectHandler;
                allSelectionHandlers.Add(selectionHandler, selectionHandler.HandlerName);
                selectedSelectionHandlers.Add(selectionHandler, selectionHandler.HandlerName);
            }
        }

        public Column GetColumnFromDataID(DataID dataID, int columnID)
        {
            if (timerData.ContainsKey(dataID))
                if (timerData[dataID].dataPointsCount > columnID)
                    return timerData[dataID].columns[columnID];


            if (counterData.ContainsKey(dataID))
                if (counterData[dataID].dataPointsCount > columnID)
                    return counterData[dataID].columns[columnID];

            return null;
        }

        public int GetFirstUniqueFrameInCountersColumn(int columnID)
        {
            int currUniqFrame = -1;
            foreach (var item in counterData)
            {
                int uniqueFrame = item.Value.columns[columnID].firstUniqueFrame;

                if (uniqueFrame != -1 && currUniqFrame < uniqueFrame)
                {
                    currUniqFrame = uniqueFrame;

                }

            }

            if (currUniqFrame != -1)
                return currUniqFrame;
            else
            {
                foreach (var item in counterData)
                {
                    for (int i = columnID; i > 0; i--)
                    {
                        int uniqueFrame = item.Value.columns[columnID].lastUniqueFrame;

                        if (uniqueFrame != -1)
                            return uniqueFrame;

                    }
                }
            }
            return -1;
        }

        public void SelectFrame(uint frameNo)
        {
            foreach (var cs in sourcesWithSelectionHandlers)
            {
                // Check if this source is selected
                IFrameSelectHandler selectionHandler = cs as IFrameSelectHandler;
                if (!selectedSelectionHandlers.ContainsKey(selectionHandler))
                    continue;

                var values = reader.GetFrameUniqueValuesForDataSource(cs.Id);

                // Find a suitable data frame to use
                uint dataFrameNo = frameNo;
                bool found = false;
                IDataBlob value;
                if (values.TryGetValue(dataFrameNo, out value))
                {
                    found = true;
                }
                else if (selectionHandler.UsePreviousState)
                {
                    while (dataFrameNo > 0)
                    {
                        dataFrameNo--;
                        if (values.TryGetValue(dataFrameNo, out value)) //TODO: could potentially be optimized
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    selectionHandler.OnFrameSelect(frameNo, dataFrameNo, value);
                }
                else
                {
                    selectionHandler.OnFrameSelect(frameNo);
                }
            }
        }

        public void ResetSelectionHandlers()
        {
            foreach (var cs in sourcesWithSelectionHandlers)
            {
                // Check if this source is selected
                IFrameSelectHandler selectionHandler = cs as IFrameSelectHandler;
                if (!selectedSelectionHandlers.ContainsKey(selectionHandler))
                    continue;

                selectionHandler.OnReset();
            }
        }
    }

    public abstract class Dataset
    {
        public int frameCount;
        public int framesFrom;
        public int framesTo;
        public int dataPointsCount;
    }

    public abstract class Column
    {
        public int frameCount;
        public int framesFrom;
        public int framesTo;

        public int uniqueframeCount;
        public int firstUniqueFrame;
        public int lastUniqueFrame;
    }


}
