using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using MeshEditingTools;
using PlayerAndEditorGUI;

[AddComponentMenu("Mesh/Mesh Editor")]
[ExecuteInEditMode]
public class playtimeMesher : PlaytimeToolComponent {
    public bool LockEditing;

	public static bool isCurrent_Tool(){
		return enabledTool == typeof(playtimeMesher);
	}

    public MeshManager manager { get { return MeshManager.inst(); } }
    public EditableMesh editedMesh { get { if (manager._target == this) return manager._Mesh; Debug.Log(name + " call Edit before accessing edited mesh."); return null;} }

    public string saveMeshDta;
    public MeshCollider _meshCollider;
    public MeshFilter _meshFilter;
    public MeshRenderer _meshRenderer;

    public virtual Vector2 GetTextureSize() {
        return Vector2.one * 128;
    }

    public virtual int GetAnimationUVy() {
        return 0;
    }

    public virtual bool AnimatedVertices() {
        return false;
    }

    public int GetVertexAnimationNumber() {
        return 0;
    }

    public void UpdateComponents() {
        if (_meshCollider == null)
            _meshCollider = UnityHelperFunctions.ForceMeshCollider(this.gameObject);
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Edit() {
        MeshManager.inst().EditMesh(this);
    }

	void Start () {
        if (Application.isPlaying == false)
            UpdateComponents();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnMouseOver()  {
		if (!isCurrent_Tool()) return;
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(1))
        {
            GameObject[] tmp = new GameObject[1];
            tmp[0] = this.gameObject;
            Selection.objects = tmp;
        }
#endif
    }

#if UNITY_EDITOR
    [MenuItem("Tools/" + MeshManager.ToolName + "/Attach To Every Mesh")]
    static void giveMesherToAll() {
        MeshRenderer[] allObjects = FindObjectsOfType<MeshRenderer>();
        foreach (MeshRenderer mr in allObjects)
            if ((mr.GetComponent<playtimeMesher>() == null) && (mr.transform.IsChildOf(MeshManager.inst().transform) == false))
                mr.gameObject.AddComponent<playtimeMesher>();
    }

    [MenuItem("Tools/" + MeshManager.ToolName + "/Remove from Every Mesh")]
    static void takeMesherFromAll() {
        MeshRenderer[] allObjects = FindObjectsOfType<MeshRenderer>();
        foreach (MeshRenderer mr in allObjects) {
            playtimeMesher ip = mr.GetComponent<playtimeMesher>();
            if (ip != null)
                DestroyImmediate(ip);
        }
        MeshManager rtp = FindObjectOfType<MeshManager>();
        if (rtp != null) {
            DestroyImmediate(rtp.gameObject);
            Debug.Log("Destroying all Mesh Managers");
        }

    }
#endif

    void OnDisable() {
        if (MeshManager.inst()._target == this)
            MeshManager.inst().DisconnectMesh();
    }

    void OnDrawGizmosSelected() {
            MeshManager.inst().DRAW_LINES();
    }
		
	public override void PEGI(){
		ToolSelector ();
		ToolManagementPEGI ();

        "Mesh Editor".nl();
	}
		


}
