using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Playtime_Painter
{

    [System.Serializable]
    public class PainterManagerPluginBase : MonoBehaviour {

        public virtual void OnEnable()  {

        }

       
        public virtual bool ConfigTab_PEGI() {
            return false;
        }

    }
}