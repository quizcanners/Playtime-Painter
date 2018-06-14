using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Playtime_Painter;




#if PEGI && UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(BrushConfigOnly))]
    public class BrushConfigOnlyEditor : Editor {
        public override void OnInspectorGUI() => ((BrushConfigOnly)target).inspect(serializedObject);
    }
#endif

public class BrushConfigOnly : MonoBehaviour
#if PEGI
    , iPEGI
#endif
{
        public BrushConfig brush = new BrushConfig();
#if PEGI
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
