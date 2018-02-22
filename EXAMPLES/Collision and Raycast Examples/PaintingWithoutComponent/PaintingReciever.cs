using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintingReciever : MonoBehaviour {

    // For best performance on Skinned Meshes use RenderTexture
    public MeshFilter meshFilter;
    public SkinnedMeshRenderer skinnedMesh;

    public Texture texture;

    private void OnEnable() {
        
        if (texture == null) {
            var rendy = GetComponent<Renderer>();
            if ((rendy != null) && (rendy.material) && (rendy.material.mainTexture!= null)) 
              
                texture = rendy.material.mainTexture;
        }
    }

}
