using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using PlayerAndEditorGUI;

[Serializable]
public class AtlasTexture {

    public int AtlasSize = 2048;

    public int textureSize = 512;

    public bool sRGB = true;

    public string name = "_MainTex";

    public Texture2D a_texture;

    public List<Texture2D> Textures = new List<Texture2D>();

	public override string ToString (){
		return name;
	}

    public void adjustListSize() {
        int ntc = TextureCount;
        while (Textures.Count < ntc)
            Textures.Add(null);
    }

    public int TextureCount {  get {
            int row = AtlasSize / textureSize;

            return row * row;
        }
    }

    public void TextureToAtlas(Texture2D tex, int x, int y) {
#if UNITY_EDITOR
        tex.Reimport_IfNotReadale();
#endif

        Color[] from = tex.GetPixels(textureSize, textureSize);

        a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, from);

    }

    public void smoothBorders(Texture2D atlas, int miplevel) {
        Color[] col = atlas.GetPixels(miplevel);

        int aSize = AtlasSize;
        int tSize = textureSize;

        for (int i=0; i<miplevel; i++) {
            aSize /= 2;
            tSize /= 2;
        }

        if (tSize == 0)
            return;

       // AtlasSize = AtlasSize

        int cnt = aSize / tSize;

        linearColor tmp = new linearColor();


        for (int ty = 0; ty < cnt; ty++) {
            int startY = ty * tSize * aSize;
            int lastY = (ty * tSize + tSize - 1)* aSize;
            // Debug.Log("Processing Y "+ (ty * tSize) +" line ");
            for (int tx = 0; tx < cnt; tx++) {
                int startX = tx * tSize;
                int lastX = startX + tSize - 1;

                //   Debug.Log("Processing X " + (tx * tSize) + " line ");

                tmp.Zero();
                tmp.Add(col[startY + startX]);
                tmp.Add(col[startY + lastX]);
                tmp.Add(col[lastY + startX]);
                tmp.Add(col[lastY + lastX]);

                tmp.MultiplyBy(0.25f);

                Color tmpC = tmp.ToColor();


                col[startY + startX] = tmpC;
                col[startY + lastX] = tmpC;
                col[lastY + startX] = tmpC;
                col[lastY + lastX] = tmpC;


                for (int x= startX+1; x<lastX; x++) {
                    tmp.Zero();
                    tmp.Add(col[startY + x]);
                    tmp.Add(col[lastY + x]);
                    tmp.MultiplyBy(0.5f);
                    tmpC = tmp.ToColor();
                    col[startY + x] = tmpC;
                    col[lastY + x] = tmpC;
                }

                for (int y = startY + aSize; y < lastY; y+= aSize) {
                    tmp.Zero();
                    tmp.Add(col[y + startX]);
                    tmp.Add(col[y + lastX]);
                    tmp.MultiplyBy(0.5f);
                    tmpC = tmp.ToColor();
                    col[y + startX] = tmpC;
                    col[y + lastX] = tmpC;
                }

            }
        }

        atlas.SetPixels(col, miplevel);
    }

    public void ReconstructAtlas() {

        if ((a_texture != null) && (a_texture.width != AtlasSize)) {
            GameObject.DestroyImmediate (a_texture);
            a_texture = null;
        }

        if (a_texture == null)
            a_texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat.ARGB32, true, !sRGB);

        int texesInRow = AtlasSize / textureSize;


        int curIndex = 0;

        for (int i=0; i<texesInRow; i++) 
           for (int j =0; j<texesInRow; j++) {
              
                    if ((Textures.Count > curIndex) && (Textures[curIndex] != null))
                        TextureToAtlas(Textures[curIndex], i, j);

                curIndex++;
            }

    }

}

[ExecuteInEditMode]
public class AtlasTextureCreator : MonoBehaviour {

	public static List<AtlasTextureCreator> atlases = new List<AtlasTextureCreator> ();

    public const string atlasFolderName = "ATLASES";

    public AtlasTexture atlas;

	public List<string> srcFields = new List<string> (); 

    public MeshRenderer preview;

	public static AtlasTextureCreator getByName(string name){
		if (atlases.Count == 0)
			return null;

		List<string> nms = new List<string> ();
		foreach (AtlasTextureCreator ac in atlases)
			nms.Add (ac.ToString ());

		return atlases[name.FindMostSimilarFrom(nms.ToArray())];

	}

	private void OnDisable(){
		atlases.Remove (this);
	}

    private void OnEnable()
    {
		atlases.Add (this);

        if (preview == null)
            preview = GetComponent<MeshRenderer>();

    }

    public void SmoothAtlas() {
        Debug.Log("Smoothing " + atlas.a_texture.name + " with " + atlas.a_texture.mipmapCount + " mipmaps");
        for (int m = 0; m < atlas.a_texture.mipmapCount; m++)
            atlas.smoothBorders(atlas.a_texture, m);

        atlas.a_texture.Apply();
    }

#if UNITY_EDITOR
    public void ReconstructAsset(AtlasTexture a) {

        a.ReconstructAtlas();

        byte[] bytes = a.a_texture.EncodeToPNG();

        string lastPart = "/" + atlasFolderName + "/";
        string fullPath = Application.dataPath + lastPart;
        Directory.CreateDirectory(fullPath);

        string fileName = a.name + ".png";
        string relativePath = "Assets" + lastPart + fileName;
        fullPath += fileName;

        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.Refresh(); // few times caused color of the texture to get updated to earlier state for some reason

        a.a_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

        TextureImporter ti = a.a_texture.getTextureImporter();
        bool needReimport = ti.wasNotReadable();
        needReimport |= ti.wasClamped();

        if (needReimport) ti.SaveAndReimport();



        if (preview != null) preview.sharedMaterial.mainTexture = a.a_texture;

    }
#endif

    public void PEGI() {

#if UNITY_EDITOR
        pegi.write("Name:", 60);
        pegi.edit(ref atlas.name);
        pegi.newLine();

        pegi.write("Atlas size:", 80);
        pegi.edit(ref atlas.AtlasSize);
        atlas.AtlasSize = Mathf.ClosestPowerOfTwo(atlas.AtlasSize);
        pegi.newLine();

        pegi.write("Textures size:", 80);
        if (pegi.edit(ref atlas.textureSize))
            pegi.foldIn();
        atlas.textureSize = Mathf.Clamp(atlas.textureSize, 32, atlas.AtlasSize / 2);
        atlas.textureSize = Mathf.ClosestPowerOfTwo(atlas.textureSize);
        pegi.newLine();

        if (pegi.foldout("Textures:"))
        {
            pegi.newLine();
            atlas.adjustListSize();
            int max = atlas.TextureCount;

            for (int i = 0; i < max; i++)
            {
                Texture2D t = atlas.Textures[i];
                if (pegi.edit(ref t))
                    atlas.Textures[i] = t;
                // = (Texture2D)EditorGUILayout.ObjectField(atlas.Textures[i], typeof(Texture2D), true);
                pegi.newLine();
            }
        }

        pegi.newLine();
        pegi.write("Is Color Atlas:", 80);
        pegi.toggle(ref atlas.sRGB);

        pegi.newLine();

        if (pegi.Click("Generate"))
            ReconstructAsset(atlas);

        if ((atlas.a_texture != null) && (pegi.Click("Smooth Edges")))
            SmoothAtlas();

        pegi.newLine();

        if (atlas.a_texture != null)
        {
            pegi.write("Atlas At " + AssetDatabase.GetAssetPath(atlas.a_texture));
            EditorGUILayout.ObjectField(atlas.a_texture, typeof(Texture2D), false);
        }

#endif

        pegi.newLine();

    }


}

// Postprocesses all textures that are placed in a folder
// "invert color" to have their colors inverted.
/*
public class InvertColor : AssetPostprocessor {
    void OnPostprocessTexture(Texture2D texture)
    {
        // Only post process textures if they are in a folder
        // "invert color" or a sub folder of it.
        string lowerCaseAssetPath = assetPath.ToLower();
        if (lowerCaseAssetPath.IndexOf("/"+ AtlasTextureCreator.atlasFolderName + "/") == -1)
            return;

        Debug.Log("Postprocessing " + texture.name + " with "+texture.mipmapCount + " mipmaps");


        TextureImporter ti = texture.getTextureImporter();
        bool needReimport = ti.wasNotReadable();
        needReimport |= ti.wasClamped();

        if (needReimport) ti.SaveAndReimport();

        for (int m = 0; m < texture.mipmapCount; m++) 
            AtlasTexture.smoothBorders(texture, m);
        
        // Instead of setting pixels for each mip map levels, you can also
        // modify only the pixels in the highest mip level. And then simply use
        // texture.Apply(true); to generate lower mip levels.
    }
}
*/