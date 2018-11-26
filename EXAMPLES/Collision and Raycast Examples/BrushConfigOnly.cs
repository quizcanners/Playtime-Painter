using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Playtime_Painter;

namespace Playtime_Painter.Examples
{

    public class BrushConfigOnly : MonoBehaviour, IPEGI

    {
        public BrushConfig brush = new BrushConfig();
#if PEGI
        public bool Inspect()
        {
            bool changed = false;

            changed |= brush.Targets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI().nl();
            changed |= brush.ColorSliders_PEGI();

            return changed;
        }

#endif
    }
}