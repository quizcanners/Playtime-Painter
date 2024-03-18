using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuizCanners.Inspect;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Migration;

namespace PainterTool.ComponentModules {
    
    [TaggedTypes.Tag(CLASS_KEY)]
    internal class TileableAtlasingComponentModule : ComponentModuleBase {
        private const string CLASS_KEY = "TilAtlsPntr";
        public override string ClassTag => CLASS_KEY;

        public List<Material> preAtlasingMaterials;
        public Mesh preAtlasingMesh;
        public CfgData preAtlasingSavedMesh;
        private int _inAtlasIndex;
        public int atlasRows = 1;

        private static Vector2Int atlasSector;
        private static int sectorSize = 1;

        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("sm", preAtlasingSavedMesh)
            .Add("iai", _inAtlasIndex)
            .Add("ar", atlasRows);

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key) {
                case "sm": preAtlasingSavedMesh = data; break;
                case "iai": data.ToInt(ref _inAtlasIndex); break;
                case "ar": data.ToInt(ref atlasRows); break;
            }
        }

        #endregion

        public override string ToString() => "Tileable Atlasing";

        public Vector2 GetAtlasedSection()  {
          
            float atY = _inAtlasIndex / atlasRows;
            var atX = _inAtlasIndex - atY * atlasRows;

            return new Vector2(atX, atY);
        }

        public override void Update_Brush_Parameters_For_Preview_Shader() =>
            QcUnity.ToggleShaderKeywords(!painter.IsAtlased(), PainterShaderVariables.UV_NORMAL, PainterShaderVariables.UV_ATLASED);
        
        public bool PaintTexture2D(Painter.Command.Base command) {

            Stroke stroke = command.Stroke;
            float brushAlpha = command.strokeAlphaPortion;
            TextureMeta image = command.TextureData;
            Brush bc = command.Brush;

            if (!painter.IsAtlased()) return false;
            
            var uvCoords = stroke.uvFrom;

            var atlasedSection = GetAtlasedSection();

            sectorSize = image.Width / atlasRows;
            atlasSector = Vector2Int.FloorToInt(atlasedSection * sectorSize);

            BlitFunctions.brAlpha = brushAlpha;

            BlitFunctions.half = (bc.Size(false)) / 2;
            var iHalf = Mathf.FloorToInt(BlitFunctions.half - 0.5f);

            var trg = command.TextureData.Target;

            var smooth = bc.GetBrushType(trg) != BrushTypes.Pixel.Inst;

            if (smooth)
                BlitFunctions.alphaMode = BlitFunctions.CircleAlpha;
            else
                BlitFunctions.alphaMode = BlitFunctions.NoAlpha;

            BlitFunctions.blitMode = bc.GetBlitMode(trg).BlitFunctionTex2D(image);

            if (smooth) iHalf += 1;

            BlitFunctions.alpha = 1;


            BlitFunctions.Set(bc.mask);

            BlitFunctions.cSrc = bc.Color;

            var tmp = image.UvToPixelNumber(uvCoords);

            var fromX = tmp.x - iHalf;

            tmp.y -= iHalf;
            
            var pixels = image.Pixels;

            for (BlitFunctions.y = -iHalf; BlitFunctions.y < iHalf + 1; BlitFunctions.y++)
            {

                tmp.x = fromX;

                for (BlitFunctions.x = -iHalf; BlitFunctions.x < iHalf + 1; BlitFunctions.x++)
                {

                    if (BlitFunctions.alphaMode())
                    {
                        var sx = tmp.x - atlasSector.x;
                        var sy = tmp.y - atlasSector.y;

                        sx %= sectorSize;
                        if (sx < 0)
                            sx += sectorSize;
                        sy %= sectorSize;
                        if (sy < 0)
                            sy += sectorSize;

                        BlitFunctions.blitMode(ref pixels[((atlasSector.y + sy)) * image.Width + (atlasSector.x + sx)]);
                    }

                    tmp.x += 1;
                }

                tmp.y += 1;
            }
            return true;

        }
        
        public override bool OffsetAndTileUv(RaycastHit hit, ref Vector2 uv)
        {
            if (!painter.IsAtlased()) return false;
            
            uv.x %= 1;
            uv.y %= 1;

            var m = painter.GetMesh();

            if (!m) return false;

            var col = painter.meshCollider;

            if (!col || m != col.sharedMesh)
            {
                QcLog.ChillLogger.LogWarningOnce("Painter mesh and collider do not match on {0}".F(painter.gameObject.name), key: "mncol", painter);
            }

            var vertex = m.triangles[hit.triangleIndex * 3];

            var v4L = new List<Vector4>();

            m.GetUVs(0, v4L);

            if (v4L.Count > vertex)
                _inAtlasIndex = (int)v4L[vertex].z;

            atlasRows = Mathf.Max(atlasRows, 1);

            uv = (GetAtlasedSection() + uv) / atlasRows;

            return true;
        }

        #region Inspector

        public override void BrushConfigPEGI()
        {
            var p = PainterComponent.inspected;

            if (!p.IsAtlased()) return;
            
            var m = p.Material;
            if (p.NotUsingPreview) {
                if (m.HasProperty(PainterShaderVariables.ATLASED_TEXTURES))
                    atlasRows = m.GetInt(PainterShaderVariables.ATLASED_TEXTURES);
            }

            "Atlased Texture {0}*{0}".F(atlasRows).PegiLabel("Shader has _ATLASED define").Write();

            if ("Undo".PegiLabel().Click().Nl())
                m.DisableKeyword(PainterShaderVariables.UV_ATLASED);

            var id = p.TexMeta;

            if (id.TargetIsRenderTexture())
                "Watch out, Render Texture Brush can change neighboring textures on the Atlas.".PegiLabel().WriteOneTimeHint("rtOnAtlas");

            if (id.TargetIsTexture2D()) return;
            
            "Render Texture painting does not yet support Atlas Editing".PegiLabel().WriteWarning();
            
            pegi.Nl();
        }
       
        #endregion

        public override void BeforeGpuStroke(Painter.Command.ForPainterComponent command) //Brush br, Stroke st, BrushTypes.Base type)
        {
            if (!painter.Is3DBrush(command.Brush) || !painter.IsAtlased()) return;
            
            var ats = GetAtlasedSection();
            PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(ats.x, ats.y, atlasRows, 1);
        }

        public override void AfterGpuStroke(Painter.Command.ForPainterComponent command) //Brush br, Stroke st, BrushTypes.Base type)
                                                                     {
            if (painter.Is3DBrush(command.Brush) && painter.IsAtlased())
                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
                                                                     }
    }
    
    [Serializable]
    public class FieldAtlas : PainterClass, IPEGI
    {
        public string atlasedField;
        public int originField;
        public int atlasCreatorId;
        public bool enabled;
        public Color col;
        public AtlasTextureCreator AtlasCreator => Painter.Data.atlases.Count > atlasCreatorId ? Painter.Data.atlases[atlasCreatorId] : null;

        #region Inspector

        //private int _inspectedItems = -1;

        private readonly pegi.EnterExitContext context = new();

        void IPEGI.Inspect() {

            using (context.StartContext())
            {
                var a = MaterialAtlases.inspectedAtlas;

                atlasedField.PegiLabel().ToggleIcon(ref enabled).Nl();

                if (!enabled)
                    return;

                pegi.Select_Index(ref originField, a.originalTextures).Nl();

                pegi.Space();

                "Atlas".PegiLabel().Enter_Inspect(AtlasCreator).Nl();

                if (!context.IsAnyEntered)
                {
                    "Atlases".PegiLabel(70).Select_Index(ref atlasCreatorId, Painter.Data.atlases);
                    if (Icon.Add.Click("Create new Atlas").Nl())
                    {
                        atlasCreatorId = Painter.Data.atlases.Count;
                        var ac = new AtlasTextureCreator(atlasedField + " for " + a.name);
                        Painter.Data.atlases.Add(ac);
                        Painter.Data.SetToDirty();
                    }
                }

                if ((atlasedField != null) && (a.originalMaterial) && (AtlasCreator != null) && (originField < a.originalTextures.Count))
                {
                    var t = a.originalMaterial.Get(a.originalTextures[originField]);
                    if (t && t is Texture2D)
                        Icon.Done.Draw();
                    else "Will use Color".PegiLabel().Edit(ref col).Nl();
                }
                else
                    "Color".PegiLabel("Color that will be used instead of a texture.", 35).Edit(ref col).Nl();
            }
        }

        #endregion
    }
    
    [Serializable]
    public class MaterialAtlases : PainterClass, IGotName, IPEGI {

        [SerializeField] public string name;
        [SerializeField] public Material originalMaterial;
        [SerializeField] protected Shader originalShader;

        [SerializeField] public List<ShaderProperty.TextureValue> originalTextures;
        private Material _atlasedMaterial;

        private Material DestinationMaterial => _atlasedMaterial ? _atlasedMaterial : originalMaterial; 

        public string NameForInspector { get { return name; } set { name = value; } }

        private readonly List<FieldAtlas> _fields = new();
        
        private string _matAtlasProfile;

        private void ConvertToAtlased(PainterComponent painter)
        {
            #if UNITY_EDITOR

            if (!_atlasedMaterial)
                _atlasedMaterial = painter.InstantiateMaterial(true);

            painter.SetOriginalShaderOnThis();

            painter.UpdateOrSetTexTarget(TexTarget.Texture2D);

            var mat = painter.Material;
            var texProperties = mat.MyGetTextureProperties_Editor();

            var index = 0;
            var passedFields = new List<FieldAtlas>();
            var passedTextures = new List<Texture2D>();
            var passedColors = new List<Color>();

            foreach (var f in _fields)
                if (f.enabled && f.AtlasCreator != null)
                {

                    var contains = false;

                    foreach (var p in texProperties)
                        if (p.ToString().Equals(originalTextures[f.originField].ToString()))
                        {
                            contains = true;
                            break;
                        }

                    if (!contains) continue;
                    
                    var original = originalTextures[f.originField];

                    var tex = mat.Get(original);

                    Texture2D texture = null;

                    if (!tex)
                    {
                        var note = painter.name + " no " + original + " texture. Using Color.";

                        pegi.GameView.ShowNotification(note);

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

                    var aTextures = f.AtlasCreator.textures;

                    var added = false;

                    for (var i = index; i < aTextures.Count; i++)
                        if ((aTextures[i] == null) || (!aTextures[i].used) || (aTextures[i].texture == texture))
                        {
                            index = i;
                            passedFields.Add(f);
                            passedTextures.Add(texture);
                            passedColors.Add(f.col);
                            added = true;
                            break;
                        }

                    if (added) continue;
                    
                    Debug.Log("Could not find a place for " + original);
                    
                    return;
                }

            if (passedFields.Count <= 0) return;
            
            var firstAtlasing = false;

            var atlPlug = painter.GetModule<TileableAtlasingComponentModule>();

            if (atlPlug.preAtlasingMaterials == null)
            {
                atlPlug.preAtlasingMaterials = painter.Materials.ToList();
                atlPlug.preAtlasingMesh = painter.GetMesh();
                firstAtlasing = true;
            }

            var mainField = passedFields[0];

            atlPlug.atlasRows = mainField.AtlasCreator.Row;

            var tiling = mat.GetTiling(originalTextures[mainField.originField]);
            var offset = mat.GetOffset(originalTextures[mainField.originField]);

            for (var i = 0; i < passedFields.Count; i++) {

                var f = passedFields[i];
                var ac = f.AtlasCreator;

                ac.textures[index] = new AtlasTextureField(passedTextures[i], passedColors[i]);

                ac.AddTargets(f, originalTextures[f.originField]);
                ac.ReconstructAsset();
                _atlasedMaterial.SetTexture(f.atlasedField, ac.aTexture);
            }

            Painter.MeshManager.EditMesh(painter, true);

            if (firstAtlasing)
                atlPlug.preAtlasingSavedMesh = MeshEditorManager.editedMesh.Encode().CfgData;

            painter.selectedMeshProfile = _matAtlasProfile;

            if (tiling != Vector2.one || offset != Vector2.zero)
            {
                MeshEditorManager.editedMesh.TileAndOffsetUVs(offset, tiling, 0);
                Debug.Log("offsetting " + offset + " tiling " + tiling);
            }

            TriangleAtlasTool.Inst.SetAllTrianglesTextureTo(index, 0, painter.selectedSubMesh);
            Painter.MeshManager.Redraw();
            Painter.MeshManager.StopEditingMesh();

            _atlasedMaterial.SetFloat(PainterShaderVariables.ATLASED_TEXTURES, atlPlug.atlasRows);
            painter.Material = _atlasedMaterial;

            if (firstAtlasing)
            {
                var m = painter.GetMesh();
                m.name = m.name + "_Atlased_" + index;
            }
 
            _atlasedMaterial.EnableKeyword(PainterShaderVariables.UV_ATLASED);
            
#endif
                    }

        private void FindAtlas(int field)
        {
            //var texMGMT = PainterCamera.Inst;

            var atlases = Painter.Data.atlases;
            
            for (var a = 0; a < atlases.Count; a++)
            {
                var atl = atlases[a];
                
                if (!atl.atlasFields.Contains(_fields[field].atlasedField)) continue;
                
                foreach (var t in originalTextures)
                {
                    if (!atl.targetFields.Contains(t)) continue;
                    
                    _fields[field].atlasCreatorId = a;
                    
                    var tex = originalMaterial.Get(t);

                    if (!tex) continue; 
                    
                    var t2 = (Texture2D) tex;
                    
                    if (t2 && atl.textures.Contains(t2))
                        return;
                }
            }
        }

        private void OnChangeMaterial() {

            #if UNITY_EDITOR

            if (originalMaterial)
                originalTextures = originalMaterial.MyGetTextureProperties_Editor();

            if ((DestinationMaterial) && (DestinationMaterial.HasProperty(PainterShaderVariables.isAtlasedProperty)))
            {
                var aTextures = DestinationMaterial.MyGetTextureProperties_Editor();
                _fields.Clear();
                foreach (var t in aTextures)
                {
                    var ac = new FieldAtlas();
                    _fields.Add(ac);
                    ac.atlasedField = t.ToString();
                }
                
                _atlasedShader = DestinationMaterial.shader;


                foreach (var p in UnityEditor.MaterialEditor.GetMaterialProperties(new Object[] { DestinationMaterial }))
                    if (p.displayName.Contains(PainterShaderVariables.isAtlasableDisaplyNameTag))
                        foreach (var f in _fields)
                            if (f.atlasedField.SameAs(p.name))
                                f.enabled = true;
                            

                if (!_atlasedMaterial)
                    for (var i = 0; i < _fields.Count; i++)
                        _fields[i].originField = i;
                else if (originalMaterial)
                {
                    var orTexts = originalMaterial.MyGetTextureProperties_Editor();
                    foreach (var f in _fields)
                        for (var i = 0; i < orTexts.Count; i++)
                            if (orTexts[i].ToString().SameAs(f.atlasedField))
                                f.originField = i;


                }
            }

            if (!originalMaterial) return;
            
            for (var i = 0; i < _fields.Count; i++)
                FindAtlas(i);
            
#endif
            }

        #region Inspector

        private Shader _atlasedShader;
        public static MaterialAtlases inspectedAtlas;
        private bool _showHint;
        void IPEGI.Inspect()
        {

#if UNITY_EDITOR
            var painter = PainterComponent.inspected;
            inspectedAtlas = this;


            painter.SetOriginalShaderOnThis();

            var mat = painter.Material;

            if ((mat) && ((mat != originalMaterial) || mat.shader != originalShader))
            {
                originalMaterial = mat;
                originalShader = mat.shader;
                OnChangeMaterial();
            }

            "Name".PegiLabel(50).Edit(ref name).Nl();

            if ("Hint".PegiLabel().IsFoldout(ref _showHint).Nl())
            {

                ("If you don't set Atlased Material(Destination)  it will try to create a copy of current material and set isAtlased toggle on it, if it has one." +
                    " Below you can see: list of Texture Properties, for each you can select or create an atlas. Atlas is a class that holds all textures assigned to an atlas, and also creates and stores the atlas itself." +
                    "After this you can select a field from current Material, texture of which will be copied into an atlas. A bit confusing, I know)" +
                    "Also if light looks smudged, rebuild the light.").PegiLabel().Write_Hint();
            }

            if (("Atlased Material:".PegiLabel(90).Edit(ref _atlasedMaterial).Nl() ||
                (_atlasedMaterial && _atlasedMaterial.shader != _atlasedShader))) 
                OnChangeMaterial();
            

            if (painter)
            {
                var mats = painter.Materials;
                if (mats != null)
                {
                    if (mats.Length > 1)
                    {
                        if ("Source Material:".PegiLabel("Same as selecting a sub Mesh, which will be converted", 90).Select_Index( ref painter.selectedSubMesh, mats))
                            OnChangeMaterial();
                    }
                    else if (mats.Length > 0)
                        "Source Material".PegiLabel("Sub Mesh which will be converted", 90).Write( mats[0]);
                }
                pegi.Nl();
                pegi.Space();
                pegi.Nl();
            }



            pegi.Space();
            pegi.Nl();

            foreach (var f in _fields)
                f.Nested_Inspect().Nl();

            "Mesh Profiles [{0}]".F(Painter.Data.meshPackagingSolutions.Count).PegiLabel()
                .Select_iGotName(ref _matAtlasProfile, Painter.Data.meshPackagingSolutions);

            if (Icon.Refresh.Click("Refresh Mesh Packaging Solutions"))
                Painter.Data.ResetMeshPackagingProfiles();

            pegi.Nl();

            if (DestinationMaterial && !DestinationMaterial.HasProperty(PainterShaderVariables.isAtlasedProperty))
            {
                if (!_atlasedMaterial) "Original Material doesn't have isAtlased property, change shader or add Destination Atlased Material".PegiLabel().Write_Hint();
                else "Atlased Material doesn't have isAtlased property".PegiLabel().Write_Hint();
            }
            else if (originalMaterial)
            {

                var names = "";
                foreach (var f in _fields)
                    if (f.enabled && f.AtlasCreator == null) names += f.atlasedField + ", ";

                if (names.Length > 0)
                    ("Fields " + names + " don't have atlases assigned to them, create some").PegiLabel().Write_Hint();
                else if ("Convert to Atlased".PegiLabel().Click())
                    ConvertToAtlased(painter);
            }
#endif

            inspectedAtlas = null;
            
        }
       
        #endregion
    }
    

    [Serializable]
    public class AtlasTextureField: IPEGI_ListInspect
    {
        [SerializeField] public Texture2D texture;
        [SerializeField] public Color color = Color.black;
        [SerializeField] public bool used;

        public AtlasTextureField() { used = true; }

        public AtlasTextureField(Texture2D tex, Color col)
        {
            texture = tex;
            color = col;
            used = true;
        }

        #region Inspector

        public void InspectInList(ref int edited, int ind)
        {
            (used ? Icon.Active : Icon.InActive).Draw();

            pegi.Edit(ref texture);
            if (!texture)
                pegi.Edit(ref color);
        }
        
        #endregion
    }
    
    public class AtlasTextureCreator : PainterClassCfg, IGotName, IPEGI
    {
        private int _atlasSize = 2048;

        private int _textureSize = 512;

        private bool _sRgb = true;
        
        public string NameForInspector { get; set;}

        public List<ShaderProperty.TextureValue> targetFields = new();

        public List<string> atlasFields = new();

        private List<string> _srcFields = new();

        public Texture2D aTexture;

        public List<AtlasTextureField> textures = new();
        private readonly pegi.CollectionInspectorMeta _texturesMeta = new("Textures");

        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()//base.Encode()//this.EncodeUnrecognized()
            .Add("af", atlasFields)
            .Add("sf", _srcFields)
            .Add_String("n", NameForInspector)
            .Add_Bool("rgb", _sRgb)
            .Add("s", _textureSize)
            .Add("as", _atlasSize);

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key) {
                case "af": data.ToList(out atlasFields); break;
                case "sf": data.ToList(out _srcFields); break;
                case "n": NameForInspector = data.ToString(); break;
                case "rgb": _sRgb = data.ToBool(); break;
                case "s":  data.ToInt(ref _textureSize); break;
                case "as":  data.ToInt(ref _atlasSize); break;
            }
        }

        #endregion

        public int Row => _atlasSize / _textureSize; 

        public void AddTargets(FieldAtlas at, ShaderProperty.TextureValue target)
        {
            if (!atlasFields.Contains(at.atlasedField))
                atlasFields.Add(at.atlasedField);
            if (!targetFields.Contains(target))
                targetFields.Add(target);
        }

        private void Init() => AdjustListSize();
        
        public AtlasTextureCreator()
        {
            Init();
        }

        private string GetUniqueName<T>(string s, List<T> list)
        {

            bool match = true;
            int index = 1;
            string mod = s;
            
            while (match)
            {
                match = false;

                foreach (var l in list)
                    if (l.ToString().SameAs(mod))
                    {
                        match = true;
                        break;
                    }

                if (match)
                {
                    mod = s + index;
                    index++;
                }
            }

            return mod;
        }

        public AtlasTextureCreator(string newName)
        {
            NameForInspector = newName;
            NameForInspector = GetUniqueName(NameForInspector, Painter.Data.atlases);
            Init();
        }

        private void AdjustListSize()
        {
            var ntc = TextureCount;
            while (textures.Count < ntc)
                textures.Add(new AtlasTextureField(null, Color.gray));
        }

        private int TextureCount
        {
            get { var r = Row; return r * r; }
        }

        private void ColorToAtlas(Color col, int x, int y)
        {
            var size = _textureSize * _textureSize;
            var pix = new Color32[size];
            Color32 tmp = col;
            for (var i = 0; i < size; i++)
                pix[i] = tmp;

            aTexture.SetPixels32(x * _textureSize, y * _textureSize, _textureSize, _textureSize, pix);
        }

        private void SmoothBorders(Texture2D atlas, int mipLevel)
        {
            var col = atlas.GetPixels(mipLevel);

            var aSize = _atlasSize;
            var tSize = _textureSize;

            for (var i = 0; i < mipLevel; i++)
            {
                aSize /= 2;
                tSize /= 2;
            }

            if (tSize == 0)
                return;

            var cnt = aSize / tSize;

            for (var ty = 0; ty < cnt; ty++)
            {
                var startY = ty * tSize * aSize;
                var lastY = (ty * tSize + tSize - 1) * aSize;
                for (var tx = 0; tx < cnt; tx++)
                {
                    var startX = tx * tSize;
                    var lastX = startX + tSize - 1;

                    Color tmp = Color.clear;
                    
                    tmp += col[startY + startX].linear;
                    tmp += col[startY + lastX].linear;
                    tmp += col[lastY + startX].linear;
                    tmp += col[lastY + lastX].linear;

                    tmp *= 0.25f;

                    var tmpC = tmp.gamma;
                    
                    col[startY + startX] = tmpC;
                    col[startY + lastX] = tmpC;
                    col[lastY + startX] = tmpC;
                    col[lastY + lastX] = tmpC;


                    for (var x = startX + 1; x < lastX; x++)
                    {
                        tmp = Color.clear;
                        tmp += col[startY + x].linear;
                        tmp += col[lastY + x].linear;
                        tmp *= 0.5f;
                        tmpC = tmp.gamma;
                        col[startY + x] = tmpC;
                        col[lastY + x] = tmpC;
                    }

                    for (var y = startY + aSize; y < lastY; y += aSize)
                    {
                        tmp = Color.clear;

                        tmp += col[y + startX].linear;
                        tmp += col[y + lastX].linear;
                        tmp *= 0.5f;
                        tmpC = tmp.gamma;
                        col[y + startX] = tmpC;
                        col[y + lastX] = tmpC;
                    }
                }
            }

            #pragma warning disable UNT0017 // Needed for floating point calculation
            atlas.SetPixels(col, mipLevel);
            #pragma warning restore UNT0017 // SetPixels invocation is slow
        }

        private void ReconstructAtlas()
        {

            if (aTexture && (aTexture.width != _atlasSize)) {
                Object.DestroyImmediate(aTexture);
                aTexture = null;
            }

            if (!aTexture)
                aTexture = new Texture2D(_atlasSize, _atlasSize, TextureFormat.ARGB32, true, !_sRgb);

            var texturesInRow = _atlasSize / _textureSize;
            
            var curIndex = 0;

            var defaultCol = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            for (var y = 0; y < texturesInRow; y++)
                for (var x = 0; x < texturesInRow; x++) {
                    var atlField = textures[curIndex];
                    if ((textures.Count > curIndex) && (atlField != null) && atlField.used)
                    {

                        var tex = atlField.texture;

                        if (tex) {
#if UNITY_EDITOR
                            tex.Reimport_IfNotReadale_Editor();
#endif

                            var from = tex.GetPixels32(_textureSize, _textureSize);

                            aTexture.SetPixels32(x * _textureSize, y * _textureSize, _textureSize, _textureSize, from);

                        }
                        else
                            ColorToAtlas(atlField.color, x, y);
                    }
                    else
                        ColorToAtlas(defaultCol, x, y);

                    curIndex++;
                }

        }

        public void ReconstructAsset()
        {
            #if UNITY_EDITOR
                ReconstructAtlas();

                for (var m = 0; m < aTexture.mipmapCount; m++)
                    SmoothBorders(aTexture, m);

                aTexture.Apply(false);
            
                var bytes = aTexture.EncodeToPNG();

                var lastPart = Path.Combine(Painter.Data.texturesFolderName, Painter.Data.atlasFolderName);
                var fullPath = Path.Combine(Application.dataPath, lastPart);
                Directory.CreateDirectory(fullPath);

                var fileName = NameForInspector + ".png";
                var relativePath = Path.Combine("Assets", lastPart, fileName);
                fullPath += fileName;

                File.WriteAllBytes(fullPath, bytes);

                UnityEditor.AssetDatabase.Refresh(); // few times caused color of the texture to get updated to earlier state for some reason

                aTexture = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

                UnityEditor.TextureImporter other = null;

                foreach (var t in textures)
                    if ((t != null) && t.texture) {
                        other = t.texture.GetTextureImporter_Editor();
                        break;
                    }

                var ti = aTexture.GetTextureImporter_Editor();

                var needReimport = ti.WasNotReadable_Editor();

                if (other != null)
                    needReimport |= ti.WasWrongIsColor_Editor(other.sRGBTexture);

                needReimport |= ti.WasClamped_Editor();

                if (needReimport) ti.SaveAndReimport();
            #endif
        }

        [SerializeField]  private pegi.EnterExitContext context = new();

        void IPEGI.Inspect()
        {
            using (context.StartContext())
            {
                if (!context.IsAnyEntered)
                {
                    "Atlas size:".PegiLabel().Edit_Delayed(ref _atlasSize, 80).Nl();
                    _atlasSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_atlasSize, 512, 4096));

                    if ("Textures size:".PegiLabel().Edit_Delayed(ref _textureSize, 80).Nl())

                        _textureSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_textureSize, 32, _atlasSize / 2));

                    AdjustListSize();
                }

                _texturesMeta.Enter_List(textures).Nl();

                if ("Textures:".PegiLabel().IsFoldout().Nl())
                {
                    AdjustListSize();
                    var max = TextureCount;

                    for (var i = 0; i < max; i++)
                    {
                        var t = textures[i];

                        if (!t.used) continue;

                        pegi.Edit(ref t.texture);

                        if (!t.texture)
                            pegi.Edit(ref t.color);

                        pegi.Nl();
                    }
                }

                pegi.Nl();
                "Is Color Atlas:".PegiLabel().ToggleIcon(ref _sRgb).Nl();

                if ("Generate".PegiLabel().Click().Nl())
                    ReconstructAsset();

#if UNITY_EDITOR
                if (aTexture)
                    ("Atlas At " + UnityEditor.AssetDatabase.GetAssetPath(aTexture)).PegiLabel().Edit(ref aTexture, false).Nl();
#endif

            }
        }
    }

    public static class AtlasingExtensions
    {
        public static bool IsAtlased(this PainterComponent p)
        {
            if (!p) return false;
            var mat = p.Material;
            return mat && mat.IsAtlased(p.GetMaterialTextureProperty());
        }
        public static bool IsProjected(this PainterComponent p) => p.Material.IsProjected(); 

        public static bool IsAtlased(this Material mat, string property) => mat.IsAtlased() && property.Contains(PainterShaderVariables.isAtlasableDisaplyNameTag);
        
        public static bool IsAtlased(this Material mat, ShaderProperty.TextureValue property) => mat.IsAtlased() && property.ToString().Contains(PainterShaderVariables.isAtlasableDisaplyNameTag);
        
        public static bool IsAtlased(this Material mat) => mat && mat.shaderKeywords.Contains(PainterShaderVariables.UV_ATLASED);
      
        public static bool Contains(this IEnumerable<AtlasTextureField> lst, Texture2D tex) => lst.All(ef => (ef == null) || (!ef.texture) || (ef.texture != tex));
        
    }
}