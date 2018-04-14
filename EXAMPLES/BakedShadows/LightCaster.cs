using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((LightCaster)target).PEGI();
            ef.end();
        }
    }
#endif

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour {

        public static Countless<LightCaster> allProbes = new Countless<LightCaster>();
        public static int FreeIndex = 0;

    


        public Color ecol = Color.yellow;
        public float brightness = 1;

        public int index;

        public override string ToString() {
            return gameObject.name;
        }

        private void OnEnable() {
            if (allProbes[index] != null) {
                while (allProbes[FreeIndex] != null) FreeIndex++;
                index = FreeIndex;
            }

            allProbes[index] = this;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = ecol;
            Gizmos.DrawWireSphere(transform.position, 1);
        }

        private void OnDisable() {
            if (allProbes[index] == this)
                allProbes[index] = null;
        }

        void ChangeIndexTo (int newIndex) {
            if (allProbes[index] == this)
                allProbes[index] = null;
            index = newIndex;

            if (allProbes[index] != null)
                Debug.Log("More then one probe is sharing index "+index);

            allProbes[index] = this;
        }

        // Use this for initialization
        public bool PEGI()
        {
            bool changed = false;

            int tmp = index;
            if ("Index".edit(ref tmp).nl()) {
                changed = true;
                ChangeIndexTo(tmp);
            }

            changed |= "Emission Color".edit(ref ecol).nl();
            changed |= "Brightness".edit(ref brightness).nl();

            if (changed) pegi.RepaintViews();

            return changed;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}