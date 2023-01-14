using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.DataMining
{
    public class DatasetCounters : Dataset
    {
        public long min;
        public uint minAt;
        public long max;
        public uint maxAt;

        public float avg = 0;

        public ColumnCounters[] columns;

        public string name;
        public DatasetCounters(Dictionary<uint, IDataBlob> values, int remainder, int from, int to, int count, int countPerColumn, uint elementID, string name, uint lastFrameAt)
        {
            this.name = name;

            framesFrom = from;
            framesTo = to;

            min = long.MaxValue;
            max = long.MinValue;
            dataPointsCount = count;

            columns = new ColumnCounters[count];
            int id = from;

            for (int i = 0; i < count; i++)
            {
                int tempTo = id + countPerColumn + Mathf.Min(remainder, 1);

                ColumnCounters column = new ColumnCounters(values, elementID, id, tempTo, lastFrameAt);

                if (column.uniqueframeCount == 0)
                {
                    if (i != 0)
                    {
                        column = new ColumnCounters(columns[i - 1]);
                    }
                    else
                    {
                        column.ZeroValue();
                    }
                }
                else
                {
                    if (column.max > max)
                        max = column.max;

                    if (column.min < min)
                        min = column.min;

                    avg += column.avg;
                }

                columns[i] = column;

                if (remainder > 0)
                    remainder--;

                id = tempTo;
            }
            avg /= count;
        }
    }
    public class ColumnCounters : Column
    {
        public long min;
        public uint minAt;

        public long max;
        public uint maxAt;

        public long avg;

        public bool withinRecordingRange;
        public ColumnCounters(Dictionary<uint, IDataBlob> values, uint elem, int from, int to, uint lastFrameAt)
        {
            min = long.MaxValue;
            max = long.MinValue;
            avg = 0;

            frameCount = to - from;
            framesFrom = from;
            framesTo = to;

            uniqueframeCount = 0;

            firstUniqueFrame = -1;
            lastUniqueFrame = -1;

            withinRecordingRange = from <= lastFrameAt;

            if (!withinRecordingRange)
                return;

            for (int i = from; i < to; i++)
            {
                uint frameNo = (uint)i;
                if (values.ContainsKey(frameNo))
                {
                    uniqueframeCount++;

                    if (firstUniqueFrame == -1)
                    {
                        firstUniqueFrame = i;

                    }
                    lastUniqueFrame = i;

                    ICountersProvider.IData countersData = (ICountersProvider.IData)values[frameNo];
                    long valueToUse = countersData.GetValue(elem);

                    if (valueToUse > max)
                    {
                        max = valueToUse;
                        maxAt = frameNo;
                    }
                    if (valueToUse < min)
                    {
                        min = valueToUse;
                        minAt = frameNo;
                    }
                    avg += valueToUse;
                }
            }

            avg = uniqueframeCount > 0 ? avg / uniqueframeCount : 0;
        }

        //copy data from another, unique frames info not included
        public ColumnCounters(ColumnCounters other)
        {
            min = other.min;
            minAt = other.minAt;

            max = other.max;
            maxAt = other.maxAt;

            avg = other.avg;

            frameCount = other.frameCount;
            framesFrom = other.framesFrom;
            framesTo = other.framesTo;

            uniqueframeCount = 0;
            firstUniqueFrame = -1;
            lastUniqueFrame = -1;

        }
        public void ZeroValue()
        {
            min = 0;
            max = 0;
            avg = 0;
            uniqueframeCount = 0;
            firstUniqueFrame = -1;
            lastUniqueFrame = -1;
        }
    }
}
