using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using SharedTools_Stuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

    [Serializable]
    public class TileableAtlasingPainterPlugin : PainterPluginBase {

        public Material[] preAtlasingMaterials;
        public Mesh preAtlasingMesh;
        public string preAtlasingSavedMesh;
        public int inAtlasIndex;
        public int atlasRows = 1;

        public Vector2 GetAtlasedSection()  {
          
            float atY = inAtlasIndex / atlasRows;
            float atX = inAtlasIndex - atY * atlasRows;

            return new Vector2(atX, atY);
        }

        public override void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p)
        {
            BlitModeExtensions.SetShaderToggle(!p.isAtlased(), PainterConfig.UV_NORMAL, PainterConfig.UV_ATLASED);
        }

        public bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
            if (pntr.isAtlased()) {

                Vector2 uvCoords = stroke.uvFrom;

                Vector2 AtlasedSection = GetAtlasedSection();

                sectorSize = image.width / atlasRows;
                atlasSector.From(AtlasedSection * sectorSize);

                Blit_Functions.brAlpha = brushAlpha;

                Blit_Functions.half = (bc.Size(false)) / 2;
                int ihalf = Mathf.FloorToInt(Blit_Functions.half - 0.5f);

                bool smooth = bc.type(true) != BrushTypePixel.inst;

                if (smooth)
                    Blit_Functions._alphaMode = Blit_Functions.circleAlpha;
                else
                    Blit_Functions._alphaMode = Blit_Functions.noAlpha;

                Blit_Functions._blitMode = bc.blitMode.BlitFunctionTex2D;

                if (smooth) ihalf += 1;

                Blit_Functions.alpha = 1;

                Blit_Functions.r = bc.mask.GetFlag(BrushMask.R);
                Blit_Functions.g = bc.mask.GetFlag(BrushMask.G);
                Blit_Functions.b = bc.mask.GetFlag(BrushMask.B);
                Blit_Functions.a = bc.mask.GetFlag(BrushMask.A);

                Blit_Functions.csrc = bc.colorLinear.ToGamma();

                myIntVec2 tmp = image.uvToPixelNumber(uvCoords);//new myIntVec2 (pixIndex);

                int fromx = tmp.x - ihalf;

                tmp.y -= ihalf;


                var pixels = image.pixels;

                for (Blit_Functions.y = -ihalf; Blit_Functions.y < ihalf + 1; Blit_Functions.y++)
                {

                    tmp.x = fromx;

                    for (Blit_Functions.x = -ihalf; Blit_Functions.x < ihalf + 1; Blit_Functions.x++)
                    {

                        if (Blit_Functions._alphaMode())
                        {
                            int sx = tmp.x - atlasSector.x;
                            int sy = tmp.y - atlasSector.y;

                            sx %= sectorSize;
                            if (sx < 0)
                                sx += sectorSize;
                            sy %= sectorSize;
                            if (sy < 0)
                                sy += sectorSize;

                            Blit_Functions._blitMode(ref pixels[((atlasSector.y + sy)) * image.width + (atlasSector.x + sx)]);
                        }

                        tmp.x += 1;
                    }

                    tmp.y += 1;
                }
                return true;
            }

            return false;
        }

        public static myIntVec2 atlasSector = new myIntVec2();
        public static int sectorSize = 1;

        public override bool offsetAndTileUV(RaycastHit hit, PlaytimePainter p, ref Vector2 uv)
        {
            if (p.isAtlased()) {

                uv.x = uv.x % 1;
                uv.y = uv.y % 1;

                var m = p.getMesh();

                int vert = m.triangles[hit.triangleIndex * 3];
                List<Vector4> v4l = new List<Vector4>();
                m.GetUVs(0, v4l);
                if (v4l.Count > vert)
                    inAtlasIndex = (int)v4l[vert].z;

                atlasRows = Mathf.Max(atlasRows, 1);

                uv = (GetAtlasedSection() + uv) / (float)atlasRows;

                return true;
            }
            return false;
        }
#if PEGI
        public override bool BrushConfigPEGI()
        {
            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            bool changed = false;

            if (p.isAtlased()) {
                var m = p.GetMaterial(false);
                if (p.isOriginalShader) {
                    if (m.HasProperty(PainterConfig.atlasedTexturesInARow))
                        atlasRows = m.GetInt(PainterConfig.atlasedTexturesInARow);
                }

               ("Atlased Texture " + atlasRows + "*" + atlasRows).write("Shader has _ATLASED define");
                if ("Undo".Click(40).nl())
                   m.DisableKeyword(PainterConfig.UV_ATLASED);

                var id = p.imgData;

                if (id.TargetIsRenderTexture())
                    pegi.writeOneTimeHint("Watch out, Render Texture Brush can change neighboring textures on the Atlas.", "rtOnAtlas");

                if (!id.TargetIsTexture2D()) {
                    pegi.writeWarning("Render Texture painting does not yet support Atlas Editing");
                    pegi.newLine();
                }
            }
            return changed;
        }
#endif
        public override void BeforeGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type)
        {
            if (br.IsA3Dbrush(pntr) && pntr.isAtlased())
            {
                var ats = GetAtlasedSection();
                Shader.SetGlobalVector(PainterConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(ats.x, ats.y, atlasRows, 1));
            }
        }

        public override void AfterGPUStroke(PlaytimePainter p, BrushConfig br, StrokeVector st, BrushType type) {
            if (br.IsA3Dbrush(p) && p.isAtlased())
                Shader.SetGlobalVector(PainterConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }

      

    }

    public static class AtlasingExtensions {
        public static bool isAtlased(this PlaytimePainter p) {
            if (p == null) return false;
            var mat = p.GetMaterial(false);
            if (mat == null) return false;
            return p.GetMaterial(false).isAtlased(p.MaterialTexturePropertyName); }
        public static bool isProjected(this PlaytimePainter p) { return p.GetMaterial(false).isProjected(); }

        public static bool isAtlased(this Material mat, string property) {
            return mat.isAtlased() && mat.DisplayNameContains(property, PainterConfig.isAtlasableDisaplyNameTag);
        }

        public static bool isAtlased(this Material mat) {
            if (mat == null) return false;
            return mat.shaderKeywords.Contains(PainterConfig.UV_ATLASED);
        }

        public static bool Contains(this List<AtlasTextureField> lst, Texture2D tex) {
            foreach (var ef in lst)
                if ((ef != null) && (ef.texture != null) && (ef.texture == tex))
                    return false;
            return true;
        }
    }
    
    [System.Serializable]
    public class FieldAtlas
    {
        static PainterManager texMGMT { get { return PainterManager.inst; } }

        public string atlasedField;
        public int originField;
        public int atlasCreatorId;
        public bool enabled;
        public Color col;
        public AtlasTextureCreator atlasCreator { get { return TileableAtlasingControllerPlugin.inst.atlases.Count > atlasCreatorId ? TileableAtlasingControllerPlugin.inst.atlases[atlasCreatorId] : null; } }
#if PEGI
         
        [SerializeField]
        bool foldoutAtlas = false;
        public bool PEGI()
        {
            bool changed = false;

            MaterialAtlases a = MaterialAtlases.inspectedAtlas;

            changed |= atlasedField.toggle("Use this field", 50, ref enabled);

            if (enabled)
            {

                changed |=  pegi.select(ref originField, a.originalTextures).nl();

                pegi.Space();

                if (atlasCreator != null)
                    "Atlas".foldout(ref foldoutAtlas);

                else foldoutAtlas = false;

                if (!foldoutAtlas)
                {
                    if (atlasCreator != null)
                        changed |= "Color".toggle(35, ref atlasCreator.sRGB);

                    pegi.select(ref atlasCreatorId, TileableAtlasingControllerPlugin.inst.atlases);
                    if (icon.Add.Click("Create new Atlas", 15).nl())
                    {
                        atlasCreatorId = TileableAtlasingControllerPlugin.inst.atlases.Count;
                        var ac = new AtlasTextureCreator(atlasedField + " for " + a.name);
                        TileableAtlasingControllerPlugin.inst.atlases.Add(ac);
                        changed = true;
                    }
                }
                else changed |= atlasCreator.PEGI().nl();



                pegi.Space();



                if ((atlasedField != null) && (a.originalMaterial != null) && (atlasCreator != null) && (originField < a.originalTextures.Count))
                {
                    Texture t = a.originalMaterial.GetTexture(a.originalTextures[originField]);
                    if ((t != null) && t.GetType() == typeof(Texture2D)) //&& (atlasCreator.textures.Contains((Texture2D)t)))
                        icon.Done.write(25);
                    else "Will use Color".edit(ref col).nl();
                }
                else
                    "Color".edit("Color that will be used instead of a texture.", 35, ref col).nl();
                pegi.Space();


            }


            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            return changed;

        }
#endif
    }

    [System.Serializable]
    public class MaterialAtlases
        #if PEGI
        : iGotName, iPEGI
#endif
    {
        //public static List<MaterialAtlases> all = new List<MaterialAtlases>();

        public string name;

        public override string ToString()
        {
            return name;
        }

        public Material originalMaterial;
        public Shader originalShader;
        public List<string> originalTextures;
        public Material AtlasedMaterial;
        Material destinationMaterial { get { return AtlasedMaterial ? AtlasedMaterial : originalMaterial; } }

        public string NameForPEGI { get { return name; } set { name = value; } }

      
        public List<FieldAtlas> fields;
        public int matAtlasProfile;


        public MaterialAtlases()
        {
            if (fields == null)
                fields = new List<FieldAtlas>();
        }

        public MaterialAtlases(string nname)
        {
            name = nname;
            if ((name == null) || (name.Length == 0))
                name = "new";
            name = name.GetUniqueName(TileableAtlasingControllerPlugin.inst.atlasedMaterials);

            fields = new List<FieldAtlas>();
        }


        public void ConvertToAtlased(PlaytimePainter painter)
        {
#if UNITY_EDITOR

         


            if (AtlasedMaterial == null)
                AtlasedMaterial = painter.InstantiateMaterial(true);

            painter.SetOriginalShaderOnThis();

            painter.UpdateOrSetTexTarget(texTarget.Texture2D);

            Material mat = painter.GetMaterial(false);
            List<string> tfields = mat.getTextures();

            int index = 0;
            List<FieldAtlas> passedFields = new List<FieldAtlas>();
            List<Texture2D> passedTextures = new List<Texture2D>();
            List<Color> passedColors = new List<Color>();

            foreach (var f in fields)
                if ((f.enabled) && (f.atlasCreator != null) && (tfields.Contains(originalTextures[f.originField])))
                {

                    string original = originalTextures[f.originField];

                    Texture tex = mat.GetTexture(original);

                    Texture2D texture = null;

                    if (tex == null)
                    {
                        var note = painter.name + " no " + original + " texture. Using Color.";
#if PEGI
                        note.showNotification();
#endif
                        Debug.Log(note);
                    }
                    else
                    {

                        if (tex.GetType() != typeof(Texture2D))
                        {
                            Debug.Log("Not a Texture 2D: " + original);
                            return;
                        }

                        texture = (Texture2D)tex;

                    }

                    var aTexes = f.atlasCreator.textures;

                    bool added = false;

                    for (int i = index; i < aTexes.Count; i++)
                        if ((aTexes[i] == null) || (!aTexes[i].used) || (aTexes[i].texture == texture))
                        {
                            index = i;
                            passedFields.Add(f);
                            passedTextures.Add(texture);
                            passedColors.Add(f.col);
                            added = true;
                            break;
                        }

                    if (!added)
                    {
                        Debug.Log("Could not find a place for " + original);
                        return;
                    }
                }

            if (passedFields.Count > 0)
            {

                bool firstAtlasing = false;

                var atlPlug = painter.getPlugin<TileableAtlasingPainterPlugin>();

                if (atlPlug.preAtlasingMaterials == null)
                {
                    atlPlug.preAtlasingMaterials = painter.GetMaterials();
                    atlPlug.preAtlasingMesh = painter.getMesh();
                    firstAtlasing = true;
                }

                var MainField = passedFields[0];

                atlPlug.atlasRows = MainField.atlasCreator.row;

                Vector2 tyling = mat.GetTextureScale(originalTextures[MainField.originField]);
                Vector2 offset = mat.GetTextureOffset(originalTextures[MainField.originField]);

                for (int i = 0; i < passedFields.Count; i++)
                {// var f in passedFields){
                    var f = passedFields[i];
                    var ac = f.atlasCreator;

                    ac.textures[index] = new AtlasTextureField(passedTextures[i], passedColors[i]);

                    ac.AddTargets(f, originalTextures[f.originField]);
                    ac.ReconstructAsset();
                    AtlasedMaterial.SetTexture(f.atlasedField, ac.a_texture);
                }

                MeshManager.inst.EditMesh(painter, true);

                if (firstAtlasing)
                    atlPlug.preAtlasingSavedMesh = MeshManager.inst.edMesh.Encode().ToString();

                painter.selectedMeshProfile = matAtlasProfile;

                if ((tyling != Vector2.one) || (offset != Vector2.zero))
                {
                    MeshManager.inst.edMesh.TileAndOffsetUVs(offset, tyling, 0);
                    Debug.Log("offsetting " + offset + " tyling " + tyling);
                }

                TriangleAtlasTool.inst.SetAllTrianglesTextureTo(index, 0, painter.selectedSubmesh);
                MeshManager.inst.Redraw();
                MeshManager.inst.DisconnectMesh();

                AtlasedMaterial.SetFloat(PainterConfig.atlasedTexturesInARow, atlPlug.atlasRows);
                painter.material = AtlasedMaterial;

                if (firstAtlasing)
                {
                    var m = painter.getMesh();
                    m.name = m.name + "_Atlased_" + index;
                }
                //painter.getMaterial(false).SetTextureOffset(1,Vector2.zero);

                AtlasedMaterial.EnableKeyword(PainterConfig.UV_ATLASED);

            }
#endif
                    }


                    public void FindAtlas(int field)
        {
            var texMGMT = PainterManager.inst;

            for (int a = 0; a < TileableAtlasingControllerPlugin.inst.atlases.Count; a++)
            {
                var atl = TileableAtlasingControllerPlugin.inst.atlases[a];
                if (atl.atlasFields.Contains(fields[field].atlasedField))
                {
                    for (int i = 0; i < originalTextures.Count; i++)
                    {
                        if (atl.targetFields.Contains(originalTextures[i]))
                        {
                            fields[field].atlasCreatorId = a;
                            Texture tex = originalMaterial.GetTexture(originalTextures[i]);
                            if ((tex != null) && (tex.GetType() == typeof(Texture2D)) && (atl.textures.Contains((Texture2D)tex)))
                                return;
                        }
                    }
                }
            }
        }


        public void OnChangeMaterial(PlaytimePainter painter)
        {
#if UNITY_EDITOR

            if (originalMaterial != null)
                originalTextures = originalMaterial.getTextures();

            if ((destinationMaterial != null) && (destinationMaterial.HasProperty(PainterConfig.isAtlasedProperty)))
            {
                List<string> aTextures = destinationMaterial.getTextures();
                fields.Clear();
                for (int i = 0; i < aTextures.Count; i++)
                {
                    FieldAtlas ac = new FieldAtlas();
                    fields.Add(ac);
                    ac.atlasedField = aTextures[i];
                }

#if PEGI
                atlasedShader = destinationMaterial.shader;
#endif

                foreach (var p in MaterialEditor.GetMaterialProperties(new Material[] { destinationMaterial }))
                    if (p.displayName.Contains(PainterConfig.isAtlasableDisaplyNameTag))
                        foreach (var f in fields)
                            if (f.atlasedField.SameAs(p.name))
                            {
                                f.enabled = true;
                                continue;
                            }

                if (AtlasedMaterial == null)
                    for (int i = 0; i < fields.Count; i++)
                        fields[i].originField = i;
                else if (originalMaterial != null)
                {
                    var orTexs = originalMaterial.getTextures();
                    foreach (var f in fields)
                        for (int i = 0; i < orTexs.Count; i++)
                            if (orTexs[i].SameAs(f.atlasedField))
                                f.originField = i;


                }
            }

            if (originalMaterial != null)
                for (int i = 0; i < fields.Count; i++)
                    FindAtlas(i);
#endif
            }

#if PEGI
        Shader atlasedShader;
        public static MaterialAtlases inspectedAtlas;
        [SerializeField]
        private bool showHint;
        public bool PEGI()
        {
            bool changed = false;

#if UNITY_EDITOR
            var painter = PlaytimePainter.inspectedPainter;
            inspectedAtlas = this;


            painter.SetOriginalShaderOnThis();

            Material mat = painter.GetMaterial(false);

            if ((mat != null) && ((mat != originalMaterial) || mat.shader != originalShader))
            {
                originalMaterial = mat;
                originalShader = mat.shader;
                OnChangeMaterial(painter);
            }
            changed |= "Name".edit(50, ref name).nl();
            if ("Hint".foldout(ref showHint).nl())
            {

                ("If you don't set Atlased Material(Destination)  it will try to create a copy of current material and set isAtlased toggle on it, if it has one." +
                    " Below you can see: list of Texture Properties, for each you can select or create an atlas. Atlas is a class that holds all textures assigned to an atlas, and also creates and stores the atlas itself." +
                    "After this you can select a field from current Material, texture of which will be copied into an atlas. A bit confusing, I know)" +
                    "Also if stuff looks smudged, rebuild the light.").writeHint();
            }

            if (("Atlased Material:".edit(90, ref AtlasedMaterial).nl()) ||
                (AtlasedMaterial != null && AtlasedMaterial.shader != atlasedShader))
            {
                OnChangeMaterial(painter);
                changed = true;
            }

            if (painter != null)
            {
                var mats = painter.GetMaterials();
                if (mats != null)
                {
                    if (mats.Length > 1)
                    {
                        if ("Source Material:".select("Same as selecting a submesh, which will be converted", 90, ref painter.selectedSubmesh, mats))
                        {
                            changed = true;
                            OnChangeMaterial(painter);
                        }
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
                changed |= f.PEGI();

            changed |= "Mesh Profile".select(110, ref matAtlasProfile, PainterConfig.inst.meshPackagingSolutions).nl();

            if ((destinationMaterial != null) && (!destinationMaterial.HasProperty(PainterConfig.isAtlasedProperty)))
            {
                if (AtlasedMaterial == null) pegi.writeHint("Original Material doesn't have isAtlased property, change shader or add Destination Atlased Material");
                else pegi.writeHint("Atlased Material doesn't have isAtlased property");
            }
            else if (originalMaterial != null)
            {

                string names = "";
                foreach (var f in fields)
                    if (f.enabled && f.atlasCreator == null) names += f.atlasedField + ", ";

                if (names.Length > 0)
                    pegi.writeHint("Fields " + names + " don't have atlases assigned to them, create some");
                else if ("Convert to Atlased".Click())
                    ConvertToAtlased(painter);
            }
#endif

            inspectedAtlas = null;
            
            return changed;

        }
#endif
    }
    
    [Serializable]
    public class AtlasTextureField
    {
        public Texture2D texture;
        public Color color;
        public bool used;

        public AtlasTextureField(Texture2D tex, Color col)
        {
            texture = tex;
            color = col;
            used = true;
        }

    }

    [Serializable]
    public class AtlasTextureCreator
        #if PEGI
        : iGotName, iPEGI
        #endif
    {

        static PainterConfig cfg { get { return PainterConfig.inst; } }

        public int AtlasSize = 2048;

        public int textureSize = 512;

        public bool sRGB = true;
        
        public string NameForPEGI { get; set;}

        public List<string> targetFields;

        public List<string> atlasFields;

        public Texture2D a_texture;

        public List<AtlasTextureField> textures;

        public int row { get { return AtlasSize / textureSize; } }

        public void AddTargets(FieldAtlas at, string target)
        {
            if (!atlasFields.Contains(at.atlasedField))
                atlasFields.Add(at.atlasedField);
            if (!targetFields.Contains(target))
                targetFields.Add(target);
        }
        
        public override string ToString()
        {
            return NameForPEGI;
        }

        void Init()
        {
            if (targetFields == null)
                targetFields = new List<string>();
            if (atlasFields == null)
                atlasFields = new List<string>();
            if (textures == null)
                textures = new List<AtlasTextureField>();


            adjustListSize();
        }

        public AtlasTextureCreator()
        {
            Init();
        }
        
        public AtlasTextureCreator(string nname)
        {
            NameForPEGI = nname;
            NameForPEGI = NameForPEGI.GetUniqueName(TileableAtlasingControllerPlugin.inst.atlases);
            Init();
        }

        public void adjustListSize()
        {
            int ntc = TextureCount;
            while (textures.Count < ntc)
                textures.Add(new AtlasTextureField(null, Color.gray));
        }

        public int TextureCount
        {
            get { int r = row; return r * r; }
        }

        public void ColorToAtlas(Color col, int x, int y)
        {
            int size = textureSize * textureSize;
            Color[] pix = new Color[size];
            for (int i = 0; i < size; i++)
                pix[i] = col;

            a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, pix);
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

                    Color tmpC = tmp.ToGamma();


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
                        tmpC = tmp.ToGamma();
                        col[startY + x] = tmpC;
                        col[lastY + x] = tmpC;
                    }

                    for (int y = startY + aSize; y < lastY; y += aSize)
                    {
                        tmp.Zero();
                        tmp.Add(col[y + startX]);
                        tmp.Add(col[y + lastX]);
                        tmp.MultiplyBy(0.5f);
                        tmpC = tmp.ToGamma();
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
                for (int x = 0; x < texesInRow; x++)
                {
                    var t = textures[curIndex];
                    if ((textures.Count > curIndex) && (t != null) && (t.used))
                    {
                        if (t.texture != null)
                        {
#if UNITY_EDITOR
                            t.texture.Reimport_IfNotReadale();
#endif

                            Color[] from = t.texture.GetPixels(textureSize, textureSize);

                            a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, from);

                            // TextureToAtlas(t.texture, x, y);
                        }
                        else
                            ColorToAtlas(t.color, x, y);
                    }
                    else
                        ColorToAtlas(defaltCol, x, y);

                    curIndex++;
                }

        }

        public List<string> srcFields = new List<string>();
        
#if UNITY_EDITOR
        public void ReconstructAsset()
        {

            ReconstructAtlas();

            for (int m = 0; m < a_texture.mipmapCount; m++)
                smoothBorders(a_texture, m);

            a_texture.Apply(false);

            byte[] bytes = a_texture.EncodeToPNG();

            string lastPart = cfg.texturesFolderName.AddPreSlashIfNotEmpty() + cfg.atlasFolderName.AddPreSlashIfNotEmpty() + "/";
            string fullPath = Application.dataPath + lastPart;
            Directory.CreateDirectory(fullPath);

            string fileName = NameForPEGI + ".png";
            string relativePath = "Assets" + lastPart + fileName;
            fullPath += fileName;

            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh(); // few times caused color of the texture to get updated to earlier state for some reason

            a_texture = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

            TextureImporter other = null;

            foreach (var t in textures)
                if ((t != null) && (t.texture != null))
                {
                    other = t.texture.getTextureImporter();
                    break;
                }

            TextureImporter ti = a_texture.getTextureImporter();
            bool needReimport = ti.wasNotReadable();
            if (other != null)
                needReimport |= ti.wasWrongIsColor(other.sRGBTexture);
            needReimport |= ti.wasClamped();

            if (needReimport) ti.SaveAndReimport();

        }
#endif

#if PEGI

        public bool PEGI()
        {
            bool changed = false;
#if UNITY_EDITOR


            this.PEGI_Name().nl();
           // changed |= "Name:".edit(60, ref name).nl();

            changed |= "Atlas size:".editDelayed( ref AtlasSize, 80).nl();
            AtlasSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(AtlasSize, 512, 4096));

            if ("Textures size:".editDelayed( ref textureSize, 80).nl())
            {
                pegi.foldIn();
                changed = true;

            }

            textureSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(textureSize, 32, AtlasSize / 2));

            adjustListSize();

            if ("Textures:".foldout().nl())
            {
                adjustListSize();
                int max = TextureCount;

                for (int i = 0; i < max; i++)
                {
                    var t = textures[i];

                    if (t.used)
                    {
                        pegi.edit(ref t.texture);
                        if (t.texture == null)
                            pegi.edit(ref t.color);
                        pegi.newLine();
                    }
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
#endif
    }

}