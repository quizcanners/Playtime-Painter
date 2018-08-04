using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Playtime_Painter;



public class BrushConfigOnly : MonoBehaviour  , IPEGI

{
        public BrushConfig brush = new BrushConfig();
#if !NO_PEGI
        public bool PEGI()
        {
            bool changed = false;

            changed |= brush.Targets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI().nl();
            changed |= brush.ColorSliders_PEGI();

            return changed;
        }

#endif
}
