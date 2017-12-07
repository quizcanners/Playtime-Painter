using UnityEngine;
using UnityEditor;
using System.Collections;
using PlayerAndEditorGUI;

//public class ConditionEditor : Editor {
namespace MeshEditingTools
{

    [CustomEditor(typeof(MeshManager))]
    public class MeshManagerEditor : Editor {
       
        public static void DrawMeshToolOptions()  {
            int before = (int)MeshManager._meshTool();
            playtimeMesherSaveData.inst()._meshTool = (MeshTool)EditorGUILayout.EnumPopup(MeshManager._meshTool());
            if (MeshManager._meshTool() == MeshTool.VertColor) {

                if ((before != (int)MeshManager._meshTool()) && (MeshManager.inst()._target != null))
                    MeshManager.inst().UpdateVertColor();
            }
        }

        public static void DrawMeshEd() {

            MeshManager tmp = MeshManager.inst();

            if (tmp._target == null)  {
                "No target selected ".nl();
                return;
            }

            foreach (meshSHaderMode m in meshSHaderMode.allModes) {
                pegi.newLine();
                if ((!m.isSelected) && (m.ToString().Click().nl()))
                    m.Apply();

            }

            "Function for G Button:".write();
            quickMeshFunctionsExtensions.current = (quickMeshFunctionForG)pegi.editEnum(quickMeshFunctionsExtensions.current);
        
            if (quickMeshFunctionForG.MakeOutline.selected())
                tmp.outlineWidth = EditorGUILayout.FloatField("Width", tmp.outlineWidth);

            if (!quickMeshFunctionForG.Nothing.selected())
                (tmp.G_toolDta.toolsHints[(int)quickMeshFunctionsExtensions.current]).nl();

            DrawMeshToolOptions();

            if (quickMeshFunctionForG.Path.selected()) {

                if (tmp.selectedLine == null) "Select Line".nl();
                else  {

                    if ("Set path start on selected".Click())
                        tmp.SetPathStart();

                    if (tmp.G_toolDta.updated == false)
                        "Select must be a Quad with shared uvs".nl();
                    else
                    {
                        if (tmp.selectedLine == null)
                            tmp.G_toolDta.updated = false;
                        else {

                            "Mode".write();
                            tmp.G_toolDta.mode = (gtoolPathConfig)pegi.editEnum(tmp.G_toolDta.mode);
                            "G to extend".nl();

                         
                        }
                    }
                }
            }

         

             pegi.writeHint(playtimeMesherSaveData.inst()._meshTool.Process().tooltip);

            pegi.newLine();

            playtimeMesherSaveData.inst()._meshTool.Process().tool_pegi();


                tmp.UpdateVertColor();

            switch (playtimeMesherSaveData.inst()._meshTool)
            {
                case MeshTool.vertices:
                   /* EditorGUILayout.LabelField(" Alt+LMB     - Add vert on grid .");
                    EditorGUILayout.LabelField(" Alt+R_MB    - Select, move vert to grid.");
                    EditorGUILayout.LabelField(" Ctrl+R_MB   - Select, don't change grid.");
                    EditorGUILayout.LabelField(" Ctrl+LMB (on tris)    - Break triangle in 3 with 3 unique UVs");
                    EditorGUILayout.LabelField(" Ctrl+LMB (on grid)    - Add vert, don't connect");
                    EditorGUILayout.LabelField(" Ctrl+Delete - Delete vert, heal triangle");
                    EditorGUILayout.LabelField(" N - Make verticles share normal");*/
                    break;
                case MeshTool.VertColor:
                    " 1234 on Line - apply RGBA for Border.".nl();
                    break;
                case MeshTool.AtlasTexture:
                    "Select Texture and click on triangles".nl();
                    break;
            }


        }

        public override void OnInspectorGUI()  {

            MeshManager tmp = (MeshManager)target;

            if (!Application.isPlaying){

                tmp.vertexPointMaterial = (Material)EditorGUILayout.ObjectField("vertexPointMaterial", tmp.vertexPointMaterial, typeof(Material), true);
                tmp.vertPrefab = (GameObject)EditorGUILayout.ObjectField("vertexPrefab", tmp.vertPrefab, typeof(GameObject), true);
                tmp.vertsShowMax = EditorGUILayout.IntField("Max Vert Markers ", tmp.vertsShowMax);
                tmp.pointedVertex.go = (GameObject)EditorGUILayout.ObjectField("pointedVertex", tmp.pointedVertex.go, typeof(GameObject), true);
                tmp.selectedVertex.go = (GameObject)EditorGUILayout.ObjectField("SelectedVertex", tmp.selectedVertex.go, typeof(GameObject), true);

            }
        }



        void ColorAsPosAnimation(ref byte chan, string name)
        {
            float f = ((float)chan) - 127;
            f = EditorGUILayout.Slider(name, f, -125, 125);

            chan = (byte)(f + 127);


        }


    }
}