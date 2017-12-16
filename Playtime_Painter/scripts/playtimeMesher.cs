using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Painter;
using PlayerAndEditorGUI;

namespace Painter {

    public class MeshPainter {

        public PlaytimePainter p;

        public bool enabled { get { return p.isCurrentTool() && p.meshPainting && (MeshManager.inst()._target == this); }    }

        public Renderer _meshRenderer { get { return p.meshRenderer; } }

        public MeshManager manager { get { return MeshManager.inst(); } }

        public EditableMesh editedMesh {
            get {
                if (manager._target == this) return manager._Mesh;
                Debug.Log(p.name + " call Edit before accessing edited mesh."); return null;
            }
        }

        public string saveMeshDta;
  
        public int GetAnimationUVy()
        {
            return 0;
        }

        public bool AnimatedVertices()
        {
            return false;
        }

        public int GetVertexAnimationNumber()
        {
            return 0;
        }

        public void Edit()
        {
            MeshManager.inst().EditMesh(this);
        }

        public void OnMouseOver() {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(1))
            {
                GameObject[] tmp = new GameObject[1];
                tmp[0] = p.gameObject;
                Selection.objects = tmp;
            }
#endif
        }

        public bool PEGI() {
            bool changed = false;

                MeshManager m = MeshManager.inst();

                if ((m._target != this) && "Modify Mesh".Click().nl()) {
                    if (p.meshFilter != null)
                        MeshManager.inst().EditMesh(this);
                    else Debug.Log("No Mesh Filter to work with");
                }
                
                if ((this == null) || (m._target != this))
                    return changed;

                playtimeMesherSaveData sd = MeshManager.cfg;

                if (MeshManager.inst().showGrid){
                    "Snap to grid:".toggle(100, ref sd.SnapToGrid);

                    if (sd.SnapToGrid)
                        "size:".edit(40, ref sd.SnapToGridSize).nl();
                }

            MeshManager.inst().PEGI();

            if ("Mesh Packaging Solution".foldout().nl())
                MeshManager.cfg.meshProfiles[0].PEGI();

            return changed;
        }

        public MeshPainter(PlaytimePainter painter) {
            p = painter;
        }

    }
}