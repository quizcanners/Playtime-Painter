using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{
    
    public class MeshPackagingProfile : AbstractStd, IPEGI, IGotName
    {
        public List<VertexContents> sln = new List<VertexContents>();

        public string name = "";

        public const string FolderName = "Mesh Profiles";

        #region Inspect
        public string NameForPEGI { get { return name; } set { name = value;  } }

        #if PEGI
        public virtual bool Inspect()
        {
            var changed = false;
            
            "Profile Name: ".edit(80, ref name);

            #if UNITY_EDITOR

            var path = Path.Combine(PainterCamera.Data.meshesFolderName, FolderName);
            
            if (icon.Save.Click("Save To:" + path, 25).nl()) {
                this.SaveToAssets(path, name);
                UnityHelperFunctions.RefreshAssetDatabase();
                (name + " Saved to " + path).showNotificationIn3D_Views();
            }


            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType).nl(ref changed)) {
               
                var mSol = new MeshPackagingProfile();
                mSol.Decode(StuffLoader.LoadTextAsset(myType));

                PainterCamera.Data.meshPackagingSolutions.Add(mSol);
                PlaytimePainter.inspected.selectedMeshProfile = PainterCamera.Data.meshPackagingSolutions.Count - 1;
            }
            #endif

            foreach (var s in sln) 
                Inspect().nl(ref changed);

            return changed;
        }
        #endif
        #endregion

        public bool Repack(MeshConstructor sm)
        {

            if (!sm.Valid) {
                Debug.Log("Got no stuff to regenerate mesh. ");
                return false;
            }

            sm.mesh.Clear();

            MeshSolutions.CurMeshDta = sm;

            MeshSolutions.dataTypeFilter = null;

            foreach (var vs in sln)
                if (vs.enabled) vs.Pack();

            foreach (var vt in MeshSolutions.dataTypes)
                vt.Clear();

            return true;
        }

        public bool UpdatePackage(MeshConstructor sm, Type dataType) {

            MeshSolutions.CurMeshDta = sm;

            MeshSolutions.dataTypeFilter = dataType;

            foreach (var vs in sln)
                if (vs.enabled) vs.Pack();

            foreach (var vt in MeshSolutions.dataTypes)
                vt.Clear();

            return true;
        }

        #region Encode & Decode
        public override StdEncoder Encode() 
        {
            var cody = new StdEncoder();

            cody.Add_String("n", name);
            cody.Add_IfNotEmpty("sln", sln);


            return cody;
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "n": name = data; break;
                case "sln":  data.Decode_List(out sln); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public const string StdTagVertSol = "vertSol";

        public MeshPackagingProfile() {

            name = "unNamed";
            var targets = MeshSolutions.dataTargets;
            sln = new List<VertexContents>(); 
            foreach (var t in targets)
                sln.Add(new VertexContents(t));

        }

    }

    #region Data
    public abstract class VertexDataTarget : IGotDisplayName
    {
        public byte chanelsHas;
        public int myIndex;

        public abstract string NameForDisplayPEGI { get; }

        public virtual void Set(Vector3[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for " + this.GetType());
        }

        public virtual void Set(Vector4[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for array of " + this.GetType());
        }

        public virtual void SetDefaults(VertexContents to)
        {
            for (int i = 0; i < to.vals.Count; i++)
                to.vals[i].valueIndex = i;
        }

        public virtual string GetFieldName(int ind)
        {
            ind = ind % 4;

            switch (ind)
            {
                case 0: return "x";
                case 1: return "y";
                case 2: return "z";
                case 3: return "w";
            }

            return "Error";
        }

        public VertexContents GetMySolution()
        {
            MeshPackagingProfile pf = MeshSolutions.CurMeshDta.profile;
            return pf.sln[myIndex];
        }

        public VertexDataTarget(byte chanCont, int index)
        {
            chanelsHas = chanCont;
            myIndex = index;
        }
    }

    public abstract class VertexDataType: IGotDisplayName
    {
        public byte chanelsNeed;
        public int myIndex;

        public abstract string NameForDisplayPEGI { get; }

        public virtual string GetFieldName(int ind)
        {
            ind = ind % 4;

            switch (ind)
            {
                case 0: return "X";
                case 1: return "Y";
                case 2: return "Z";
                case 3: return "W";
            }

            return "Error";
        }

        public VertexDataType(byte chanCnt, int index)
        {
            myIndex = index;
            chanelsNeed = chanCnt;

        }

        public abstract void Clear();

        public virtual void GenerateIfNull() => Debug.Log(this.GetType() + " does not generate any data");
        
        public virtual float[] GetValue(int no) => null;
        
        public virtual Vector2[] GetV2(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector2 array");
            return null;
        }

        public virtual Vector3[] GetV3(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector3 array");
            return null;
        }

        public virtual Vector4[] GetV4(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector4 array");
            return null;
        }

    }

    public class VertexDataValue : AbstractStd {

        public int typeIndex = 0;
        public int valueIndex = 0;

        public VertexDataType VertDataType => MeshSolutions.dataTypes[typeIndex]; 

        public float[] GetDataArray() {
            VertDataType.GenerateIfNull();
            return VertDataType.GetValue(valueIndex);
        }

        #region Encode & Decode
        public override StdEncoder Encode() {
            StdEncoder cody = new StdEncoder();
            cody.Add_IfNotZero("t", typeIndex);
            cody.Add_IfNotZero("v", valueIndex);
            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "t": typeIndex = data.ToInt(); break;
                case "v": valueIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }
    #endregion

    public class VertexContents : AbstractStd, IPEGI
    {
        public int sameSizeDataIndex = -1;
        private int _targetIndex;
        public bool enabled;
        public static bool showHint;
        
        public VertexDataType SameSizeValue => (sameSizeDataIndex >= 0) ?  MeshSolutions.dataTypes[sameSizeDataIndex] : null;
        public VertexDataTarget Target { get { return MeshSolutions.dataTargets[_targetIndex]; } set { _targetIndex = value.myIndex; } }

        public List<VertexDataValue> vals = new List<VertexDataValue>();

        #region Encode & Decode
        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();

            cody.Add_IfTrue("en", enabled)
            .Add_IfNotZero("t", _targetIndex);

            if (enabled)
            {
                if (sameSizeDataIndex == -1)
                    cody.Add_IfNotEmpty("vals", vals);
                else
                    cody.Add_IfNotNegative("sameSize", sameSizeDataIndex);
            }
            return cody;
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "en": enabled = data.ToBool(); break;
                case "t": _targetIndex = data.ToInt(); if (!enabled) InitVals(); break;
                case "vals": data.Decode_List(out vals); sameSizeDataIndex = -1; break;
                case "sameSize": sameSizeDataIndex = data.ToInt(); InitVals(); break;

                default: return false;
            }
            return true;
        }
        
        public override void Decode(string data) {
            base.Decode(data);
            InitVals();
        }

        #endregion

        #region Inspector
#if PEGI
        public virtual bool Inspect()
        {
            bool changed = false;

            (Target.ToPEGIstring() + ":").toggle(80, ref enabled);

            if (enabled)
            {

                var tps = MeshSolutions.GetTypesBySize(vals.Count);
                string[] nms = new string[tps.Count + 1];

                for (int i = 0; i < tps.Count; i++)
                    nms[i] = tps[i].ToPEGIstring();

                nms[tps.Count] = "Other";

                int selected = tps.Count;

                if (SameSizeValue != null)
                    for (int i = 0; i < tps.Count; i++)
                        if (tps[i] == SameSizeValue)
                        {
                            selected = i;
                            break;
                        }

                changed |= pegi.select(ref selected, nms).nl();

                if (selected >= tps.Count)
                    sameSizeDataIndex = -1;
                else
                    sameSizeDataIndex = tps[selected].myIndex;

                string[] allDataNames = MeshSolutions.GetAllTypesNames();

                if (SameSizeValue == null)
                {
                    for (int i = 0; i < vals.Count; i++) {
                        VertexDataValue v = vals[i];

                        changed |= Target.GetFieldName(i).select(40, ref v.typeIndex, allDataNames);

                        string[] typeFields = new string[v.VertDataType.chanelsNeed];

                        for (int j = 0; j < typeFields.Length; j++)
                            typeFields[j] = v.VertDataType.GetFieldName(j);
                        
                        changed |= pegi.select(ref v.valueIndex, typeFields).nl();

                        typeFields.ClampIndexToLength(ref v.valueIndex);
                    }
                }
                "**************************************************".nl();
            }

            return changed;
        }
        #endif
        #endregion

        public VertexContents() { }

        public VertexContents(VertexDataTarget ntrg) {
            Target = ntrg;
            InitVals();
            ntrg.SetDefaults(this);
        }

        private void InitVals() {
            while (vals.Count < Target.chanelsHas)
                vals.Add(new VertexDataValue());
        }

        public void Pack()
        {
            try
            {
                switch (Target.chanelsHas)
                {
                    case 3: PackVector3(); break;
                    case 4: PackVector4(); break;
                    default: Debug.Log("No packaging function for Vector" + Target.chanelsHas + " target."); break;
                }
            } catch (Exception ex)
            {
                Debug.LogError("Exception in {0}  :  {1}".F(Target.ToPEGIstring(), ex.ToString()));
            }
        }

        private void PackVector3() {

            Vector3[] ar;

            if (SameSizeValue != null)
            {
                if (MeshSolutions.dataTypeFilter != null &&  SameSizeValue.GetType() != MeshSolutions.dataTypeFilter)
                    return;

                SameSizeValue.GenerateIfNull();

                ar = SameSizeValue.GetV3(Target);

            }
            else
            {
                ar = new Vector3[MeshSolutions.vcnt];

                for (var i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector3();

                for (var i = 0; i < 3; i++) {
                    var v = vals[i];
                    var tmp = v.GetDataArray();

                    if (tmp == null)
                        continue;
                    
                    for (var j = 0; j < MeshSolutions.vcnt; j++)
                        ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Target.Set(ar);

        }

        private void PackVector4()
        {

            Vector4[] ar;

            if (SameSizeValue != null)
            {
                if (MeshSolutions.dataTypeFilter != null && SameSizeValue.GetType() != MeshSolutions.dataTypeFilter)
                    return;

                SameSizeValue.GenerateIfNull();

                ar = SameSizeValue.GetV4(Target);

            }
            else
            {

                ar = new Vector4[MeshSolutions.vcnt];

                for (var i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector4();

                for (var i = 0; i < 4; i++)
                {
                    var v = vals[i];
                    var tmp = v.GetDataArray();

                    if (tmp == null) continue;
                    
                    for (var j = 0; j < MeshSolutions.vcnt; j++)
                        ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Target.Set(ar);

        }

    }
    
    public static class MeshSolutions {

        #region Static
        public static Type dataTypeFilter;

        private const string shaderPreferredPackagingSolution = "Solution";

        public static int GetMeshProfileByTag(this Material mat)
        {
            if (!mat)
                return 0;

            var name = mat.GetTag(shaderPreferredPackagingSolution, false, "Standard");

            var prf = PainterCamera.Data.meshPackagingSolutions;

            for (var i = 0; i < prf.Count; i++)
                if (prf[i].name.SameAs(name)) return i;

            return 0;
        }

        private static MeshConstructor _curMeshDra;

        public static MeshConstructor CurMeshDta
        {

            get { return _curMeshDra; }

            set { _curMeshDra = value;
                vcnt = value.vertsCount;
                _chanelMedium = new float[vcnt];
            }

        }

        public static int vcnt = 0;
        private static float[] _chanelMedium = null;
        #endregion

        private class VertexPosTrg : VertexDataTarget
        {
            private const int dataSize = 3;

            public override void Set(Vector3[] dta)
            {
                CurMeshDta.mesh.vertices = dta;
                if (CurMeshDta.tris == null) return;
                
                CurMeshDta.mesh.subMeshCount = CurMeshDta.tris.Length;
                for (var sm = 0; sm < CurMeshDta.tris.Length; sm++)
                    CurMeshDta.mesh.SetTriangles(CurMeshDta.tris[sm], sm, true);
                
            }

            public override string NameForDisplayPEGI => "position";
            
            public override void SetDefaults(VertexContents to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexPos.inst.myIndex;
            }

            public VertexPosTrg(int index) : base(dataSize, index)
            {

            }

        }

        private class VertexUVTrg : VertexDataTarget
        {
            private const int dataSize = 4;

            private int MyUvChanel() => (myIndex - 1); 

            public override void Set(Vector4[] dta)
            {

                var vs = GetMySolution();
                
                if ((vs.SameSizeValue != null) || (vs.vals[2].VertDataType != VertexNull.inst) || (vs.vals[3].VertDataType != VertexNull.inst))
                    CurMeshDta.mesh.SetUVs(MyUvChanel(), new List<Vector4>(dta));
                else
                {
                    var v2S = new Vector2[dta.Length];

                    for (var i = 0; i < dta.Length; i++)
                    {
                        var v4 = dta[i];
                        v2S[i] = new Vector2(v4.x, v4.y);
                    }

                    switch (MyUvChanel())
                    {
                        case 0:
                            CurMeshDta.mesh.uv = v2S; break;
                        case 1:
                            CurMeshDta.mesh.uv2 = v2S; break;
                        case 2:
                            CurMeshDta.mesh.uv3 = v2S; break;
                        case 3:
                            CurMeshDta.mesh.uv3 = v2S; break;
                    }
                }
            }

            public override string NameForDisplayPEGI => "UV" + MyUvChanel().ToString();
            
            public override void SetDefaults(VertexContents to)
            {
                base.SetDefaults(to);

                if (myIndex != 1) return;

                to.enabled = true;

                var ind = VertexUV.inst[0].myIndex;

                to.vals[0].typeIndex = ind;
                to.vals[0].valueIndex = 0;
                to.vals[1].typeIndex = ind;
                to.vals[1].valueIndex = 1;

                ind++;

                to.vals[2].typeIndex = ind;
                to.vals[2].valueIndex = 0;
                to.vals[3].typeIndex = ind;
                to.vals[3].valueIndex = 1;

            }

            public VertexUVTrg(int index) : base(dataSize, index)
            {

            }

        }

        private class VertexTangentTrg : VertexDataTarget
        {
            private const int dataSize = 4;

            public override void Set(Vector4[] dta)
            {
                CurMeshDta.mesh.tangents = dta;
            }

             public override string NameForDisplayPEGI => "tangent";
            
            public override void SetDefaults(VertexContents to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexTangent.inst.myIndex;
            }

            public VertexTangentTrg(int index) : base(dataSize, index)
            {

            }
        }

        private class VertexNormalTrg : VertexDataTarget
        {
            private const int dataSize = 3;

            public override void Set(Vector3[] dta) =>  CurMeshDta.mesh.normals = dta;
            
            public override string NameForDisplayPEGI => "normal";
            
            public override void SetDefaults(VertexContents to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexNormal.inst.myIndex;
            }

            public VertexNormalTrg(int index) : base(dataSize, index)
            {

            }
        }

        private class VertexColorTrg : VertexDataTarget
        {
            private const int dataSize = 4;

            public override void Set(Vector4[] dta)
            {
                var cols = new Color[dta.Length];
                for (var i = 0; i < dta.Length; i++)
                    cols[i] = dta[i];

                CurMeshDta.mesh.colors = cols;
            }

            public override string NameForDisplayPEGI => "color";
            
            public override string GetFieldName(int ind)
            {
                switch (ind) {
                    case 0: return "R";
                    case 1: return "G";
                    case 2: return "B";
                    case 3: return "A";
                    default: return "Error";
                }
            }

            public override void SetDefaults(VertexContents to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexColor.inst.myIndex;
                for (var i = 0; i < 4; i++)
                    to.vals[i].typeIndex = VertexColor.inst.myIndex;
            }

            public VertexColorTrg(int index) : base(dataSize, index)
            {

            }
        }

        public static readonly VertexDataTarget[] dataTargets = {
        new VertexPosTrg(0) , new VertexUVTrg(1) , new VertexUVTrg(2) , new VertexUVTrg(3),
        new VertexUVTrg(4),   new VertexNormalTrg(5), new VertexTangentTrg(6),  new VertexColorTrg(7)

    };


        #region Data Types

        public class VertexPos : VertexDataType
        {
            public static VertexPos inst;
            private const int dataSize = 3;

            private Vector3[] _vertices;

            public override void GenerateIfNull()
            {
                if (_vertices == null)
                    _vertices = CurMeshDta._position;
 
            }

            public override float[] GetValue(int no)
            {
                var vrts = _vertices;
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = vrts[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg) => _vertices;
            

            public override string NameForDisplayPEGI => "position";
            
            public VertexPos(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear() => _vertices = null;
            

        }

        public class VertexUV : VertexDataType
        {
            private static int _uvEnum = 0;
            private readonly int _myUvIndex;
            public static readonly VertexUV[] inst = new VertexUV[2];

            private const int dataSize = 2;

            Vector2[] v2s;

            public override void GenerateIfNull() {
                if (v2s == null)
                    v2s =  (_myUvIndex == 0) ? CurMeshDta._uv : CurMeshDta._uv1;
            }

            public override Vector2[] GetV2(VertexDataTarget trg) => v2s;
            

            public override float[] GetValue(int no)  {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = v2s[i][no];

                return _chanelMedium;
            }

            public override void Clear() {
                v2s = null;
            }

            public override string NameForDisplayPEGI => "uv" + _myUvIndex.ToString();

            public VertexUV(int index) : base(dataSize, index)
            {

                _myUvIndex = _uvEnum;
                inst[_myUvIndex] = this;
                _uvEnum++;
            }

        }

        public class VertexTangent : VertexDataType
        {
            public static VertexTangent inst;
            private const int dataSize = 4;

            Vector4[] v4s;

            public override void GenerateIfNull() {
                if (v4s == null)
                    v4s = CurMeshDta._tangents;
            }

            public override Vector4[] GetV4(VertexDataTarget trg)
            {
                if (trg.GetType() == typeof(VertexTangentTrg))
                {
                    CurMeshDta.mesh.RecalculateTangents();
                    return null;
                }

                return v4s;
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = v4s[i][no];

                return _chanelMedium;
            }

            public override void Clear() => v4s = null;
            
            public override string NameForDisplayPEGI => "tangent";
            
            public VertexTangent(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class VertexNormal : VertexDataType
        {
            public static VertexNormal inst;
            private const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull() {
                if (v3norms == null)
                    v3norms = CurMeshDta._normals;
            }
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = v3norms[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                GenerateIfNull();
                return v3norms;
            }

            public override void Clear() => v3norms = null;
            
            public override string NameForDisplayPEGI => "normal";
            
            public VertexNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        private class VertexSharpNormal : VertexDataType
        {
            private static VertexSharpNormal inst;
            private const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull()
            {
                if (v3norms == null)
                    v3norms = CurMeshDta._sharpNormals;

            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = v3norms[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg) => v3norms;
            
            public override void Clear() => v3norms = null;
            
            public override string NameForDisplayPEGI => "SharpNormal";
            
            public VertexSharpNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class VertexColor : VertexDataType
        {
            public static VertexColor inst;
            const int dataSize = 4;

            Vector4[] cols;

            public override void GenerateIfNull()
            {
                if (cols != null) return;
                
                var tmp = CurMeshDta._colors;

                cols = new Vector4[vcnt];

                for (var i = 0; i < vcnt; i++)
                    cols[i] = tmp[i].ToVector4();
            }

            public override Vector4[] GetV4(VertexDataTarget trg) => cols;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = cols[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "Color";
            
            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "R";
                    case 1: return "G";
                    case 2: return "B";
                    case 3: return "A";
                    default: return "Error";
                }
            }

            public VertexColor(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear() => cols = null;
            
        }

        public class VertexIndex : VertexDataType
        {
            private static VertexIndex _inst;
            private const int dataSize = 1;

            int[] inds;

            public override void GenerateIfNull()
            {
                if (inds == null) 
                    inds = CurMeshDta._vertexIndex;
                
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = inds[i];

                return _chanelMedium;
            }
            
            public override string GetFieldName(int ind) => "index";
            
            public override string NameForDisplayPEGI => "vertexIndex";
            
            public VertexIndex(int index) : base(dataSize, index)
            {
                _inst = this;
            }

            public override void Clear() => inds = null;
            
        }

        public class VertexShadow : VertexDataType
        {
            private static VertexShadow inst;
            private const int dataSize = 4;

            Vector4[] _shadows;

            public override void GenerateIfNull() {
                if (_shadows == null)
                        _shadows = CurMeshDta._shadowBake;

            }

            public override Vector4[] GetV4(VertexDataTarget trg) => _shadows;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = _shadows[i][no];

                return _chanelMedium;
            }

            public override string GetFieldName(int ind) => "light " + ind;
            
            public override string NameForDisplayPEGI => "shadow";
            
            public VertexShadow(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear() => _shadows = null;
            
        }

        private class VertexAtlasTextures : VertexDataType
        {
            private static VertexAtlasTextures inst;
            private const int dataSize = 4;

            Vector4[] textureNumbers;


            public override void GenerateIfNull() {
                if (textureNumbers == null)
                    textureNumbers = CurMeshDta._trisTextures;
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = textureNumbers[i][no];

                return _chanelMedium;
            }

            public override Vector4[] GetV4(VertexDataTarget trg)
            {
                return textureNumbers;
            }


            public override void Clear() => textureNumbers = null;
            

            public override string GetFieldName(int ind) => "tex " + ind;
            
            public override string NameForDisplayPEGI => "Atlas Texture";
            
            public VertexAtlasTextures(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }
        
        public class VertexNull : VertexDataType
        {
            public static VertexNull inst;
            private const int dataSize = 1;
            private float[] zeroVal = null;

            public override void GenerateIfNull() => zeroVal = new float[vcnt];
            
            public override float[] GetValue(int no) => zeroVal;
            
            public override string GetFieldName(int ind) => "null";
            
            public override string NameForDisplayPEGI => "null";
            
            public VertexNull(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear()
            {
                zeroVal = null;
            }
        }

        public class VertexEdge : VertexDataType
        {
            private static VertexEdge inst;
            const int dataSize = 4;

            private Vector4[] _edges;

            public override void GenerateIfNull()
            {
                if (_edges == null)
                    _edges = CurMeshDta._edgeData;
            }

            public override Vector4[] GetV4(VertexDataTarget trg) => _edges;
            

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = _edges[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "Edge";
            
            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    case 3: return "Strength";
                    default: return "Error";
                }
            }

            public VertexEdge(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear() => _edges = null;
            
        }

        public class VertexEdgeByWeight : VertexDataType
        {
            private static VertexEdgeByWeight inst;
            private const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {
                if (edges == null)
                    edges = CurMeshDta._edgeDataByWeight;
            }

            public override Vector3[] GetV3(VertexDataTarget trg) => edges;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = edges[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "Edge * weight";
            
            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    default: return "Error";
                }
                
            }

            public VertexEdgeByWeight(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        private class EdgeNormal_0 : VertexDataType
        {
            private static EdgeNormal_0 inst;
            private const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeNormal_0_OrSharp;

            }

            public override Vector3[] GetV3(VertexDataTarget trg) => edges;
            

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = edges[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "LineNormal_0";
            

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    default: return "Error";
                }
                
            }

            public EdgeNormal_0(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear() => edges = null;
            
        }

        public class EdgeNormal_1 : VertexDataType
        {
            private static EdgeNormal_1 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeNormal_1_OrSharp;

            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = edges[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "LineNormal_1";
            

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    default: return "Error";
                }
                
            }

            public EdgeNormal_1(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear() => edges = null;
            
        }

        public class EdgeNormal_2 : VertexDataType
        {
            private static EdgeNormal_2 inst;
            private const int dataSize = 3;

            private Vector3[] _edges;

            public override void GenerateIfNull()
            {

                if (_edges == null)
                    _edges = CurMeshDta._edgeNormal_2_OrSharp;

            }

            public override Vector3[] GetV3(VertexDataTarget trg) => _edges;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vcnt; i++)
                    _chanelMedium[i] = _edges[i][no];

                return _chanelMedium;
            }

            public override string NameForDisplayPEGI => "LineNormal_2";
            

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    default:  return "Error";
                }
               
            }

            public EdgeNormal_2(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear()
            {
                _edges = null;
            }
        }

        public static readonly VertexDataType[] dataTypes = {

            new VertexPos(0), new VertexUV(1), new VertexUV(2), new VertexNormal(3),

            new VertexTangent(4), new VertexSharpNormal(5), new VertexColor(6), new VertexIndex(7),

            new VertexShadow(8), new VertexAtlasTextures(9),  new VertexNull(10), new VertexEdge(11),

            new EdgeNormal_0(12), new EdgeNormal_1(13), new EdgeNormal_2(14), new VertexEdgeByWeight(15)

        };

        private static string[] _typesNames;
        
        public static string[] GetAllTypesNames()
        {
            if (_typesNames.IsNullOrEmpty())
            {
                _typesNames = new string[dataTypes.Length];

                for (var i = 0; i < dataTypes.Length; i++)
                    _typesNames[i] = dataTypes[i].ToPEGIstring();
            }

            return _typesNames;
        }

        public static List<VertexDataType> GetTypesBySize(int size)
        {

            var tmp = new List<VertexDataType>();

            foreach (var d in dataTypes)
                if (d.chanelsNeed == size)
                    tmp.Add(d);

            return tmp;
        }

        #endregion

    }
}