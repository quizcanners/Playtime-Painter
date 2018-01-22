using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Painter
{

    [System.Serializable]
    public class FieldAtlas {
        public string targetField;
        public AtlasTextureCreator atlasCreator;
    }

    public class MaterialAtlases  {
        public static List<MaterialAtlases> all = new List<MaterialAtlases>();

        public Material terget;
        public FieldAtlas[] fields;

#if UNITY_EDITOR
        public void ConvertToAtlased(PlaytimePainter m) {
            Material mat = m.meshRenderer.sharedMaterial;
            List<string> tfields = mat.getTextures();

            int index = -1;

            foreach (string field in tfields) {

                Texture texture = mat.GetTexture(field);

                if (texture == null)
                {
                    Debug.Log(m.name + " no " + field + " texture.");
                    continue;
                }

                if (texture.GetType() != typeof(Texture2D))
                {
                    Debug.Log(m.name + " is not using a Texture2D on " + field);
                    continue;
                }

                foreach (FieldAtlas aConfig in fields)
                    if (field.Equals(aConfig.targetField))
                    {
                        List<Texture2D> aTexes = aConfig.atlasCreator.Textures;

                        if (index == -1)
                            for (int i = 0; i < aTexes.Count; i++)
                                if (aTexes[i] == texture)
                                {
                                    index = i;
                                    i = 999;
                                }

                        if (index == -1)
                            for (int i = 0; i < aTexes.Count; i++)
                                if (aTexes[i] == null)
                                {
                                    index = i;
                                    i = 999;
                                }


                        if (index != -1) {

                            Debug.Log("Assigning " + m.gameObject.name + " to Atlas index " + index);
                            MeshManager.inst().EditMesh(m, false);
                            VertexAtlasTool.inst.SetAllTrianglesTextureTo(index, 0);
                            MeshManager.inst().Redraw(); 
                        } else {
                            Debug.Log("Could not find space for " + m.name + " texture: " + field);
                        }

                    }
            }

            m.meshRenderer.sharedMaterial = terget;

        }
#endif

        public void OnEnable()
        {
            all.Add(this);
        }

        public void OnDisable()
        {
            all.Remove(this);
        }

#if UNITY_EDITOR
        public void OnChangeTargetMaterial (){
			if ((terget != null) && (fields.Length == 0)){
				List<string> tnames = terget.getTextures ();
				fields = new FieldAtlas[tnames.Count];
				for (int i = 0; i < fields.Length; i++) {
					FieldAtlas ac = new FieldAtlas ();
					fields [i] = ac;
					ac.targetField = tnames [i];


					ac.atlasCreator.srcFields.Add(tnames [i]);
				}
			}
		}
#endif

        public void EditorPEGI() {

            pegi.write("Material:");
            pegi.edit(ref terget);


        }

    }
}