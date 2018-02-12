using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Painter {


    public static class MaterialTagsExtensions {

        public const string shaderPreferedPackagingSolution = "Solution";

        public static int getMeshProfileByTag(this Material mat) {

            var name = mat.GetTag(shaderPreferedPackagingSolution, false, "Standard");

			var prf = PainterConfig.inst.meshProfileSolutions;

			for(int i = 0; i<prf.Count; i++)// (var s in PainterConfig.inst.meshProfileSolutions)
				if (String.Compare(prf[i].name, name) == 0) return i;

			return 0;//PainterConfig.inst.meshProfileSolutions[0];
        }



        public static bool isAtlased(this Material mat) {
            if (mat == null) return false;
            return mat.shaderKeywords.Contains(PainterConfig.UV_ATLASED);
        }

        public static bool isProjected(this Material mat) {
            if (mat == null) return false;
            return mat.shaderKeywords.Contains(PainterConfig.UV_PROJECTED);
        }


    }

}