using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Painter
{

    [System.Serializable]
    public class FieldAtlas {
        
		public string atlasedField;
		public int originField;
		int atlasIndex;
        public AtlasTextureCreator atlasCreator;
		public bool enabled;



		public void PEGI(MaterialAtlases a){

			atlasedField.toggle ("Use this field", 50, ref enabled);


			if (enabled) {

				":".select (50, ref atlasCreator, PainterManager.inst.atlases);

				if (icon.Add.Click ("Create new Atlas",15).nl()) {
					atlasCreator = new AtlasTextureCreator (atlasedField);
					PainterManager.inst.atlases.Add (atlasCreator);

				}

				pegi.Space ();


				"From:".select (ref originField, a.originalTextures);

				if ((atlasedField != null) && (a.originalMaterial != null) && (atlasCreator != null) && (originField < a.originalTextures.Count)) {
					Texture t = a.originalMaterial.GetTexture (a.originalTextures [originField]);
					if ((t != null) && (t.GetType() == typeof(Texture2D)) && (atlasCreator.textures.Contains((Texture2D)t)))
						icon.Done.nl (10);
				}
			}
			pegi.newLine ();
			pegi.Space ();
			pegi.newLine ();
		}
    }

	[System.Serializable]
    public class MaterialAtlases  {
        //public static List<MaterialAtlases> all = new List<MaterialAtlases>();

		public string name;

		public override string ToString () {
			return name;
		}

        public Material originalMaterial;
        public Shader originalShader;
		public List<string> originalTextures;
		public Material AtlasedMaterial;
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
            painter.selectedMeshProfile = matAtlasProfile;

			painter.SetOriginalShader ();

			painter.UpdateOrSetTexTarget (texTarget.Texture2D);

            Material mat = painter.meshRenderer.sharedMaterial;
            List<string> tfields = mat.getTextures();

           
			int index = 0;
			List<FieldAtlas> passedFields = new List<FieldAtlas> ();
			List<Texture2D> passedTextures = new List<Texture2D> ();


			foreach (var f in fields)
				if ((f.enabled) && (f.atlasCreator != null) && (tfields.Contains (originalTextures[f.originField]))) {
				
					string original = originalTextures [f.originField];

					//Debug.Log ("passing " + f.atlasedField + " from " + originalTextures[f.originField]);


					Texture tex = mat.GetTexture (original);

					if (tex == null) {
						Debug.Log (painter.name + " no " + original + " texture.");
						return;
					}

					if (tex.GetType () != typeof(Texture2D)) {
						Debug.Log ("Not a Texture 2D: " + original);
						return;
					}

					Texture2D texture = (Texture2D)tex;
               
					List<Texture2D> aTexes = f.atlasCreator.textures;


					bool added = false;

					for (int i = index; i < aTexes.Count; i++)
						if ((aTexes [i] == null) || (aTexes [i] == texture)) {
							index = i;
							passedFields.Add (f);
							passedTextures.Add (texture);
							added = true;
							//Debug.Log ("Assigning index "+i);
							i = 999;
						}

					if (!added) {
						Debug.Log ("Could not find a place for "+original);
						return;
					}
						

			}


			if (passedFields.Count > 0) {
				
				painter.preAtlasingMaterial = painter.getMaterial (true);
				painter.preAtlasingMesh = painter.getMesh ();
				painter.inAtlasIndex = index;

				var MainField = passedFields [0];

				painter.atlasRows = MainField.atlasCreator.row;

				Vector2 tyling = mat.GetTextureScale(originalTextures [MainField.originField]);
				Vector2 offset = mat.GetTextureOffset(originalTextures [MainField.originField]);

				for(int i=0; i<passedFields.Count; i++){// var f in passedFields){
					var f = passedFields[i];
					var ac = f.atlasCreator;

					ac.textures [index] = passedTextures [i];
					ac.AddTargets (f,originalTextures [f.originField]);

					ac.ReconstructAsset();
					AtlasedMaterial.SetTexture (f.atlasedField, ac.a_texture);
				}

				MeshManager.inst.EditMesh(painter, true);

				if ((tyling != Vector2.one) || (offset != Vector2.zero)) {
					MeshManager.inst._Mesh.TileAndOffsetUVs (offset, tyling);
					Debug.Log ("offsetting "+offset + " tyling "+tyling);
				}

				painter.preAtlasingSavedMesh = MeshManager.inst._Mesh.Encode ().ToString ();

				VertexAtlasTool.inst.SetAllTrianglesTextureTo(index, 0);
				MeshManager.inst.Redraw();
				MeshManager.inst.DisconnectMesh ();

				AtlasedMaterial.SetFloat (PainterConfig.atlasedTexturesInARow , painter.atlasRows);
				painter.meshRenderer.sharedMaterial = AtlasedMaterial;
				AtlasedMaterial.EnableKeyword(PainterConfig.UV_ATLASED);
			}
#endif
        }


        public void FindAtlas(int field){
			foreach (var a in PainterManager.inst.atlases) {
				if (a.atlasFields.Contains(fields[field].atlasedField)) {
				for (int i=0; i< originalTextures.Count; i++){
					if (a.targetFields.Contains (originalTextures[i])) {
						fields [field].atlasCreator = a;
						Texture tex = originalMaterial.GetTexture (originalTextures[i]);
							if ((tex!= null) && (tex.GetType() == typeof(Texture2D)) && (a.textures.Contains((Texture2D)tex)))
						return;
					}
				}
				}
			}
		}


        public void OnChangeMaterial (PlaytimePainter painter){
#if UNITY_EDITOR
            if ((originalMaterial != null) && (AtlasedMaterial != null) &&
			    (originalMaterial == AtlasedMaterial))
				return;

			if (originalMaterial != null)
				originalTextures = originalMaterial.getTextures ();

			if (AtlasedMaterial != null) {
				List<string> aTextures = AtlasedMaterial.getTextures ();
				fields.Clear ();
				for (int i = 0; i < aTextures.Count; i++) {
					FieldAtlas ac = new FieldAtlas ();
					fields.Add(ac);
					ac.atlasedField = aTextures [i];
				}
				atlasedShader = AtlasedMaterial.shader;
			}

			if ((originalMaterial != null) && (AtlasedMaterial != null))
				for (int i = 0; i < fields.Count; i++)
					FindAtlas (i);
#endif
        }



        public void PEGI(PlaytimePainter painter) {

#if UNITY_EDITOR

            painter.SetOriginalShader ();

				Material mat = painter.getMaterial (false);

			if ((mat != originalMaterial) || ((mat!= null) && (mat.shader != originalShader))) {
				originalMaterial = mat;
                originalShader = mat.shader;
         
                OnChangeMaterial (painter);
			}

				foreach (var f in fields)
					f.PEGI (this);
			
			if (("Atlased Material:".edit (90, ref AtlasedMaterial).nl ()) || 
				(AtlasedMaterial!= null && AtlasedMaterial.shader != atlasedShader))
				OnChangeMaterial (painter);

			"Profile".select (50, ref matAtlasProfile, PainterConfig.inst.meshProfileSolutions).nl ();


			if (originalMaterial != null && AtlasedMaterial != null && "Convert to Atlased".Click ())
				ConvertToAtlased (painter);

#endif

        }

    }
}