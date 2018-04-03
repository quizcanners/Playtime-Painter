using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Playtime_Painter;




#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(BrushConfigOnly))]
    public class BrushConfigOnlyEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
        ((BrushConfigOnly)target).PEGI();
        ef.end();
    }
    }
#endif

    public class BrushConfigOnly : MonoBehaviour  {
        public BrushConfig brush = new BrushConfig();

        public bool PEGI()
        {
            bool changed = false;

            changed |= brush.Targets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI().nl();
            changed |= brush.ColorSliders_PEGI();

            return changed;
        }
    }
