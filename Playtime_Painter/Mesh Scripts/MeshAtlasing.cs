using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using StoryTriggerData;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    [System.Serializable]
    public class FieldAtlas {
    
        

        static PainterManager texMGMT { get { return PainterManager.inst; } }

		public string atlasedField;
		public int originField;
		int atlasIndex;
        public int atlasCreatorId;
		public bool enabled;
        public Color col;
        public AtlasTextureCreator atlasCreator { get { return texMGMT.atlases.Count > atlasCreatorId ? texMGMT.atlases[atlasCreatorId] : null; } }


        [SerializeField]
        bool foldoutAtlas = false;
		public void PEGI(MaterialAtlases a){
            
			atlasedField.toggle ("Use this field", 50, ref enabled);

            if (enabled){

                pegi.select(ref originField, a.originalTextures).nl();

                pegi.Space();

                if (atlasCreator != null)
                    "Atlas".foldout(ref foldoutAtlas);
                
                else foldoutAtlas = false;

                if (!foldoutAtlas)
                {
                    if (atlasCreator != null)
                        "Color".toggle(35, ref atlasCreator.sRGB);

                    pegi.select(ref atlasCreatorId, PainterManager.inst.atlases);
                    if (icon.Add.Click("Create new Atlas", 15).nl())
                    {
                        atlasCreatorId = PainterManager.inst.atlases.Count;
                        var ac = new AtlasTextureCreator(atlasedField + " for " + a.name);
                        PainterManager.inst.atlases.Add(ac);
                    }
                }
                else atlasCreator.PEGI().nl();



                pegi.Space();



                if ((atlasedField != null) && (a.originalMaterial != null) && (atlasCreator != null) && (originField < a.originalTextures.Count))
                {
                    Texture t = a.originalMaterial.GetTexture(a.originalTextures[originField]);
                    if ((t != null) && t.GetType() == typeof(Texture2D)) //&& (atlasCreator.textures.Contains((Texture2D)t)))
                        icon.Done.write(25);
                    else "Will use Color".edit(ref col).nl();
                } else
                "Color".edit("Color that will be used instead of a texture.", 35, ref col).nl();
                pegi.Space();


            }
           
                
			pegi.newLine ();
			pegi.Space ();
			pegi.newLine ();
		}
    }

	[System.Serializable]
    public class MaterialAtlases : iGotName {
        //public static List<MaterialAtlases> all = new List<MaterialAtlases>();

		public string name;

		public override string ToString () {
			return name;
		}

        public Material originalMaterial;
        public Shader originalShader;
		public List<string> originalTextures;
		public Material AtlasedMaterial;
        Material destinationMaterial { get { return AtlasedMaterial ? AtlasedMaterial : originalMaterial; } }

        public string Name { get  {return name; } set { name = value; } }

        Shader atlasedShader;
        public List<FieldAtlas> fields;
		public int matAtlasProfile;


		public MaterialAtlases (){
			if (fields == null)
			fields = new List<FieldAtlas> ();
		}

		public MaterialAtlases(string nname){
			name = nname;
			if ((name == null) || (name.Length == 0)) 
				name = "new";
			name = name.GetUniqueName (PainterManager.inst.atlasedMaterials);

			fields = new List<FieldAtlas> ();
		}


        public void ConvertToAtlased(PlaytimePainter painter) {
#if UNITY_EDITOR

            if (AtlasedMaterial == null)
                AtlasedMaterial = painter.InstantiateMaterial(true);
            
			painter.SetOriginalShader ();

			painter.UpdateOrSetTexTarget (texTarget.Texture2D);

            Material mat = painter.getMaterial(false);
            List<string> tfields = mat.getTextures();
            
			int index = 0;
			List<FieldAtlas> passedFields = new List<FieldAtlas> ();
			List<Texture2D> passedTextures = new List<Texture2D> ();
            List<Color> passedColors = new List<Color>();

            foreach (var f in fields)
				if ((f.enabled) && (f.atlasCreator != null) && (tfields.Contains (originalTextures[f.originField]))) {
				
					string original = originalTextures [f.originField];

					Texture tex = mat.GetTexture (original);

                    Texture2D texture = null;

                    if (tex == null)
                    {
                        var note = painter.name + " no " + original + " texture. Using Color.";
                        note.showNotification();
                        Debug.Log(note);
                    }
                    else
                    {

                        if (tex.GetType() != typeof(Texture2D)) {
                            Debug.Log("Not a Texture 2D: " + original);
                            return;
                        }

                        texture = (Texture2D)tex;
                        
                    }

                    var aTexes = f.atlasCreator.textures;

                    bool added = false;

					for (int i = index; i < aTexes.Count; i++)
						if ((aTexes [i] == null) || (!aTexes[i].used) || (aTexes [i].texture == texture)) {
							index = i;
							passedFields.Add (f);
							passedTextures.Add (texture);
                            passedColors.Add(f.col);
                            added = true;
                            break;
						}

					if (!added) {
						Debug.Log ("Could not find a place for "+original);
						return;
					}
			}
            
			if (passedFields.Count > 0) {

                bool firstAtlasing = false;

                if (painter.preAtlasingMaterials == null) {
                    painter.preAtlasingMaterials = painter.getMaterials();
                    painter.preAtlasingMesh = painter.getMesh();
                    firstAtlasing = true;
                }

				var MainField = passedFields [0];

				painter.atlasRows = MainField.atlasCreator.row;

				Vector2 tyling = mat.GetTextureScale(originalTextures [MainField.originField]);
				Vector2 offset = mat.GetTextureOffset(originalTextures [MainField.originField]);

				for(int i=0; i<passedFields.Count; i++){// var f in passedFields){
					var f = passedFields[i];
					var ac = f.atlasCreator;

                    ac.textures[index] = new AtlasTextureField(passedTextures[i], passedColors[i]);
                   
                    ac.AddTargets (f,originalTextures [f.originField]);
					ac.ReconstructAsset();
                    AtlasedMaterial.SetTexture (f.atlasedField, ac.a_texture);
				}

				MeshManager.inst.EditMesh(painter, true);

                if (firstAtlasing)
                    painter.preAtlasingSavedMesh = MeshManager.inst._Mesh.Encode().ToString();

                painter.selectedMeshProfile = matAtlasProfile;
                
				if ((tyling != Vector2.one) || (offset != Vector2.zero)) {
					MeshManager.inst._Mesh.TileAndOffsetUVs (offset, tyling);
					Debug.Log ("offsetting "+offset + " tyling "+tyling);
				}
                
				TriangleAtlasTool.inst.SetAllTrianglesTextureTo(index, 0, painter.selectedSubmesh);
				MeshManager.inst.Redraw();
				MeshManager.inst.DisconnectMesh ();

                AtlasedMaterial.SetFloat (PainterConfig.atlasedTexturesInARow , painter.atlasRows);
				painter.material = AtlasedMaterial;

                if (firstAtlasing) {
                    var m = painter.getMesh();
                    m.name = m.name + "_Atlased_" + index;
                }
                //painter.getMaterial(false).SetTextureOffset(1,Vector2.zero);

                AtlasedMaterial.EnableKeyword(PainterConfig.UV_ATLASED);

			}
#endif
        }


        public void FindAtlas(int field){
            var texMGMT = PainterManager.inst;
            
            for (int a = 0; a< texMGMT.atlases.Count; a++) {
                var atl = texMGMT.atlases[a];
                if (atl.atlasFields.Contains(fields[field].atlasedField)) {
				for (int i=0; i< originalTextures.Count; i++){
					if (atl.targetFields.Contains (originalTextures[i])) {
						fields [field].atlasCreatorId = a;
                            Texture tex = originalMaterial.GetTexture (originalTextures[i]);
							if ((tex!= null) && (tex.GetType() == typeof(Texture2D)) && (atl.textures.Contains((Texture2D)tex)))
						return;
					}
				}
				}
			}
		}


        public void OnChangeMaterial (PlaytimePainter painter){
#if UNITY_EDITOR
          
			if (originalMaterial != null)
				originalTextures = originalMaterial.getTextures ();

			if ((destinationMaterial != null) && (destinationMaterial.HasProperty(PainterConfig.isAtlasedProperty))) {
				List<string> aTextures = destinationMaterial.getTextures ();
				fields.Clear ();
				for (int i = 0; i < aTextures.Count; i++) {
					FieldAtlas ac = new FieldAtlas ();
					fields.Add(ac);
					ac.atlasedField = aTextures [i];
				}
				atlasedShader = destinationMaterial.shader;

            
                    foreach (var p in MaterialEditor.GetMaterialProperties(new Material[] { destinationMaterial }))
                        if (p.displayName.Contains(PainterConfig.isAtlasableDisaplyNameTag))
                            foreach (var f in fields)
                                if (f.atlasedField.SameAs(p.name)) {
                                    f.enabled = true;
                                    continue;
                                }

                if (AtlasedMaterial == null)
                    for (int i = 0; i < fields.Count; i++)
                        fields[i].originField = i;
                else if (originalMaterial != null) {
                    var orTexs = originalMaterial.getTextures();
                    foreach (var f in fields)
                        for (int i = 0; i < orTexs.Count; i++)
                            if (orTexs[i].SameAs(f.atlasedField))
                                f.originField = i;

                    
                }
			}
            
            if (originalMaterial != null)
				for (int i = 0; i < fields.Count; i++)
					FindAtlas (i);
#endif
        }


        [SerializeField]
        private bool showHint;
        public void PEGI(PlaytimePainter painter) {

#if UNITY_EDITOR

            painter.SetOriginalShader ();

				Material mat = painter.getMaterial (false);

			if  ((mat!= null) && ((mat != originalMaterial) || mat.shader != originalShader)) {
				originalMaterial = mat;
                originalShader = mat.shader;
                OnChangeMaterial (painter);
			}
            "Name".edit(50,ref name).nl();
            if ("Hint".foldout(ref showHint).nl()) {

                ("If you don't set Atlased Material(Destination)  it will try to create a copy of current material and set isAtlased toggle on it, if it has one." +
                    " Below you can see: list of Texture Properties, for each you can select or create an atlas. Atlas is a class that holds all textures assigned to an atlas, and also creates and stores the atlas itself." +
                    "After this you can select a field from current Material, texture of which will be copied into an atlas. A bit confusing, I know)" +
                    "Also if stuff looks smudged, rebuild the light.").writeHint(); 
            }

			if (("Atlased Material:".edit (90, ref AtlasedMaterial).nl ()) || 
				(AtlasedMaterial!= null && AtlasedMaterial.shader != atlasedShader))
				OnChangeMaterial (painter);

            if (painter != null) {
                var mats = painter.getMaterials();
                if (mats != null)
                {
                    if (mats.Length > 1)
                    {
                        if ("Source Material:".select("Same as selecting a submesh, which will be converted", 90, ref painter.selectedSubmesh, mats))
                        OnChangeMaterial(painter);
                    }
                    else if (mats.Length > 0)
                        "Source Material".write("Submesh which will be converted", 90, mats[0]);
                }
                pegi.newLine();
                pegi.Space();
                pegi.newLine();
            }



            pegi.Space();
            pegi.newLine();

            foreach (var f in fields)
                f.PEGI(this);

            "Mesh Profile".select (110, ref matAtlasProfile, PainterConfig.inst.meshPackagingSolutions).nl ();

            if ((destinationMaterial != null) && (!destinationMaterial.HasProperty(PainterConfig.isAtlasedProperty))) {
                if (AtlasedMaterial == null) pegi.writeHint("Original Material doesn't have isAtlased property, change shader or add Destination Atlased Material");
                else pegi.writeHint("Atlased Material doesn't have isAtlased property");
            } else if (originalMaterial != null) {
                
                string names = "";
                foreach (var f in fields)
                    if (f.enabled && f.atlasCreator == null)  names += f.atlasedField + ", ";  

                if (names.Length > 0) 
                    pegi.writeHint("Fields "+names+" don't have atlases assigned to them, create some");
                else if ("Convert to Atlased".Click())
				    ConvertToAtlased(painter);
            }

#endif

        }

    }
}