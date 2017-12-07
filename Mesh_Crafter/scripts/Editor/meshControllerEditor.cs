using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;


namespace MeshEditingTools
{

    [CustomEditor(typeof(playtimeMesher))]
    public class meshControllerEditor : SceneViewEditable<playtimeMesher> {


        public override bool AllowEditing(playtimeMesher targ)
        {
            return (targ != null) && (targ.LockEditing == false);
        }

        public override void getEvents(Event e, Ray ray)
        {
            MeshManager.inst().UpdateInputEditorTime(e, ray, L_mouseUp, L_mouseDwn);
        }

        public override bool OnEditorRayHit(RaycastHit hit, Ray ray)  {
            Transform tf = hit.transform;
            playtimeMesher pm = tf == null ? null : tf.GetComponent<playtimeMesher>();
            playtimeMesher edited = MeshManager.inst()._target;

            Event e = Event.current;

            bool allowRefocusing = false;

            if ((pm != null) && (pm != edited) && (pm.LockEditing == false) && L_mouseDwn && (e.button == 0))
            {
                MeshManager.inst().EditMesh(pm);
                allowRefocusing = true;
            }

            if ((edited == null) || (edited != painter))
                allowRefocusing = true;

            // if ((tf != null) && (tf.tag != "VertexEd"))
            //   allowRefocusing = true;

            if ((((e.button == 1) && (!MeshManager.inst().draggingSelected)) || (e.button == 2)) && ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag) || (e.type == EventType.mouseUp)))
                navigating = true;

            return allowRefocusing;
        }

    /*    void OnDrawGizmos() {
            if (Input.GetMouseButton(0)) Debug.Log("ow shit");
        }*/

        public override void OnInspectorGUI() {
			painter = (playtimeMesher)target;
            ef.start(serializedObject);
            MeshManager m = MeshManager.inst();


			if (painter.ToolManagementPEGI ()) {
				if (painter.isCurrentTool() == false)
					CloseAllButThis(painter);
			}


            if ((m._target != painter) && "Modify Mesh".Click().nl()) {
				PlaytimeToolComponent.enabledTool = typeof(playtimeMesher);
                painter.UpdateComponents();
                if (painter._meshFilter != null)
                    MeshManager.inst().EditMesh(painter);
                else Debug.Log("No Mesh Filter to work with");
            }


			if ((painter == null) || (m._target != painter))
				return;

            playtimeMesherSaveData sd = MeshManager.cfg;

            if (MeshManager.inst().showGrid) {
                "Snap to grid:".toggle(100,ref sd.SnapToGrid);

                if (sd.SnapToGrid) 
                    "size:".edit(40,ref sd.SnapToGridSize).nl();
            }

            MeshManagerEditor.DrawMeshEd();

            if ("Mesh Packaging Solution".foldout().nl())
                DrawMeshPackagingSolution(MeshManager.cfg.meshProfiles[0]);

        }

        public void DrawMeshPackagingSolution(MeshSolutionProfile s) {
            for (int i = 0; i < s.sln.Length; i++)
                SlnSelector(s.sln[i]);
        }

        public void SlnSelector(VertexSolution sln) {
            (sln.target.name() + ":").toggle( 80, ref sln.enabled);

            if (sln.enabled) {

                List<VertexDataType> tps = MeshSolutions.getTypesBySize(sln.vals.Length);
                string[] nms = new string[tps.Count + 1];

                for (int i = 0; i < tps.Count; i++)
                    nms[i] = tps[i].ToString();

                nms[tps.Count] = "Other";

                int selected = tps.Count;

                if (sln.sameSizeValue != null) 
                    for (int i = 0; i < tps.Count; i++)
                        if (tps[i] == sln.sameSizeValue) {
                            selected = i;
                            break;
                        }
                
                pegi.select(ref selected, nms).nl();

                if (selected >= tps.Count) sln.sameSizeDataIndex = -1;
                else
                    sln.sameSizeDataIndex = tps[selected].myIndex;

                string[] allDataNames = MeshSolutions.getAllTypesNames();

                if (sln.sameSizeValue == null)  {
                    for (int i = 0; i < sln.vals.Length; i++) {
                        VertexDataValue v = sln.vals[i];

                       sln.target.getFieldName(i).select( 40, ref v.typeIndex, allDataNames);

                        string[] typeFields = new string[v.vertDataType.chanelsNeed];

                        for (int j = 0; j < typeFields.Length; j++)
                            typeFields[j] = v.vertDataType.getFieldName(j);

                        pegi.select(ref v.valueIndex, typeFields).nl();
                    }
                }
                "**************************************************".nl();
            }
        }
    }
}