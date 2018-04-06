using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    [System.Serializable]
    public class PainterManagerPluginBase : MonoBehaviour {

        

        public virtual void OnEnable()  {

        }

        public virtual bool BrushConfigPEGI() {


            return false;
        }

        public virtual bool ConfigTab_PEGI() {

            return false;
        }

    }
}