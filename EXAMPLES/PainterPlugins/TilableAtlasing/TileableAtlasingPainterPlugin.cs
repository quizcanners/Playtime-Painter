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

namespace Playtime_Painter.Examples {

    [TaggedType(tag)]
    public class TileableAtlasingPainterPlugin : PainterComponentPluginBase {

        const string tag = "TilAtlsPntr";
        public override string ClassTag => tag;

        public List<Material> preAtlasingMaterials;
        public Mesh preAtlasingMesh;
        public string preAtlasingSavedMesh;
        public int inAtlasIndex;
        public int atlasRows = 1;

        public static MyIntVec2 atlasSector = new MyIntVec2();
        public static int sectorSize = 1;

        #region Encode & Decode

        public override StdEncoder Encode() => new StdEncoder()
            .Add_References("pam", preAtlasingMaterials)
            .Add_Reference("pamsh", preAtlasingMesh)
            .Add_String("sm", preAtlasingSavedMesh)
            .Add("iai", inAtlasIndex)
            .Add("ar", atlasRows);

        public override bool Decode(string tag, string data)
        {
            switch (tag) {
                case "pam": data.Decode_References(out preAtlasingMaterials); break;
                case "pamsh": data.Decode_Reference(ref preAtlasingMesh); break;
                case "sm": preAtlasingSavedMesh = data; break;
                case "iai": inAtlasIndex = data.ToInt(); break;
                case "ar": atlasRows = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        public Vector2 GetAtlasedSection()  {
          
            float atY = inAtlasIndex / atlasRows;
            float atX = inAtlasIndex - atY * atlasRows;

            return new Vector2(atX, atY);
        }

        public override void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p) =>
            UnityHelperFunctions.ToggleShaderKeywords(!p.IsAtlased(), PainterDataAndConfig.UV_NORMAL, PainterDataAndConfig.UV_ATLASED);
        
        public bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
            if (pntr.IsAtlased()) {

                Vector2 uvCoords = stroke.uvFrom;

                Vector2 AtlasedSection = GetAtlasedSection();

                sectorSize = image.width / atlasRows;
                atlasSector.From(AtlasedSection * sectorSize);

                Blit_Functions.brAlpha = brushAlpha;

                Blit_Functions.half = (bc.Size(false)) / 2;
                int ihalf = Mathf.FloorToInt(Blit_Functions.half - 0.5f);

                bool smooth = bc.Type(true) != BrushTypePixel.Inst;

                if (smooth)
                    Blit_Functions._alphaMode = Blit_Functions.circleAlpha;
                else
                    Blit_Functions._alphaMode = Blit_Functions.noAlpha;

                Blit_Functions._blitMode = bc.BlitMode.BlitFunctionTex2D(image);

                if (smooth) ihalf += 1;

                Blit_Functions.alpha = 1;

                Blit_Functions.r = bc.mask.GetFlag(BrushMask.R);
                Blit_Functions.g = bc.mask.GetFlag(BrushMask.G);
                Blit_Functions.b = bc.mask.GetFlag(BrushMask.B);
                Blit_Functions.a = bc.mask.GetFlag(BrushMask.A);

                Blit_Functions.csrc = bc.colorLinear.ToGamma();

                MyIntVec2 tmp = image.UvToPixelNumber(uvCoords);//new myIntVec2 (pixIndex);

                int fromx = tmp.x - ihalf;

                tmp.y -= ihalf;


                var pixels = image.Pixels;

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
        
        public override bool OffsetAndTileUV(RaycastHit hit, PlaytimePainter p, ref Vector2 uv)
        {
            if (p.IsAtlased()) {

                uv.x = uv.x % 1;
                uv.y = uv.y % 1;

                var m = p.GetMesh();

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

        #region Inspector
        #if PEGI
        public override bool BrushConfigPEGI()
        {
            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            bool changed = false;

            if (p.IsAtlased()) {
                var m = p.Material;
                if (p.IsOriginalShader) {
                    if (m.HasProperty(PainterDataAndConfig.atlasedTexturesInARow))
                        atlasRows = m.GetInt(PainterDataAndConfig.atlasedTexturesInARow);
                }

               ("Atlased Texture " + atlasRows + "*" + atlasRows).write("Shader has _ATLASED define");
                if ("Undo".Click(40).nl())
                   m.DisableKeyword(PainterDataAndConfig.UV_ATLASED);

                var id = p.ImgData;

                if (id.TargetIsRenderTexture())
                    pegi.writeOneTimeHint("Watch out, Render Texture Brush can change neighboring textures on the Atlas.", "rtOnAtlas");

                if (!id.TargetIsTexture2D()) {
                    pegi.writeWarning("Render Texture painting does not yet support Atlas Editing");
                    pegi.nl();
                }
            }
            return changed;
        }
        #endif
        #endregion

        public override void BeforeGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type)
        {
            if (br.IsA3Dbrush(pntr) && pntr.IsAtlased())
            {
                var ats = GetAtlasedSection();
                Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(ats.x, ats.y, atlasRows, 1));
            }
        }

        public override void AfterGPUStroke(PlaytimePainter p, BrushConfig br, StrokeVector st, BrushType type) {
            if (br.IsA3Dbrush(p) && p.IsAtlased())
                Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }
    }
    
    public class FieldAtlas : AbstractKeepUnrecognized_STD, IPEGI
    {
        static PainterCamera TexMGMT => PainterCamera.Inst; 

        public string atlasedField;
        public int originField;
        public int atlasCreatorId;
        public bool enabled;
        public Color col;
        public AtlasTextureCreator AtlasCreator => TileableAtlasingControllerPlugin.inst.atlases.Count > atlasCreatorId ? TileableAtlasingControllerPlugin.inst.atlases[atlasCreatorId] : null; 

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("af", atlasedField)
            .Add("of", originField)
            .Add("acid", atlasCreatorId)
            .Add_Bool("e", enabled)
            .Add("c", col);

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "af": atlasedField = data; break;
                case "of": originField = data.ToInt(); break;
                case "acid": atlasCreatorId = data.ToInt(); break;
                case "e": enabled = data.ToBool(); break;
                case "c": col = data.ToColor(); break;
            default: return false;
        }
        return true;
        }

        #endregion

        #region Inspector
#if PEGI
        public override bool Inspect() {
            bool changed = false;

            MaterialAtlases a = MaterialAtlases.inspectedAtlas;

            atlasedField.toggleIcon(ref enabled).nl(ref changed);

            if (enabled) {

                pegi.select(ref originField, a.originalTextures).nl(ref changed);

                pegi.Space();

                "Atlas".enter_Inspect(AtlasCreator, ref inspectedStuff, 11).nl(ref changed);

                if (inspectedStuff == -1) {
                    "Atlases".select(70, ref atlasCreatorId, TileableAtlasingControllerPlugin.inst.atlases).changes(ref changed);
                    if (icon.Add.Click("Create new Atlas").nl(ref changed)) {
                        atlasCreatorId = TileableAtlasingControllerPlugin.inst.atlases.Count;
                        var ac = new AtlasTextureCreator(atlasedField + " for " + a.name);
                        TileableAtlasingControllerPlugin.inst.atlases.Add(ac);
                    }
                }

                if ((atlasedField != null) && (a.originalMaterial) && (AtlasCreator != null) && (originField < a.originalTextures.Count))
                {
                    Texture t = a.originalMaterial.GetTexture(a.originalTextures[originField]);
                    if ((t) && t.GetType() == typeof(Texture2D))
                        icon.Done.write();
                    else "Will use Color".edit(ref col).nl();
                }
                else
                    "Color".edit("Color that will be used instead of a texture.", 35, ref col).nl();
            }

            return changed;

        }
#endif
        #endregion
    }
    
    public class MaterialAtlases : AbstractKeepUnrecognized_STD, IGotName, IPEGI {

        public string name;

        public Material originalMaterial;
        public Shader originalShader;
        public List<string> originalTextures;
        public Material AtlasedMaterial;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("n", name)
            .Add_Reference("om", originalMaterial)
            .Add("ot", originalTextures)
            .Add_Reference("am", AtlasedMaterial);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "om": data.Decode_Reference(ref originalMaterial); break;
                case "ot": data.Decode_List(out originalTextures); break;
                case "am": data.Decode_Reference(ref AtlasedMaterial); break;
            default: return false;
            }
            return true;
        }

        #endregion

        Material DestinationMaterial { get { return AtlasedMaterial ? AtlasedMaterial : originalMaterial; } }

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

            if (!AtlasedMaterial)
                AtlasedMaterial = painter.InstantiateMaterial(true);

            painter.SetOriginalShaderOnThis();

            painter.UpdateOrSetTexTarget(TexTarget.Texture2D);

            Material mat = painter.Material;
            List<string> tfields = mat.MyGetTextureProperties();

            int index = 0;
            List<FieldAtlas> passedFields = new List<FieldAtlas>();
            List<Texture2D> passedTextures = new List<Texture2D>();
            List<Color> passedColors = new List<Color>();

            foreach (var f in fields)
                if ((f.enabled) && (f.AtlasCreator != null) && (tfields.Contains(originalTextures[f.originField])))
                {

                    string original = originalTextures[f.originField];

                    Texture tex = mat.GetTexture(original);

                    Texture2D texture = null;

                    if (!tex)
                    {
                        var note = painter.name + " no " + original + " texture. Using Color.";
#if PEGI
                        note.showNotificationIn3D_Views();
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

                    var aTexes = f.AtlasCreator.textures;

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

                var atlPlug = painter.GetPlugin<TileableAtlasingPainterPlugin>();

                if (atlPlug.preAtlasingMaterials == null)
                {
                    atlPlug.preAtlasingMaterials = painter.GetMaterials().ToList();
                    atlPlug.preAtlasingMesh = painter.GetMesh();
                    firstAtlasing = true;
                }

                var MainField = passedFields[0];

                atlPlug.atlasRows = MainField.AtlasCreator.Row;

                Vector2 tyling = mat.GetTextureScale(originalTextures[MainField.originField]);
                Vector2 offset = mat.GetTextureOffset(originalTextures[MainField.originField]);

                for (int i = 0; i < passedFields.Count; i++) {

                    var f = passedFields[i];
                    var ac = f.AtlasCreator;

                    ac.textures[index] = new AtlasTextureField(passedTextures[i], passedColors[i]);

                    ac.AddTargets(f, originalTextures[f.originField]);
                    ac.ReconstructAsset();
                    AtlasedMaterial.SetTexture(f.atlasedField, ac.a_texture);
                }

                MeshManager.Inst.EditMesh(painter, true);

                if (firstAtlasing)
                    atlPlug.preAtlasingSavedMesh = MeshManager.Inst.edMesh.Encode().ToString();

                painter.selectedMeshProfile = matAtlasProfile;

                if ((tyling != Vector2.one) || (offset != Vector2.zero))
                {
                    MeshManager.Inst.edMesh.TileAndOffsetUVs(offset, tyling, 0);
                    Debug.Log("offsetting " + offset + " tyling " + tyling);
                }

                TriangleAtlasTool.Inst.SetAllTrianglesTextureTo(index, 0, painter.selectedSubmesh);
                MeshManager.Inst.Redraw();
                MeshManager.Inst.DisconnectMesh();

                AtlasedMaterial.SetFloat(PainterDataAndConfig.atlasedTexturesInARow, atlPlug.atlasRows);
                painter.Material = AtlasedMaterial;

                if (firstAtlasing)
                {
                    var m = painter.GetMesh();
                    m.name = m.name + "_Atlased_" + index;
                }
 
                AtlasedMaterial.EnableKeyword(PainterDataAndConfig.UV_ATLASED);

            }
#endif
                    }

        public void FindAtlas(int field)
        {
            var texMGMT = PainterCamera.Inst;

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
                            if ((tex) && (tex.GetType() == typeof(Texture2D)) && (atl.textures.Contains((Texture2D)tex)))
                                return;
                        }
                    }
                }
            }
        }

        public void OnChangeMaterial(PlaytimePainter painter)
        {
#if UNITY_EDITOR

            if (originalMaterial)
                originalTextures = originalMaterial.MyGetTextureProperties();

            if ((DestinationMaterial) && (DestinationMaterial.HasProperty(PainterDataAndConfig.isAtlasedProperty)))
            {
                List<string> aTextures = DestinationMaterial.MyGetTextureProperties();
                fields.Clear();
                for (int i = 0; i < aTextures.Count; i++)
                {
                    FieldAtlas ac = new FieldAtlas();
                    fields.Add(ac);
                    ac.atlasedField = aTextures[i];
                }

#if PEGI
                atlasedShader = DestinationMaterial.shader;
#endif

                foreach (var p in MaterialEditor.GetMaterialProperties(new Material[] { DestinationMaterial }))
                    if (p.displayName.Contains(PainterDataAndConfig.isAtlasableDisaplyNameTag))
                        foreach (var f in fields)
                            if (f.atlasedField.SameAs(p.name))
                            {
                                f.enabled = true;
                                continue;
                            }

                if (!AtlasedMaterial)
                    for (int i = 0; i < fields.Count; i++)
                        fields[i].originField = i;
                else if (originalMaterial)
                {
                    var orTexs = originalMaterial.MyGetTextureProperties();
                    foreach (var f in fields)
                        for (int i = 0; i < orTexs.Count; i++)
                            if (orTexs[i].SameAs(f.atlasedField))
                                f.originField = i;


                }
            }

            if (originalMaterial)
                for (int i = 0; i < fields.Count; i++)
                    FindAtlas(i);
#endif
            }

        #region Inspector
        #if PEGI
        Shader atlasedShader;
        public static MaterialAtlases inspectedAtlas;
        private bool showHint;
        public override bool Inspect()
        {
            bool changed = false;

#if UNITY_EDITOR
            var painter = PlaytimePainter.inspectedPainter;
            inspectedAtlas = this;


            painter.SetOriginalShaderOnThis();

            Material mat = painter.Material;

            if ((mat) && ((mat != originalMaterial) || mat.shader != originalShader))
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

            if ((("Atlased Material:".edit(90, ref AtlasedMaterial).nl()) ||
                (AtlasedMaterial && AtlasedMaterial.shader != atlasedShader)).changes(ref changed)) 
                OnChangeMaterial(painter);
            

            if (painter)
            {
                var mats = painter.GetMaterials();
                if (mats != null)
                {
                    if (mats.Length > 1)
                    {
                        if ("Source Material:".select("Same as selecting a submesh, which will be converted", 90, ref painter.selectedSubmesh, mats).changes(ref changed))
                            OnChangeMaterial(painter);
                    }
                    else if (mats.Length > 0)
                        "Source Material".write_obj("Submesh which will be converted", 90, mats[0]);
                }
                pegi.nl();
                pegi.Space();
                pegi.nl();
            }



            pegi.Space();
            pegi.nl();

            foreach (var f in fields)
                changed |= f.Nested_Inspect();

            changed |= "Mesh Profile".select(110, ref matAtlasProfile, PainterCamera.Data.meshPackagingSolutions).nl();

            if (DestinationMaterial && !DestinationMaterial.HasProperty(PainterDataAndConfig.isAtlasedProperty))
            {
                if (!AtlasedMaterial) pegi.writeHint("Original Material doesn't have isAtlased property, change shader or add Destination Atlased Material");
                else pegi.writeHint("Atlased Material doesn't have isAtlased property");
            }
            else if (originalMaterial)
            {

                string names = "";
                foreach (var f in fields)
                    if (f.enabled && f.AtlasCreator == null) names += f.atlasedField + ", ";

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
        #endregion
    }
    
    public class AtlasTextureField: AbstractKeepUnrecognized_STD, IPEGI_ListInspect
    {
        public Texture2D texture;
        public Color color = Color.black;
        public bool used;

        public AtlasTextureField() { used = true; }

        public AtlasTextureField(Texture2D tex, Color col)
        {
            texture = tex;
            color = col;
            used = true;
        }

        #region Inspector
        #if PEGI
        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            (used ? icon.Active : icon.InActive).write();

            var changed = false;
            pegi.edit(ref texture).changes(ref changed);
            if (!texture)
            pegi.edit(ref color).changes(ref changed);

            return changed;
        }
        #endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("tex", texture)
            .Add("col", color)
            .Add_IfTrue("u", used);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "tex": data.Decode_Reference(ref texture); break;
                case "col": color = data.ToColor(); break;
                case "u": used = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }
    
    public class AtlasTextureCreator : AbstractKeepUnrecognized_STD, IGotName, IPEGI
    {

        static PainterDataAndConfig Cfg => PainterCamera.Data;

        int AtlasSize = 2048;

        int textureSize = 512;

        public bool sRGB = true;
        
        public string NameForPEGI { get; set;}

        public List<string> targetFields;

        public List<string> atlasFields;

        List<string> srcFields = new List<string>();

        public Texture2D a_texture;

        public List<AtlasTextureField> textures;
        List_Data texturesMeta = new List_Data("Textures", enterIcon: icon.Painter);

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("tf", targetFields)
            .Add("af", atlasFields)
            .Add("sf", srcFields)
            .Add_Reference("atex", a_texture)
            .Add("txs", textures, texturesMeta)
            .Add_String("n", NameForPEGI)
            .Add_Bool("rgb", sRGB)
            .Add("s", textureSize)
            .Add("as", AtlasSize);

        public override bool Decode(string tag, string data)
        {
            switch (tag) {
                case "tf": data.Decode_List(out targetFields); break;
                case "af": data.Decode_List(out atlasFields); break;
                case "sf": data.Decode_List(out srcFields); break;
                case "atex": data.Decode_Reference(ref a_texture); break;
                case "txs": data.Decode_List(out textures, ref texturesMeta); break;
                case "n": NameForPEGI = data; break;
                case "rgb": sRGB = data.ToBool(); break;
                case "s": textureSize = data.ToInt(); break;
                case "as": AtlasSize = data.ToInt(); break;
            default: return false;
            }
            return true;
        }

        #endregion

        public int Row { get { return AtlasSize / textureSize; } }

        public void AddTargets(FieldAtlas at, string target)
        {
            if (!atlasFields.Contains(at.atlasedField))
                atlasFields.Add(at.atlasedField);
            if (!targetFields.Contains(target))
                targetFields.Add(target);
        }
        
        void Init()
        {
            if (targetFields == null)
                targetFields = new List<string>();
            if (atlasFields == null)
                atlasFields = new List<string>();
            if (textures == null)
                textures = new List<AtlasTextureField>();


            AdjustListSize();
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

        public void AdjustListSize()
        {
            int ntc = TextureCount;
            while (textures.Count < ntc)
                textures.Add(new AtlasTextureField(null, Color.gray));
        }

        public int TextureCount
        {
            get { int r = Row; return r * r; }
        }

        public void ColorToAtlas(Color col, int x, int y)
        {
            int size = textureSize * textureSize;
            Color[] pix = new Color[size];
            for (int i = 0; i < size; i++)
                pix[i] = col;

            a_texture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, pix);
        }
        
        public void SmoothBorders(Texture2D atlas, int miplevel)
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

            LinearColor tmp = new LinearColor();


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

            if ((a_texture) && (a_texture.width != AtlasSize))
            {
                GameObject.DestroyImmediate(a_texture);
                a_texture = null;
            }

            if (!a_texture)
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
                        if (t.texture)
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


#if UNITY_EDITOR
        public void ReconstructAsset()
        {

            ReconstructAtlas();

            for (int m = 0; m < a_texture.mipmapCount; m++)
                SmoothBorders(a_texture, m);

            a_texture.Apply(false);

            byte[] bytes = a_texture.EncodeToPNG();

            string lastPart = Cfg.texturesFolderName.AddPreSlashIfNotEmpty() + Cfg.atlasFolderName.AddPreSlashIfNotEmpty() + "/";
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
                if ((t != null) && (t.texture))
                {
                    other = t.texture.GetTextureImporter();
                    break;
                }

            TextureImporter ti = a_texture.GetTextureImporter();
            bool needReimport = ti.WasNotReadable();
            if (other != null)
                needReimport |= ti.WasWrongIsColor(other.sRGBTexture);
            needReimport |= ti.WasClamped();

            if (needReimport) ti.SaveAndReimport();

        }
#endif

#if PEGI

        public override bool Inspect() {
            bool changed = false;
#if UNITY_EDITOR

            if (inspectedStuff == -1) {

                "Atlas size:".editDelayed(ref AtlasSize, 80).nl(ref changed);
                    AtlasSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(AtlasSize, 512, 4096));

                if ("Textures size:".editDelayed(ref textureSize, 80).nl(ref changed))

                textureSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(textureSize, 32, AtlasSize / 2));

                AdjustListSize();
            }

            texturesMeta.enter_List(ref textures, ref inspectedStuff, 11);

            if ("Textures:".foldout().nl()) {
                AdjustListSize();
                int max = TextureCount;

                for (int i = 0; i < max; i++) {
                    var t = textures[i];

                    if (t.used) {
                        pegi.edit(ref t.texture);
                        if (!t.texture)
                            pegi.edit(ref t.color);
                        pegi.newLine();
                    }
                }
            }

            pegi.newLine();
            "Is Color Atlas:".toggle(80, ref sRGB).nl();

            if ("Generate".Click().nl())
                ReconstructAsset();

            if (a_texture)
                ("Atlas At " + AssetDatabase.GetAssetPath(a_texture)).edit(ref a_texture, false).nl();

#endif

            return changed;
        }
#endif
    }

    public static class AtlasingExtensions
    {
        public static bool IsAtlased(this PlaytimePainter p)
        {
            if (!p) return false;
            var mat = p.Material;
            if (!mat) return false;
            return p.Material.IsAtlased(p.GetMaterialTexturePropertyName);
        }
        public static bool IsProjected(this PlaytimePainter p) { return p.Material.IsProjected(); }

        public static bool IsAtlased(this Material mat, string property) => mat.IsAtlased() && mat.DisplayNameContains(property, PainterDataAndConfig.isAtlasableDisaplyNameTag);
        
        public static bool IsAtlased(this Material mat) => !mat ? false :  mat.shaderKeywords.Contains(PainterDataAndConfig.UV_ATLASED);
      
        public static bool Contains(this List<AtlasTextureField> lst, Texture2D tex)
        {
            foreach (var ef in lst)
                if ((ef != null) && (ef.texture) && (ef.texture == tex))
                    return false;
            return true;
        }
    }
}