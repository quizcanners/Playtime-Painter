using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace Playtime_Painter
{
    
    public class MeshPackagingProfile : AbstractCfg, IPEGI, IGotName
    {
        public List<VertexDataLink> dtaLnks;

        public string name;

        public const string FolderName = "Mesh Profiles";

        private bool UsesDestination<T>() where T : VertexDataDestination {
            foreach (var s in dtaLnks)
                if (s.Destination.IsDestinationType<T>())
                    return s.enabled;

            return false;
        }

        private bool UsesSource<T>() where T : VertexDataSource
        {
            foreach (var s in dtaLnks)
                if (s.enabled)
                {
                    if (s.sameSizeDataIndex != -1) {
                        if (s.SameSizeValue.IsSourceType<T>())
                            return true;
                        continue;
                    }
                    
                    foreach (var d in s.links)
                        if (d.DataSource.IsSourceType<T>())
                            return true;
                    
                }
            
            return false;
        }

        public bool UsesColor => UsesSource<VertexDataTypes.VertexColor>();

        public bool WritesColor => UsesDestination<VertexDataTypes.VertexColorTrg>();

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
                UnityUtils.RefreshAssetDatabase();
                (name + " Saved to " + path).showNotificationIn3D_Views();
            }


            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType).nl(ref changed)) {
               
                var mSol = new MeshPackagingProfile();
                mSol.Decode(FileLoadUtils.LoadTextAsset(myType));

                PainterCamera.Data.meshPackagingSolutions.Add(mSol);
                PlaytimePainter.inspected.selectedMeshProfile = PainterCamera.Data.meshPackagingSolutions.Count - 1;
            }
            #endif

            foreach (var s in dtaLnks) 
                s.Inspect().nl(ref changed);

            return changed;
        }
        #endif
        #endregion

        public bool Repack(MeshConstructor sm)
        {

            if (!sm.Valid) {
                Debug.Log("Got no data for mesh regeneration. ");
                return false;
            }

            sm.mesh.Clear();

            VertexDataTypes.CurMeshDta = sm;

            foreach (var vs in dtaLnks)
                if (vs.enabled) vs.Pack();

            foreach (var vt in VertexDataTypes.DataTypes)
                vt.Clear();

            return true;
        }

        public bool UpdatePackage<T>(MeshConstructor sm) where T : VertexDataSource {

            VertexDataTypes.CurMeshDta = sm;

            foreach (var vs in dtaLnks)
                if (vs.enabled && !vs.FilterOutData<T>()) vs.Pack();

            foreach (var vt in VertexDataTypes.DataTypes)
                vt.Clear();

            return true;
        }

        #region Encode & Decode
        public override CfgEncoder Encode() 
        {
            var cody = new CfgEncoder();

            cody.Add_String("n", name);
            cody.Add_IfNotEmpty("sln", dtaLnks);


            return cody;
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "n": name = data; break;
                case "sln":  data.Decode_List(out dtaLnks); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public MeshPackagingProfile() {

            name = "unNamed";
            var targets = VertexDataTypes.DataDestinations;
            dtaLnks = new List<VertexDataLink>(); 
            foreach (var t in targets)
                dtaLnks.Add(new VertexDataLink(t));

        }

    }

    #region Data
    public abstract class VertexDataDestination : IGotDisplayName
    {
        public byte channelsHas;
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

        public virtual void SetDefaults(VertexDataLink to)
        {
            for (int i = 0; i < to.links.Count; i++)
                to.links[i].dstIndex = i;
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

        public VertexDataLink GetMySolution()
        {
            MeshPackagingProfile pf = VertexDataTypes.CurMeshDta.profile;
            return pf.dtaLnks[myIndex];
        }

        public VertexDataDestination(byte chanCont, int index)
        {
            channelsHas = chanCont;
            myIndex = index;
        }
    }

    public abstract class VertexDataSource: IGotDisplayName
    {
        public byte channelsNeed;
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

        public VertexDataSource(byte chanCnt, int index)
        {
            myIndex = index;
            channelsNeed = chanCnt;

        }

        public abstract void Clear();

        public virtual void GenerateIfNull() => Debug.Log(this.GetType() + " does not generate any data");
        
        public virtual float[] GetValue(int no) => null;
        
        public virtual Vector2[] GetV2(VertexDataDestination trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector2 array");
            return null;
        }

        public virtual Vector3[] GetV3(VertexDataDestination trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector3 array");
            return null;
        }

        public virtual Vector4[] GetV4(VertexDataDestination trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector4 array");
            return null;
        }

    }

    public class VertexDataLink : AbstractCfg, IPEGI
    {
        public int sameSizeDataIndex = -1;
        private int _targetIndex;
        public List<VertexDataChannelLink> links = new List<VertexDataChannelLink>();
        public bool enabled;

        public VertexDataSource SameSizeValue => (sameSizeDataIndex >= 0) ? VertexDataTypes.DataTypes[sameSizeDataIndex] : null;

        public VertexDataDestination Destination
        {
            get { return VertexDataTypes.DataDestinations[_targetIndex]; }
            set { _targetIndex = value.myIndex; }
        }

        //protected bool FilterOutSameSizeData => VertexDataTypes.dataSourceTypeFilter != null && SameSizeValue.GetType() != VertexDataTypes.dataSourceTypeFilter;

        public bool FilterOutData<T>() where T: VertexDataSource
        {
            if (sameSizeDataIndex != -1)
                return !SameSizeValue.IsSourceType<T>(); 
               
            foreach (var l in links)
                if (l.DataSource.IsSourceType<T>())
                    return false;
                
            return true;
        }

        #region Encode & Decode
        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder();

            cody.Add_IfTrue("en", enabled)
            .Add_IfNotZero("t", _targetIndex);

            if (enabled)
            {
                if (sameSizeDataIndex == -1)
                    cody.Add_IfNotEmpty("vals", links);
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
                case "vals": data.Decode_List(out links); sameSizeDataIndex = -1; break;
                case "sameSize": sameSizeDataIndex = data.ToInt(); InitVals(); break;

                default: return false;
            }
            return true;
        }

        public override void Decode(string data)
        {
            base.Decode(data);
            InitVals();
        }

        #endregion

        #region Inspector
#if PEGI
        public virtual bool Inspect()
        {
            var changed = false;

            (Destination.ToPegiString() + ":").toggle(80, ref enabled).changes(ref changed);

            if (!enabled) return changed;

            var tps = VertexDataTypes.GetTypesBySize(links.Count);
            var nms = new string[tps.Count + 1];

            for (var i = 0; i < tps.Count; i++)
                nms[i] = tps[i].ToPegiString();

            nms[tps.Count] = "Custom";

            var selected = tps.Count;

            if (SameSizeValue != null)
                for (var i = 0; i < tps.Count; i++)
                    if (tps[i] == SameSizeValue) {
                        selected = i;
                        break;
                    }

            pegi.select(ref selected, nms).nl(ref changed);

            if (selected >= tps.Count)
                sameSizeDataIndex = -1;
            else
                sameSizeDataIndex = tps[selected].myIndex;

            var allDataNames = VertexDataTypes.GetAllTypesNames();

            if (SameSizeValue == null)
            {
                for (var i = 0; i < links.Count; i++)
                {
                    var v = links[i];

                    changed |= Destination.GetFieldName(i).select(40, ref v.srcIndex, allDataNames);

                    var typeFields = new string[v.DataSource.channelsNeed];

                    for (var j = 0; j < typeFields.Length; j++)
                        typeFields[j] = v.DataSource.GetFieldName(j);

                    changed |= pegi.select(ref v.dstIndex, typeFields).nl();

                    typeFields.ClampIndexToLength(ref v.dstIndex);
                }
            }
            "**************************************************".nl();

            return changed;
        }
#endif
        #endregion

        public VertexDataLink() { }

        public VertexDataLink(VertexDataDestination ntrg)
        {
            Destination = ntrg;
            InitVals();
            ntrg.SetDefaults(this);
        }

        private void InitVals()
        {
            while (links.Count < Destination.channelsHas)
                links.Add(new VertexDataChannelLink());
        }

        public void Pack()
        {
            try
            {
                switch (Destination.channelsHas)
                {
                    case 3: PackVector3(); break;
                    case 4: PackVector4(); break;
                    default: Debug.Log("No packaging function for Vector" + Destination.channelsHas + " target."); break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception in {0}  :  {1}".F(Destination.ToPegiString(), ex.ToString()));
            }
        }

        private void PackVector3()
        {

            Vector3[] ar;

            if (SameSizeValue != null)
            {
                //if (FilterOutSameSizeData)
                  //  return;

                SameSizeValue.GenerateIfNull();

                ar = SameSizeValue.GetV3(Destination);

            }
            else
            {
                ar = new Vector3[VertexDataTypes.vCnt];

                for (var i = 0; i < VertexDataTypes.vCnt; i++)
                    ar[i] = new Vector3();

                for (var i = 0; i < 3; i++)
                {
                    var v = links[i];
                    var tmp = v.GetDataArray();

                    if (tmp == null)
                        continue;

                    for (var j = 0; j < VertexDataTypes.vCnt; j++)
                        ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Destination.Set(ar);

        }

        private void PackVector4()
        {

            Vector4[] ar;

            if (SameSizeValue != null)
            {
                //if (FilterOutSameSizeData)
                  //  return;

                SameSizeValue.GenerateIfNull();

                ar = SameSizeValue.GetV4(Destination);

            }
            else
            {

                ar = new Vector4[VertexDataTypes.vCnt];

                for (var i = 0; i < VertexDataTypes.vCnt; i++)
                    ar[i] = new Vector4();

                for (var i = 0; i < 4; i++)
                {
                    var v = links[i];
                    var tmp = v.GetDataArray();

                    if (tmp == null) continue;

                    for (var j = 0; j < VertexDataTypes.vCnt; j++)
                        ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Destination.Set(ar);

        }

    }
    
    public class VertexDataChannelLink : AbstractCfg {

        public int srcIndex;
        public int dstIndex;

        public VertexDataSource DataSource => VertexDataTypes.DataTypes[srcIndex]; 

        public float[] GetDataArray() {
            DataSource.GenerateIfNull();
            return DataSource.GetValue(dstIndex);
        }

        #region Encode & Decode
        public override CfgEncoder Encode() {
            CfgEncoder cody = new CfgEncoder();
            cody.Add_IfNotZero("t", srcIndex);
            cody.Add_IfNotZero("v", dstIndex);
            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "t": srcIndex = data.ToInt(); break;
                case "v": dstIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }
    #endregion

    public static class VertexDataTypes {

        #region Static

        public static int GetMeshProfileByTag(this Material mat)
        {
            if (!mat)
                return 0;

            var name = mat.Get(ShaderTags.MeshSolution, false, "Standard");

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
                vCnt = value.vertexCount;
                _chanelMedium = new float[vCnt];
            }

        }

        public static int vCnt;
        private static float[] _chanelMedium;

        public static bool IsDestinationType<T>(this VertexDataDestination vd) where T : VertexDataDestination => vd.GetType() == typeof(T);
        public static bool IsSourceType<T>(this VertexDataSource vd) where T : VertexDataSource => vd != null && vd.GetType() == typeof(T);

        #endregion

        public class VertexPosTrg : VertexDataDestination
        {
            private const int dataSize = 3;

            public override void Set(Vector3[] dta)
            {
                CurMeshDta.mesh.vertices = dta;
                if (CurMeshDta.triangles == null) return;
                
                CurMeshDta.mesh.subMeshCount = CurMeshDta.triangles.Length;
                for (var sm = 0; sm < CurMeshDta.triangles.Length; sm++)
                    CurMeshDta.mesh.SetTriangles(CurMeshDta.triangles[sm], sm, true);
                
            }

            public override string NameForDisplayPEGI => "position";
            
            public override void SetDefaults(VertexDataLink to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexPos.inst.myIndex;
            }

            public VertexPosTrg(int index) : base(dataSize, index)
            {

            }

        }

        public class VertexUVTrg : VertexDataDestination
        {
            private const int dataSize = 4;

            private int MyUvChanel() => (myIndex - 1); 

            public override void Set(Vector4[] dta)
            {

                var vs = GetMySolution();
                
                if ((vs.SameSizeValue != null) || (vs.links[2].DataSource != VertexNull.inst) || (vs.links[3].DataSource != VertexNull.inst))
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
            
            public override void SetDefaults(VertexDataLink to)
            {
                base.SetDefaults(to);

                if (myIndex != 1) return;

                to.enabled = true;

                var ind = VertexUv.inst[0].myIndex;

                to.links[0].srcIndex = ind;
                to.links[0].dstIndex = 0;
                to.links[1].srcIndex = ind;
                to.links[1].dstIndex = 1;

                ind++;

                to.links[2].srcIndex = ind;
                to.links[2].dstIndex = 0;
                to.links[3].srcIndex = ind;
                to.links[3].dstIndex = 1;

            }

            public VertexUVTrg(int index) : base(dataSize, index)
            {

            }

        }

        public class VertexTangentTrg : VertexDataDestination
        {
            private const int dataSize = 4;

            public override void Set(Vector4[] dta)
            {
                CurMeshDta.mesh.tangents = dta;
            }

             public override string NameForDisplayPEGI => "tangent";
            
            public override void SetDefaults(VertexDataLink to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexTangent.inst.myIndex;
            }

            public VertexTangentTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class VertexNormalTrg : VertexDataDestination
        {
            private const int dataSize = 3;

            public override void Set(Vector3[] dta) =>  CurMeshDta.mesh.normals = dta;
            
            public override string NameForDisplayPEGI => "normal";
            
            public override void SetDefaults(VertexDataLink to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexNormal.inst.myIndex;
            }

            public VertexNormalTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class VertexColorTrg : VertexDataDestination
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

            public override void SetDefaults(VertexDataLink to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexColor.inst.myIndex;
                for (var i = 0; i < 4; i++)
                    to.links[i].srcIndex = VertexColor.inst.myIndex;
            }

            public VertexColorTrg(int index) : base(dataSize, index)
            {

            }
        }

        public static readonly VertexDataDestination[] DataDestinations = {
        new VertexPosTrg(0) , new VertexUVTrg(1) , new VertexUVTrg(2) , new VertexUVTrg(3),
        new VertexUVTrg(4),   new VertexNormalTrg(5), new VertexTangentTrg(6),  new VertexColorTrg(7)

    };


        #region Data Types

        public class VertexPos : VertexDataSource
        {
            public static VertexPos inst;
            private const int dataSize = 3;

            private Vector3[] _vertices;

            public override void GenerateIfNull()
            {
                if (_vertices == null)
                    _vertices = CurMeshDta.Position;
 
            }

            public override float[] GetValue(int no)
            {
                var vrts = _vertices;
                for (var i = 0; i < vCnt; i++)
                    _chanelMedium[i] = vrts[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataDestination trg) => _vertices;
            

            public override string NameForDisplayPEGI => "position";
            
            public VertexPos(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear() => _vertices = null;
            

        }

        public class VertexUv : VertexDataSource
        {
            private static int _uvEnum;
            private readonly int _myUvIndex;
            public static readonly VertexUv[] inst = new VertexUv[2];

            private const int dataSize = 2;

            Vector2[] v2s;

            public override void GenerateIfNull() {
                if (v2s == null)
                    v2s =  (_myUvIndex == 0) ? CurMeshDta.Uv : CurMeshDta.Uv1;
            }

            public override Vector2[] GetV2(VertexDataDestination trg) => v2s;
            
            public override float[] GetValue(int no)  {
                for (var i = 0; i < vCnt; i++)
                    _chanelMedium[i] = v2s[i][no];

                return _chanelMedium;
            }

            public override void Clear() {
                v2s = null;
            }

            public override string NameForDisplayPEGI => "uv" + _myUvIndex.ToString();

            public VertexUv(int index) : base(dataSize, index)
            {

                _myUvIndex = _uvEnum;
                inst[_myUvIndex] = this;
                _uvEnum++;
            }

        }

        public class VertexTangent : VertexDataSource
        {
            public static VertexTangent inst;
            private const int dataSize = 4;

            Vector4[] v4s;

            public override void GenerateIfNull() {
                if (v4s == null)
                    v4s = CurMeshDta.Tangents;
            }

            public override Vector4[] GetV4(VertexDataDestination trg)
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
                for (var i = 0; i < vCnt; i++)
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

        public class VertexNormal : VertexDataSource
        {
            public static VertexNormal inst;
            private const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull() {
                if (v3norms == null)
                    v3norms = CurMeshDta.Normals;
            }
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
                    _chanelMedium[i] = v3norms[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataDestination trg)
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

        private class VertexSharpNormal : VertexDataSource
        {
            private static VertexSharpNormal inst;
            private const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull()
            {
                if (v3norms == null)
                    v3norms = CurMeshDta.SharpNormals;

            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
                    _chanelMedium[i] = v3norms[i][no];

                return _chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataDestination trg) => v3norms;
            
            public override void Clear() => v3norms = null;
            
            public override string NameForDisplayPEGI => "SharpNormal";
            
            public VertexSharpNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class VertexColor : VertexDataSource
        {
            public static VertexColor inst;
            const int dataSize = 4;

            Vector4[] cols;

            public override void GenerateIfNull()
            {
                if (cols != null) return;
                
                var tmp = CurMeshDta.Colors;

                cols = new Vector4[vCnt];

                for (var i = 0; i < vCnt; i++)
                    cols[i] = tmp[i].ToVector4();
            }

            public override Vector4[] GetV4(VertexDataDestination trg) => cols;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

        public class VertexIndex : VertexDataSource
        {
            private static VertexIndex _inst;
            private const int dataSize = 1;

            int[] inds;

            public override void GenerateIfNull()
            {
                if (inds == null) 
                    inds = CurMeshDta.VertexIndex;
                
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

        public class VertexShadow : VertexDataSource
        {
            private static VertexShadow inst;
            private const int dataSize = 4;

            Vector4[] _shadows;

            public override void GenerateIfNull() {
                if (_shadows == null)
                        _shadows = CurMeshDta.ShadowBake;

            }

            public override Vector4[] GetV4(VertexDataDestination trg) => _shadows;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

        private class VertexAtlasTextures : VertexDataSource
        {
            private static VertexAtlasTextures inst;
            private const int dataSize = 4;

            Vector4[] textureNumbers;


            public override void GenerateIfNull() {
                if (textureNumbers == null)
                    textureNumbers = CurMeshDta.TriangleTextures;
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
                    _chanelMedium[i] = textureNumbers[i][no];

                return _chanelMedium;
            }

            public override Vector4[] GetV4(VertexDataDestination trg)
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
        
        public class VertexNull : VertexDataSource
        {
            public static VertexNull inst;
            private const int dataSize = 1;
            private float[] zeroVal = null;

            public override void GenerateIfNull() => zeroVal = new float[vCnt];
            
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

        public class VertexEdge : VertexDataSource
        {
            private static VertexEdge inst;
            const int dataSize = 4;

            private Vector4[] _edges;

            public override void GenerateIfNull()
            {
                if (_edges == null)
                    _edges = CurMeshDta.EdgeData;
            }

            public override Vector4[] GetV4(VertexDataDestination trg) => _edges;
            

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

        public class VertexEdgeByWeight : VertexDataSource
        {
            private static VertexEdgeByWeight inst;
            private const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {
                if (edges == null)
                    edges = CurMeshDta.EdgeDataByWeight;
            }

            public override Vector3[] GetV3(VertexDataDestination trg) => edges;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

        private class EdgeNormal0 : VertexDataSource
        {
            private static EdgeNormal0 inst;
            private const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta.EdgeNormal0OrSharp;

            }

            public override Vector3[] GetV3(VertexDataDestination trg) => edges;
            

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

            public EdgeNormal0(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear() => edges = null;
            
        }

        public class EdgeNormal1 : VertexDataSource
        {
            private static EdgeNormal1 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta.EdgeNormal1OrSharp;

            }

            public override Vector3[] GetV3(VertexDataDestination trg)
            {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

            public EdgeNormal1(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear() => edges = null;
            
        }

        public class EdgeNormal2 : VertexDataSource
        {
            private static EdgeNormal2 inst;
            private const int dataSize = 3;

            private Vector3[] _edges;

            public override void GenerateIfNull()
            {

                if (_edges == null)
                    _edges = CurMeshDta.EdgeNormal2OrSharp;

            }

            public override Vector3[] GetV3(VertexDataDestination trg) => _edges;
            
            public override float[] GetValue(int no)
            {
                for (var i = 0; i < vCnt; i++)
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

            public EdgeNormal2(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear()
            {
                _edges = null;
            }
        }

        public static readonly VertexDataSource[] DataTypes = {

            new VertexPos(0), new VertexUv(1), new VertexUv(2), new VertexNormal(3),

            new VertexTangent(4), new VertexSharpNormal(5), new VertexColor(6), new VertexIndex(7),

            new VertexShadow(8), new VertexAtlasTextures(9),  new VertexNull(10), new VertexEdge(11),

            new EdgeNormal0(12), new EdgeNormal1(13), new EdgeNormal2(14), new VertexEdgeByWeight(15)

        };

        private static string[] _typesNames;
        
        public static string[] GetAllTypesNames()
        {
            if (!_typesNames.IsNullOrEmpty()) return _typesNames;

            _typesNames = new string[DataTypes.Length];

            for (var i = 0; i < DataTypes.Length; i++)
                _typesNames[i] = DataTypes[i].ToPegiString();

            return _typesNames;
        }

        public static List<VertexDataSource> GetTypesBySize(int size)
        {

            var tmp = new List<VertexDataSource>();

            foreach (var d in DataTypes)
                if (d.channelsNeed == size)
                    tmp.Add(d);

            return tmp;
        }

        #endregion

    }
}