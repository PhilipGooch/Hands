using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Undo
{
    internal class UndoStep 
    {
        internal List<IUndoState> states;

        internal UndoStep()
        {
            states = new List<IUndoState>();
        }

        internal void Add(IUndoState state)
        {
            states.Add(state);
        }
    }
}
