using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

#if PEGI && UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : Editor
    {

        public override void OnInspectorGUI() => ((LightCaster)target).Inspect(serializedObject);
        
    }
#endif

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour
#if PEGI
        , IPEGI
#endif

    {

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

        void OnDrawGizmosSelected()
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

        #if PEGI
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

            if (changed) UnityHelperFunctions.RepaintViews();

            return changed;
        }
#endif
        // Update is called once per frame
        void Update()
        {

        }
    }
}