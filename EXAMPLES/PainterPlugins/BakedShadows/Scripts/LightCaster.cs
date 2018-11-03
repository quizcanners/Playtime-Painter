using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour, IPEGI , IGotIndex, IGotName {

        public static Countless<LightCaster> allProbes = new Countless<LightCaster>();
        public static int FreeIndex = 0;
        
        public Color ecol = Color.yellow;
        public float brightness = 1;

        public int index;

        public int IndexForPEGI { get { return index;  } set { index = value; } }
        public string NameForPEGI { get { return gameObject.name; } set { gameObject.name = value; } }

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
                Debug.Log("More then one probe is sharing index {0}".F(index));

            allProbes[index] = this;
        }

        #if PEGI
        public bool Inspect()
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
       
    }
}