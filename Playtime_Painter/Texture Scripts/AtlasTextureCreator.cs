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
using StoryTriggerData;

namespace Playtime_Painter {

    [Serializable]
    public class AtlasTextureCreator : iGotName  {

        static PainterConfig cfg { get { return PainterConfig.inst; } }

        public int AtlasSize = 2048;

        public int textureSize = 512;

        public bool sRGB = true;

        public string name = "New_Atlas";

        public string Name { get { return name; } set { name = value; } }

		public List<string> targetFields;

		public List<string> atlasFields;

        public Texture2D a_texture;

        public List<Texture2D> textures;

		public int row { get{ return AtlasSize / textureSize;}}

		public void AddTargets(FieldAtlas at, string target){
			if (!atlasFields.Contains (at.atlasedField))
				atlasFields.Add (at.atlasedField);
			if (!targetFields.Contains (target))
				targetFields.Add (target);
		}


        public override string ToString()
        {
            return name;
        }

		void Init (){
			if (targetFields == null)
			targetFields = new List<string> ();
			if (atlasFields == null)
			atlasFields = new List<string> ();
			if (textures == null)
			textures = new List<Texture2D> ();
			adjustListSize ();
		}

		public AtlasTextureCreator (){
			Init ();
		}


		public AtlasTextureCreator(string nname){
			name = nname;
			name = name.GetUniqueName (PainterManager.inst.atlases);
			Init ();
		}

        public void adjustListSize()
        {
            int ntc = TextureCount;
            while (textures.Count < ntc)
                textures.Add(null);
        }

        public int TextureCount {
			get { int r = row; return r * r; }
        }

        public void ColorToAtlas (Color col, int x, int y) {
            int size = textureSize * textureSize;
            Color[] pix = new Color[size];
            for (int i = 0; i < size; i++)
                pix[i] = col;

            a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, pix);
        }

        public void TextureToAtlas(Texture2D tex, int x, int y)
        {
#if UNITY_EDITOR
            tex.Reimport_IfNotReadale();
#endif

            Color[] from = tex.GetPixels(textureSize, textureSize);

            a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, from);

        }

        public void smoothBorders(Texture2D atlas, int miplevel)
        {
            Color[] col = atlas.GetPixels(miplevel);

            int aSize = AtlasSize;
            int tSize = textureSize;

            for (int i = 0; i < miplevel; i++)
            {
                aSize /= 2;
                tSize /= 2;
            }

            if (tSize == 0)
                return;
            
            int cnt = aSize / tSize;

            linearColor tmp = new linearColor();


            for (int ty = 0; ty < cnt; ty++)
            {
                int startY = ty * tSize * aSize;
                int lastY = (ty * tSize + tSize - 1) * aSize;
                for (int tx = 0; tx < cnt; tx++)
                {
                    int startX = tx * tSize;
                    int lastX = startX + tSize - 1;
                    

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


                    for (int x = startX + 1; x < lastX; x++)
                    {
                        tmp.Zero();
                        tmp.Add(col[startY + x]);
                        tmp.Add(col[lastY + x]);
                        tmp.MultiplyBy(0.5f);
                        tmpC = tmp.ToColor();
                        col[startY + x] = tmpC;
                        col[lastY + x] = tmpC;
                    }

                    for (int y = startY + aSize; y < lastY; y += aSize)
                    {
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

        public void ReconstructAtlas()
        {

            if ((a_texture != null) && (a_texture.width != AtlasSize))
            {
                GameObject.DestroyImmediate(a_texture);
                a_texture = null;
            }

            if (a_texture == null)
                a_texture = new Texture2D(AtlasSize, AtlasSize, TextureFormat.ARGB32, true, !sRGB);

            int texesInRow = AtlasSize / textureSize;


            int curIndex = 0;

            Color defaltCol = new Color(0.5f, 0.5f, 0.5f, 0.5f);

			for (int y = 0; y < texesInRow; y++)
				for (int x = 0; x < texesInRow; x++){
                    if ((textures.Count > curIndex) && (textures[curIndex] != null))
                        TextureToAtlas(textures[curIndex], x, y);
                    else
                        ColorToAtlas(defaltCol, x, y);

                    curIndex++;
                }

        }

        public List<string> srcFields = new List<string>();

        public void SmoothAtlas() {
          //  Debug.Log("Smoothing " + a_texture.name + " with " + a_texture.mipmapCount + " mipmaps");
            for (int m = 0; m < a_texture.mipmapCount; m++)
                smoothBorders(a_texture, m);

            a_texture.Apply();
        }

#if UNITY_EDITOR
        public void ReconstructAsset() {

            ReconstructAtlas();

            SmoothAtlas();

            byte[] bytes = a_texture.EncodeToPNG();

            string lastPart = cfg.atlasFolderName.AddPreSlashIfNotEmpty() + "/";
            string fullPath = Application.dataPath + lastPart;
            Directory.CreateDirectory(fullPath);

            string fileName = name + ".png";
            string relativePath = "Assets" + lastPart + fileName;
            fullPath += fileName;

            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh(); // few times caused color of the texture to get updated to earlier state for some reason

            a_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));
            
            TextureImporter other = null;

            foreach (var t in textures) 
                if (t != null) {
                    other = t.getTextureImporter();
                    break;
            }

            TextureImporter ti = a_texture.getTextureImporter();
            bool needReimport = ti.wasNotReadable();
            if (other!= null)
            needReimport |= ti.wasWrongIsColor(other.sRGBTexture);
            needReimport |= ti.wasClamped();

            if (needReimport) ti.SaveAndReimport();

        }
#endif

        public bool PEGI() {
            bool changed = false;
#if UNITY_EDITOR


            changed |= "Name:".edit(60, ref name).nl();

            changed |=  "Atlas size:".editDelayed(ref AtlasSize, 80).nl();
            AtlasSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(AtlasSize, 512, 4096));

			if ("Textures size:".editDelayed(ref textureSize, 80).nl()){
                pegi.foldIn();
                changed = true;

            }

            textureSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(textureSize, 32, AtlasSize / 2));

			adjustListSize();

            if ("Textures:".foldout().nl()) {
                adjustListSize();
                int max = TextureCount;

                for (int i = 0; i < max; i++) {
                    Texture2D t = textures[i];
                    if (pegi.edit(ref t).nl())
                        textures[i] = t;
                }
            }

            pegi.newLine();
            "Is Color Atlas:".toggle(80, ref sRGB).nl();

            if ("Generate".Click().nl())
                ReconstructAsset();

            if (a_texture != null)            
                ("Atlas At " + AssetDatabase.GetAssetPath(a_texture)).edit(ref a_texture, false).nl();

#endif

            return changed;
        }


    }
  }