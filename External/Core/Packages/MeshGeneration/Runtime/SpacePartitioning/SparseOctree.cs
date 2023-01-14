using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NBG.MeshGeneration
{
    public struct SparseOctree : IDisposable
    {
        internal struct OctreeUnit
        {
            public int childPageIndex;
            public byte validMask;
            public byte leafMask;
            public static OctreeUnit New()
            {
                return new OctreeUnit
                {
                    childPageIndex = INVALID_INDEX,
                    validMask = 0,
                    leafMask = 0
                };
            }
        }
        internal struct Root
        {
            public int index;
            public float3 pos;
        }
        private struct StackOperation
        {
            public int shift;
            public int index;
        }

        private int levels;
        private float side;

        internal int octreeUsage;

        internal NativeList<OctreeUnit> units;
        internal NativeList<Root> roots;
        private NativeList<int> removedPages;

        private NativeArray<StackOperation> stack;

        private UnityEngine.Color[] debugColors;

        private const int pageSize = 8;
        private const int INVALID_INDEX = -1;
        private const byte maxMask = 255;
        public SparseOctree(int levels, float side)
        {
            this.levels = levels;
            this.side = side;

            octreeUsage = 0;

            const int defaultListSize = 1024;

            units = new NativeList<OctreeUnit>(defaultListSize, Allocator.Persistent);
            removedPages = new NativeList<int>(defaultListSize, Allocator.Persistent);
            roots = new NativeList<Root>(defaultListSize, Allocator.Persistent);
            stack = new NativeArray<StackOperation>(levels, Allocator.Persistent);

            debugColors = new UnityEngine.Color[levels];
            for (int i = 0; i < levels; i++)
                debugColors[i] = UnityEngine.Color.HSVToRGB(UnityEngine.Random.Range(0.0f, 1.0f), 1.0f, 0.7f);
        }
        public void Add(float3 pos)
        {
            int index = INVALID_INDEX;

            for (int i = 0; i < roots.Length; i++)
            {
                if (Intersects(roots[i].pos, ref pos, side))
                {
                    index = roots[i].index;
                    break;
                }
            }

            if (index == INVALID_INDEX)
            {
                index = CreateRoot(pos);
            }

            float levelSide = side;
            float halfLevelSide = side / 2;

            const int zero = 0;
            const int xMask = 1;
            const int yMask = 2;
            const int zMask = 4;

            int lastLevel = levels - 1;

            bool rebuild = false;

            for (int i = 0; i < levels; i++)
            {
                var unit = units[index];
                float3 unitPos = math.frac(pos / levelSide) * levelSide;

                int positionShift = (
                    (unitPos.x >= halfLevelSide ? xMask : zero) |
                    (unitPos.y >= halfLevelSide ? yMask : zero) |
                    (unitPos.z >= halfLevelSide ? zMask : zero)
                    );

                byte positionMask = (byte)(
                    1 << positionShift
                );

                stack[i] = new StackOperation { index = index, shift = positionShift };

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                if (isValid)
                {
                    if (isLeaf)
                        return;
                    else
                        index = unit.childPageIndex + positionShift;
                }
                else
                {
                    unit.validMask |= positionMask;

                    if (i == lastLevel)
                    {
                        unit.leafMask |= positionMask;
                        rebuild = unit.leafMask == maxMask;
                    }
                    else
                    {
                        if (unit.childPageIndex == INVALID_INDEX)
                            unit.childPageIndex = CreatePage();
                    }

                    units[index] = unit;

                    index = unit.childPageIndex + positionShift;
                }

                levelSide = halfLevelSide;
                halfLevelSide /= 2.0f;
            }

            if (rebuild)
                RebuildLeavesAdd();
        }

        public void Sub(float3 pos)
        {
            int index = INVALID_INDEX;

            for (int i = 0; i < roots.Length; i++)
            {
                if (Intersects(roots[i].pos, ref pos, side))
                {
                    index = roots[i].index;
                    break;
                }
            }

            if (index == INVALID_INDEX)
            {
                index = CreateRoot(pos);
            }

            float levelSide = side;
            float halfLevelSide = side / 2;

            const int zero = 0;
            const int xMask = 1;
            const int yMask = 2;
            const int zMask = 4;

            int lastLevel = levels - 1;

            bool rebuild = false;

            for (int i = 0; i < levels; i++)
            {
                var unit = units[index];
                float3 unitPos = math.frac(pos / levelSide) * levelSide;

                int positionShift = (
                    (unitPos.x >= halfLevelSide ? xMask : zero) |
                    (unitPos.y >= halfLevelSide ? yMask : zero) |
                    (unitPos.z >= halfLevelSide ? zMask : zero)
                    );

                byte positionMask = (byte)(1 << positionShift);
                byte invertedPositionMask = (byte)~positionMask;

                stack[i] = new StackOperation { index = index, shift = positionShift };

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                if (isValid)
                {
                    if (isLeaf)
                    {
                        if (i == lastLevel)
                        {
                            unit.leafMask &= invertedPositionMask;
                            unit.validMask &= invertedPositionMask;
                            rebuild = unit.leafMask == 0;
                        }
                        else
                        {
                            unit.leafMask &= invertedPositionMask;
                            if (unit.childPageIndex == INVALID_INDEX)
                                unit.childPageIndex = CreatePage();

                            int subIndex = unit.childPageIndex + positionShift;
                            var subUnit = units[subIndex];
                            subUnit.leafMask = maxMask;
                            subUnit.validMask = maxMask;
                            subUnit.childPageIndex = INVALID_INDEX;
                            units[subIndex] = subUnit;
                        }
                    }

                    units[index] = unit;

                    index = unit.childPageIndex + positionShift;
                }
                else
                {
                    return;
                }

                levelSide = halfLevelSide;
                halfLevelSide /= 2.0f;
            }

            if (rebuild)
                RebuildLeavesSub();
        }
        public bool Get(float3 pos)
        {
            int index = INVALID_INDEX;

            for (int i = 0; i < roots.Length; i++)
            {
                if (Intersects(roots[i].pos, ref pos, side))
                {
                    index = roots[i].index;
                    break;
                }
            }

            if (index == INVALID_INDEX)
            {
                return false;
            }

            float levelSide = side;
            float halfLevelSide = side / 2;

            const int zero = 0;
            const int xMask = 1;
            const int yMask = 2;
            const int zMask = 4;

            for (int i = 0; i < levels; i++)
            {
                var unit = units[index];
                float3 unitPos = math.frac(pos / levelSide) * levelSide;

                int positionShift = (
                    (unitPos.x >= halfLevelSide ? xMask : zero) |
                    (unitPos.y >= halfLevelSide ? yMask : zero) |
                    (unitPos.z >= halfLevelSide ? zMask : zero)
                    );

                byte positionMask = (byte)(
                    1 << positionShift
                );

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                if (isValid)
                {
                    if (isLeaf)
                        return true;
                }
                else
                {
                    return false;
                }

                index = unit.childPageIndex + positionShift;
                levelSide = halfLevelSide;
                halfLevelSide /= 2.0f;
            }

            return false;
        }
        private void RebuildLeavesAdd()
        {
            bool removeNext = false;
            for (int i = levels - 1; i >= 0; i--)
            {
                var operation = stack[i];
                var unit = units[operation.index];
                if (removeNext)
                {
                    byte mask = (byte)(1 << operation.shift);
                    unit.leafMask |= mask;

                    if (unit.leafMask == maxMask)
                    {
                        RemovePage(unit.childPageIndex);
                        unit.childPageIndex = INVALID_INDEX;
                        units[operation.index] = unit;
                    }
                    else
                    {
                        units[operation.index] = unit;
                        return;
                    }
                }
                else
                {
                    if (unit.leafMask == maxMask)
                    {
                        removeNext = true;
                    }
                }
            }
        }

        private void RebuildLeavesSub()
        {
            bool removeNext = false;
            for (int i = levels - 1; i >= 0; i--)
            {
                var operation = stack[i];
                var unit = units[operation.index];

                if (removeNext)
                {
                    byte inverted = (byte)~(1 << operation.shift);
                    unit.leafMask &= inverted;
                    unit.validMask &= inverted;
                    units[operation.index] = unit;
                    return;
                }
                else
                {
                    if (unit.leafMask == 0)
                    {
                        removeNext = true;
                    }
                }
            }
        }
        private int CreateRoot(float3 worldPoint)
        {
            float3 rootOrigin = math.floor(worldPoint / side) * side;
            int newUnitIndex = CreateRootOctreeUnit();

            roots.Add(
            new Root
                {
                    index = newUnitIndex,
                    pos = rootOrigin
                }
            );

            return newUnitIndex;
        }
        private int CreateRootOctreeUnit()
        {
            units.Add(OctreeUnit.New());
            octreeUsage++;
            return units.Length-1;
        }
        private int CreatePage()
        {
            int pageIndex;
            if (removedPages.Length > 0)
            {
                pageIndex = removedPages[removedPages.Length - 1];
                removedPages.RemoveAt(removedPages.Length - 1);
                for (int i = 0; i < pageSize; i++)
                    units[pageIndex + i] = OctreeUnit.New();
            }
            else
            {
                pageIndex = units.Length;
                for (int i = 0; i < pageSize; i++)
                    units.Add(OctreeUnit.New());
            }

            octreeUsage += pageSize;

            return pageIndex;
        }
        private void RemovePage(int pageIndex)
        {
            removedPages.Add(pageIndex);
            octreeUsage -= pageSize;
        }

        /// <summary>
        /// Removes the octree space by using index and mask. You must call CleanEmptyPaths after operations.
        /// </summary>
        /// <param name="index">The unit index</param>
        /// <param name="mask">The mask of the section you want to remove</param>
        public void RemoveByIndexAndMask(int index, byte mask)
        {
            //Must
            var unit = units[index];
            byte invertedMask = (byte)~mask;
            bool isValid = (mask & unit.validMask) > 0;

            if (isValid)
            {
                unit.validMask &= invertedMask;
                unit.leafMask &= invertedMask;
                units[index] = unit;
            }
        }
        public void CleanEmptyPaths()
        {
            for (int i = 0; i < roots.Length; i++)
            {
                int index = roots[i].index;
                RemoveEmptyPathsRecursively(index, false);
            }
        }

        public void AddWithPositionAndSize(int4 posSize)
        {
            int depth = levels - (int)math.log2((float)posSize.w);
            int3 pos = posSize.xyz;
            float3 floatPos = (float3)pos;

            int index = INVALID_INDEX;

            for (int i = 0; i < roots.Length; i++)
            {
                if (Intersects(roots[i].pos, ref floatPos, side))
                {
                    index = roots[i].index;
                    break;
                }
            }

            if (index == INVALID_INDEX)
            {
                index = CreateRoot(floatPos);
            }

            float levelSide = side;
            float halfLevelSide = side / 2;

            const int zero = 0;
            const int xMask = 1;
            const int yMask = 2;
            const int zMask = 4;

            int lastLevel = depth - 1;

            for (int i = 0; i < depth; i++)
            {
                var unit = units[index];
                float3 unitPos = math.frac(floatPos / levelSide) * levelSide;

                int positionShift = (
                    (unitPos.x >= halfLevelSide ? xMask : zero) |
                    (unitPos.y >= halfLevelSide ? yMask : zero) |
                    (unitPos.z >= halfLevelSide ? zMask : zero)
                    );

                byte positionMask = (byte)(
                    1 << positionShift
                );

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                if (isValid)
                {
                    if (isLeaf)
                        return;
                    else
                        index = unit.childPageIndex + positionShift;
                }
                else
                {
                    unit.validMask |= positionMask;

                    if (i == lastLevel)
                    {
                        unit.leafMask |= positionMask;
                    }
                    else
                    {
                        if (unit.childPageIndex == INVALID_INDEX)
                            unit.childPageIndex = CreatePage();
                    }

                    units[index] = unit;

                    index = unit.childPageIndex + positionShift;
                }

                levelSide = halfLevelSide;
                halfLevelSide /= 2.0f;
            }
        }
        private void RemoveEmptyPathsRecursively(int unitIndex, bool remove)
        {
            var unit = units[unitIndex];
            bool isEmpty = unit.validMask == 0;
            bool isIndexValid = unit.childPageIndex != INVALID_INDEX;
            bool removable = isEmpty && isIndexValid;
            remove = remove || removable;

            for (int i = 0; i < 8; i++)
            {
                byte positionMask = (byte)(1 << i);
                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;
                bool navigable = isValid && !isLeaf;
                if (navigable || removable)
                    RemoveEmptyPathsRecursively(unit.childPageIndex + i, remove);
            }

            if (remove)
            {
                if (isIndexValid)
                {
                    RemovePage(unit.childPageIndex);
                    unit.childPageIndex = INVALID_INDEX;
                    units[unitIndex] = unit;
                }
            }
        }

        public static bool Intersects(float3 origin, ref float3 pos, float side)
        {
            return pos.x >= origin.x && pos.x < (origin.x + side) &&
                   pos.y >= origin.y && pos.y < (origin.y + side) &&
                   pos.z >= origin.z && pos.z < (origin.z + side);
        }
        public float GetMinimumUnitSize()
        {
            return side / math.pow(2.0f, levels);
        }

        internal static bool showPositions = false;
        internal void DrawGizmos()
        {
            for (int j = 0; j < roots.Length; j++)
            {
                DrawOctreeGizmos(roots[j].index, roots[j].pos, side, 0);
            }
        }
        public void DrawOctreeGizmos(int index, float3 pos, float currentSide, int level)
        {
            const int xMask = 1;
            const int yMask = 2;
            const int zMask = 4;

            float halfLevelSide = currentSide / 2.0f;

            var unit = units[index];

            for (int i = 0; i < 8; i++)
            {
                byte positionMask = (byte)(
                    1 << i
                );

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                float3 offset = new float3(
                    (i & xMask) > 0 ? halfLevelSide : 0.0f,
                    (i & yMask) > 0 ? halfLevelSide : 0.0f,
                    (i & zMask) > 0 ? halfLevelSide : 0.0f
                );

                if (isValid)
                {
                    if (isLeaf)
                    {
                        UnityEngine.Gizmos.color = debugColors[level];
                        UnityEngine.Gizmos.DrawWireCube(pos + offset + halfLevelSide * 0.5f, halfLevelSide * UnityEngine.Vector3.one);
#if UNITY_EDITOR
                        if (showPositions)
                            UnityEditor.Handles.Label(pos + offset + halfLevelSide * 0.5f, "" + (pos + offset));
#endif
                    }
                    else
                    {
                        DrawOctreeGizmos(unit.childPageIndex + i, pos + offset, halfLevelSide, level + 1);
                    }
                }
            }
        }

        public int GetLevels()
        {
            return levels;
        }

        public float GetSide()
        {
            return side;
        }

        public int CalculateLeafCount()
        {
            int leafCount = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                int rootIndex = root.index;

                WriteBoxes(rootIndex, ref leafCount);
            }

            return leafCount;
        }
        private void WriteBoxes(int unitIndex, ref int leafCount)
        {
            var unit = units[unitIndex];

            for (int i = 0; i < 8; i++)
            {
                byte positionMask = (byte)(
                    1 << i
                );

                bool isValid = (positionMask & unit.validMask) > 0;
                bool isLeaf = (positionMask & unit.leafMask) > 0;

                if (isValid)
                {
                    if (isLeaf)
                    {
                        leafCount++;
                    }
                    else
                    {
                        WriteBoxes(unit.childPageIndex + i, ref leafCount);
                    }
                }
            }
        }
        public void Dispose()
        {
            units.Dispose();
            roots.Dispose();
            stack.Dispose();
            removedPages.Dispose();
        }
    }
}
