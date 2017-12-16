using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Painter{

[ExecuteInEditMode]
public class RenderBrush : MonoBehaviour {

		public static PainterManager rtp {get {return PainterManager.inst;}}

    public MeshRenderer meshRendy;
    public MeshFilter meshFilter;
    public Mesh modifiedMesh;
    public Bounds modifiedBound;

    public void RestoreBounds() {
        modifiedMesh.bounds = modifiedBound;
            transform.parent = PainterManager.inst.transform;
    }

    public void CopyAllFrom (GameObject go, Mesh mesh) {

        Transform target = go.transform;

        transform.position = target.position;
        transform.rotation = target.rotation;
        transform.localScale = target.localScale;

        modifiedMesh = mesh;
        meshFilter.sharedMesh = mesh;


        Transform camTransform = PainterManager.inst.transform;
        Vector3 center = camTransform.position + camTransform.forward * 100;
        float extremeBound = 500.0f;
        modifiedBound = meshFilter.sharedMesh.bounds;
        meshFilter.sharedMesh.bounds = new Bounds(transform.InverseTransformPoint(center)
            , Vector3.one * extremeBound);
            transform.parent = target.parent;
    }

    public void Set(Shader shade) {
        meshRendy.sharedMaterial.shader = shade;
        //meshRendy.sharedMaterial.EnableKeyword("BR_EDITING");
    }

        public void Set( Texture tex) {
        meshRendy.sharedMaterial.SetTexture("_MainTex", tex);
    }


		public void PrepareForFullCopyOf (Texture tex){
			float size = PainterManager.orthoSize * 2;
			float aspectRatio = (float)tex.width / (float)tex.height;
			transform.localScale = new Vector3(size * aspectRatio, size, 0);
			transform.localPosition = Vector3.forward * 10;
			transform.localRotation = Quaternion.identity;
			Set(rtp.pixPerfectCopy);
			Set(tex);
			meshFilter.mesh = brushMeshGenerator.inst().GetQuad();
		}


    private void OnEnable() {
     
    }

    // Use this for initialization
    void Awake () {
        if (meshRendy == null)
            meshRendy = GetComponent<MeshRenderer>();

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
}