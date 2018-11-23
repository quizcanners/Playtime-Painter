using PlayerAndEditorGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SharedTools_Stuff
{

    public static class CustomShaderParameters   {

        public const string imageProjectionPosition = "_imgProjPos";
        public const string nextTexture = "_Next_MainTex";
        public const string currentTexture = "_MainTex_Current";
        public const string transitionPortion = "_Transition";


#if PEGI
        public static void Inspect() {
            "Image projection position".write_ForCopy(imageProjectionPosition); pegi.nl();

            "Next Texture".write_ForCopy(nextTexture); pegi.nl();

            "Transition portion".write_ForCopy(transitionPortion); pegi.nl();
        }
#endif
    }
}
