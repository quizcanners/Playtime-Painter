using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour, IPEGI , IGotIndex, IGotName {

        public static readonly Countless<LightCaster> AllProbes = new Countless<LightCaster>();
        private static int freeIndex;
        
        public Color ecol = Color.yellow;
        public float brightness = 1;

        public int index;

        public int IndexForPEGI { get { return index;  } set { index = value; } }
        public string NameForPEGI { get { return gameObject.name; } set { gameObject.name = value; } }

        private void OnEnable() {
            if (AllProbes[index]) {
                while (AllProbes[freeIndex]) freeIndex++;
                index = freeIndex;
            }

            AllProbes[index] = this;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = ecol;
            Gizmos.DrawWireSphere(transform.position, 1);
        }

        private void OnDisable() {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
        }

        private void ChangeIndexTo (int newIndex) {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
            index = newIndex;

            if (AllProbes[index])
                Debug.Log("More then one probe is sharing index {0}".F(index));

            AllProbes[index] = this;
        }

        #if PEGI
        public bool Inspect()
        {
            var changed = false;

            var tmp = index;
            if ("Index".edit(ref tmp).nl(ref changed)) 
                ChangeIndexTo(tmp);
            
            "Emission Color".edit(ref ecol).nl(ref changed);
            "Brightness".edit(ref brightness).nl(ref changed);

            if (changed) UnityUtils.RepaintViews();

            return changed;
        }
    #endif
       
    }
}