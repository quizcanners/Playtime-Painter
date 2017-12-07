using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshEditingTools
{

    [System.Serializable]
    public class AtlasMergeConfig {
        public string targetField;
        public AtlasTextureCreator atlasCreator;
    }

    public class MeshAtlasing : MonoBehaviour  {
        public static List<MeshAtlasing> all = new List<MeshAtlasing>();

        public Material terget;
        public AtlasMergeConfig[] a_configs;

#if UNITY_EDITOR
        public void ConvertToAtlased(playtimeMesher m) {
            Material mat = m._meshRenderer.sharedMaterial;
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

                foreach (AtlasMergeConfig aConfig in a_configs)
                    if (field.Equals(aConfig.targetField))
                    {
                        List<Texture2D> aTexes = aConfig.atlasCreator.atlas.Textures;

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

                            m.Edit();
                            VertexAtlasTool.inst.SetAllTrianglesTextureTo(index, 0);
                            MeshManager.inst().Redraw(); //m.RegenerateMesh();

                        } else {
                            Debug.Log("Could not find space for " + m.name + " texture: " + field);
                        }

                    }
            }

            m._meshRenderer.sharedMaterial = terget;

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
			if ((terget != null) && (a_configs.Length == 0)){
				List<string> tnames = terget.getTextures ();
				a_configs = new AtlasMergeConfig[tnames.Count];
				for (int i = 0; i < a_configs.Length; i++) {
					AtlasMergeConfig ac = new AtlasMergeConfig ();
					a_configs [i] = ac;
					ac.targetField = tnames [i];


					ac.atlasCreator.srcFields.Add(tnames [i]);
				}
			}
		}
#endif

        public void EditorPEGI() {
			


        }

    }
}