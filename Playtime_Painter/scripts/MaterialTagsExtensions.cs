using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Painter {

    public enum ShaderUV_Method { Normal, Atlased, Projected, AtlasedProjected }

    public static class MaterialTagsExtensions {

        // Shader Tags
        public const string shaderUVtype = "UVtype";
        public const string shaderUV_Normal = "Normal";
        public const string shaderUV_Atlas = "Atlas";
        public const string shaderUV_Projected = "Projected";
        public const string shaderUV_AtlasedProjected = "Atlas Projected";

        public const string shaderPreferedPackagingSolution = "Solution";


        public static MeshSolutionProfile getSolutionByTag(this Material mat) {

            var name = mat.GetTag(shaderPreferedPackagingSolution, false, "Standard");

            foreach (var s in PainterConfig.inst.meshProfileSolutions)
                if (String.Compare(s.name, name) == 0) return s;

            return PainterConfig.inst.meshProfileSolutions[0];
        }

        public static ShaderUV_Method getUVmethod (this Material mat) {
           
            switch (mat.GetTag(shaderUVtype, false, shaderUV_Normal)) {

                case shaderUV_Atlas: return ShaderUV_Method.Atlased;
                case shaderUV_Projected: return ShaderUV_Method.Projected;
                case shaderUV_AtlasedProjected: return ShaderUV_Method.AtlasedProjected;

                default: return ShaderUV_Method.Normal;
            }
        }

        public static bool isAtlased(Material mat) {
            var method = mat.getUVmethod();
            return (method == ShaderUV_Method.Atlased) || (method == ShaderUV_Method.AtlasedProjected);
        }

        public static bool isProjected(Material mat) {
            var method = mat.getUVmethod();
            return (method == ShaderUV_Method.Projected) || (method == ShaderUV_Method.AtlasedProjected);
        }


    }

}