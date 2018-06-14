using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SharedTools_Stuff
{

#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(SelectionHolder))]
    public class SelectionHolderEditor : Editor
    {
        public override void OnInspectorGUI()
        {

            var trg = ((SelectionHolder)target);

            var sel = Selection.objects;

            for (int i = 0; i < trg.list.Count; i++)
            {

                EditorGUILayout.BeginHorizontal();

                var el = trg.list[i];

                if (GUILayout.Button("X"))
                {
                    trg.list.RemoveAt(i);
                    i--;
                }
                else
                {

                    el.name = EditorGUILayout.TextField(el.name);
                    if (GUILayout.Button("apply"))
                        Selection.objects = el.selection;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (sel.Length > 0 && GUILayout.Button("Save"))
            {
                var save = new SavedSelection();
                save.name = sel[0].name;
                save.selection = sel;
                trg.list.Add(save);
            }

        }
    }
#endif

    public class SelectionHolder : MonoBehaviour
    {
        public List<SavedSelection> list = new List<SavedSelection>();
    }

    [Serializable]
    public class SavedSelection
    {

        public string name;
        public UnityEngine.Object[] selection;

    }
}