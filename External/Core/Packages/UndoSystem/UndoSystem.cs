using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Undo
{
    public class UndoSystem
    {
        private readonly List<UndoStep> steps;
        private readonly List<IUndoStateCollector> stateCollectors;
        private readonly int undoMaxSteps;

        private int undoIndex;

        private bool started = false;

        private UndoDummy dummy;
        public UndoSystem(int undoMaxSteps)
        {
            this.undoMaxSteps = undoMaxSteps;
            steps = new List<UndoStep>(undoMaxSteps);
            stateCollectors = new List<IUndoStateCollector>();
        }

        public void StartSystem(params IUndoStateCollector[] collectorParams)
        {

#if UNITY_EDITOR
            GetDummy();
#endif
            started = true;
            for (int i = 0; i < collectorParams.Length; i++)
                stateCollectors.Add(collectorParams[i]);
            RecordUndo();
        }

        public void RecordUndo()
        {

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(dummy, "dummy");
            dummy.dummyValue = !dummy.dummyValue;
#endif
            if (!started)
            {
                UnityEngine.Debug.LogError("You've to use StartSystem before using undo.");
                return;
            }

            if (steps.Count > 0 && undoIndex != (steps.Count - 1))
            {
                for (int i = steps.Count - 1; i > undoIndex; i--)
                {
                    steps.RemoveAt(i);
                }
            }

            if (steps.Count >= undoMaxSteps)
                steps.RemoveAt(0);

            steps.Add(CreateUndoStep());
            undoIndex = steps.Count - 1;
        }

        public void OverwriteUndo()
        {
            if (undoIndex >= 0)
            {
                undoIndex--;
                RecordUndo();
            }
        }

        internal UndoStep CreateUndoStep()
        {
            UndoStep step = new UndoStep();
            for (int i = 0; i < stateCollectors.Count; i++)
                step.Add(stateCollectors[i].RecordUndoState());
            return step;
        }

        public void Undo()
        {
            undoIndex--;

            if (undoIndex >= 0)
                GoToStep(steps[undoIndex]);
            else
                undoIndex++;
        }

        public void Redo()
        {
            undoIndex++;

            if (undoIndex < steps.Count)
                GoToStep(steps[undoIndex]);
            else
                undoIndex--;
        }

        internal void GoToStep(UndoStep step)
        {
            for (int i = 0; i < step.states.Count; i++)
            {
                step.states[i].Undo();
            }
        }

#if UNITY_EDITOR
        private void GetDummy()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(UndoDummy).Name);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                dummy = AssetDatabase.LoadAssetAtPath<UndoDummy>(path);
            }
        }
#endif
    }
}
