using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Undo
{
    public interface IUndoState 
    {
        void Undo();
    }
}
