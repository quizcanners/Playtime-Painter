using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{


    [Serializable]
    public class MeshPackagingProfile : Abstract_STD, IPEGI
    {
        public List<VertexSolution> sln;

        public string name = "";

        public const string folderName = "Mesh Profiles";
        #if PEGI
        public virtual bool Inspect()
        {

            "Profile Name: ".edit(80, ref name);

#if UNITY_EDITOR

            string path = PainterCamera.Data.meshesFolderName + "/" + folderName;
            if (icon.Save.Click("Save To:" + path, 25).nl())
            {
                this.SaveToAssets(path, name);
                UnityHelperFunctions.RefreshAssetDatabase();
                (name + " Saved to " + path).showNotificationIn3D_Views();
            }


            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType).nl()) {
               
                var msol = new MeshPackagingProfile();
                msol.Decode(StuffLoader.LoadTextAsset(myType));

                PainterCamera.Data.meshPackagingSolutions.Add(msol);
                PlaytimePainter.inspectedPainter.selectedMeshProfile = PainterCamera.Data.meshPackagingSolutions.Count - 1;
            }
#endif


            bool changed = false;
            for (int i = 0; i < sln.Count; i++)
                changed |= sln[i].Inspect().nl();

            return changed;
        }
#endif
        public override string ToString()
        {
            return name;
        }

        public bool Repack(MeshConstructor sm)
        {


            if (!sm.Valid)
            {//(sm.verts == null) || (sm.tris == null) || (sm.verts.Length < 3) || (sm.tris.TotalCount() < 3)) {
                Debug.Log("Got no stuff to regenerate mesh. ");
                return false;
            }

            sm.mesh.Clear();

            MeshSolutions.CurMeshDta = sm;

            MeshSolutions.dataTypeFilter = null;

            foreach (VertexSolution vs in sln)
                if (vs.enabled) vs.Pack();

            foreach (VertexDataType vt in MeshSolutions.types)
                vt.Clear();

            return true;
        }

        public bool UpdatePackage(MeshConstructor sm, Type dataType) {

           // if (!sm.valid)
           //{//(sm.verts == null) || (sm.tris == null) || (sm.verts.Length < 3) || (sm.tris.TotalCount() < 3)) {
           //  Debug.Log("Got no stuff to regenerate mesh. ");
           //  return false;
           //}

            MeshSolutions.CurMeshDta = sm;

            MeshSolutions.dataTypeFilter = dataType;

            foreach (VertexSolution vs in sln)
                if (vs.enabled) vs.Pack();

            foreach (VertexDataType vt in MeshSolutions.types)
                vt.Clear();

            return true;
        }

        public override StdEncoder Encode() 
        {
            StdEncoder cody = new StdEncoder();

            cody.Add_String("n", name);
            cody.Add_IfNotEmpty("sln", sln);


            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "sln":  data.Decode_List(out sln); break;
                default: return false;
            }
            return true;
        }

        public const string stdTag_vertSol = "vertSol";

        

        public MeshPackagingProfile()
        {
            VertexDataTarget[] trgs = MeshSolutions.targets;
            sln = new List<VertexSolution>(); //[trgs.Length];
            name = "unnamedd";
            for (int i = 0; i < trgs.Length; i++)
                sln.Add(new VertexSolution(trgs[i]));
        }

    }



    public abstract class VertexDataTarget
    {
        public byte chanelsHas;
        public int myIndex;

        public virtual void Set(Vector3[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for " + this.GetType());
        }

        public virtual void Set(Vector4[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for array of " + this.GetType());
        }

        public abstract string Name();

        public virtual void SetDefaults(VertexSolution to)
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

        public VertexSolution GetMySolution()
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

    public abstract class VertexDataType
    {
        public byte chanelsNeed;
        public int myIndex;
       // public abstract string name();

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

        public virtual void GenerateIfNull()
        {
            Debug.Log(this.GetType() + " does not generate any data");
        }

        public virtual float[] GetValue(int no)
        {
            return null;
        }

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


    [Serializable]
    public class VertexDataValue : Abstract_STD {

        public int typeIndex;
        public int valueIndex;

        public VertexDataType VertDataType { get { return MeshSolutions.types[typeIndex]; } }

        public float[] GetDataArray() {
            VertDataType.GenerateIfNull();
            return VertDataType.GetValue(valueIndex);
        }

        public override StdEncoder Encode() {
            StdEncoder cody = new StdEncoder();
            cody.Add("t", typeIndex);
            cody.Add("v", valueIndex);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "t": typeIndex = data.ToInt(); break;
                case "v": valueIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

     

    }


    [Serializable]
    public class VertexSolution : Abstract_STD, IPEGI
    {
        public int sameSizeDataIndex;
        public int targetIndex;
        public bool enabled;
        public static bool showHint;


        public VertexDataType SameSizeValue { get { if (sameSizeDataIndex >= 0) return MeshSolutions.types[sameSizeDataIndex]; else return null; } }
        public VertexDataTarget Target { get { return MeshSolutions.targets[targetIndex]; } set { targetIndex = value.myIndex; } }

        public List<VertexDataValue> vals;
        #if PEGI
        public virtual bool Inspect()
        {
            bool changed = false;

            (Target.Name() + ":").toggle(80, ref enabled);

            if (enabled)
            {

                List<VertexDataType> tps = MeshSolutions.GetTypesBySize(vals.Count);
                string[] nms = new string[tps.Count + 1];

                for (int i = 0; i < tps.Count; i++)
                    nms[i] = tps[i].ToString();

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

                if (selected >= tps.Count) sameSizeDataIndex = -1;
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

                        v.valueIndex = v.valueIndex.ClampZeroTo(typeFields.Length);
                    }
                }
                "**************************************************".nl();
            }

            return changed;
        }
#endif
        public VertexSolution() {

        }

        public VertexSolution(VertexDataTarget ntrg) {
            Target = ntrg;

            InitVals();
            sameSizeDataIndex = -1;
         
            ntrg.SetDefaults(this);
        }

        void InitVals() {
            vals = new List<VertexDataValue>();
            for (int i = 0; i < Target.chanelsHas; i++)
                vals.Add(new VertexDataValue());
        }

        public void Pack()
        {
            switch (Target.chanelsHas)
            {
                case 3: PackVector3(); break;
                case 4: PackVector4(); break;
                default: Debug.Log("No packaging function for Vector" + Target.chanelsHas + " taget."); break;
            }
        }

        void PackVector3() {

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

                for (int i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector3();

                for (int i = 0; i < 3; i++) {
                    VertexDataValue v = vals[i];
                    float[] tmp = v.GetDataArray();

                    if (tmp != null)
                        for (int j = 0; j < MeshSolutions.vcnt; j++)
                            ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Target.Set(ar);

        }

        void PackVector4()
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

                for (int i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector4();

                for (int i = 0; i < 4; i++)
                {
                    VertexDataValue v = vals[i];
                    float[] tmp = v.GetDataArray();

                    if (tmp != null)
                        for (int j = 0; j < MeshSolutions.vcnt; j++)
                            ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                Target.Set(ar);

        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder();

           
            cody.Add_Bool("en", enabled);
            cody.Add("t", targetIndex);

            if (enabled) {
                if (sameSizeDataIndex == -1)
                    cody.Add_IfNotEmpty("vals", vals);
                else
                    cody.Add("sameSize", sameSizeDataIndex);

               
            }
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "en": enabled = data.ToBool();  break;
                case "t": targetIndex = data.ToInt(); if (!enabled) InitVals(); break;
                case "vals": data.Decode_List(out vals); sameSizeDataIndex = -1; break;
                case "sameSize": sameSizeDataIndex = data.ToInt(); InitVals(); break;

                default: return false;
            }
            return true;
        }

       
    }

  
    public static class MeshSolutions
    {

        public static Type dataTypeFilter;

        public const string shaderPreferedPackagingSolution = "Solution";

        public static int GetMeshProfileByTag(this Material mat)
        {
            if (mat == null)
                return 0;

            var name = mat.GetTag(shaderPreferedPackagingSolution, false, "Standard");

            var prf = PainterCamera.Data.meshPackagingSolutions;

            for (int i = 0; i < prf.Count; i++)// (var s in PainterDataAndConfig.dataHolder.meshProfileSolutions)
                if (String.Compare(prf[i].name, name) == 0) return i;

            return 0;//PainterDataAndConfig.dataHolder.meshProfileSolutions[0];
        }

        private static MeshConstructor _curMeshDra;

        public static MeshConstructor CurMeshDta
        {

            get { return _curMeshDra; }

            set { _curMeshDra = value;
                vcnt = value.vertsCount;
                chanelMedium = new float[vcnt];
            }

        }

        public static int vcnt = 0;
        public static float[] chanelMedium = null;
        // ********************************************************* DATA Targets

        public class VertexPosTrg : VertexDataTarget
        {
            const int dataSize = 3;

            public override void Set(Vector3[] dta)
            {
                CurMeshDta.mesh.vertices = dta;
                if (CurMeshDta.tris != null) {
                    CurMeshDta.mesh.subMeshCount = CurMeshDta.tris.Length;
                    for (int sm = 0; sm < CurMeshDta.tris.Length; sm++)
                        CurMeshDta.mesh.SetTriangles(CurMeshDta.tris[sm], sm, true);
                }
            }

            public override string Name()
            {
                return "position";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexPos.inst.myIndex;
            }

            public VertexPosTrg(int index) : base(dataSize, index)
            {

            }

        }

        public class VertexUVTrg : VertexDataTarget
        {
            const int dataSize = 4;

            int MyUVChanel() { return (myIndex - 1); }

            public override void Set(Vector4[] dta)
            {

                VertexSolution vs = GetMySolution();
                
                if ((vs.SameSizeValue != null) || (vs.vals[2].VertDataType != VertexNull.inst) || (vs.vals[3].VertDataType != VertexNull.inst))
                    CurMeshDta.mesh.SetUVs(MyUVChanel(), new List<Vector4>(dta));
                else
                {
                    Vector2[] v2s = new Vector2[dta.Length];

                    for (int i = 0; i < dta.Length; i++)
                    {
                        Vector4 v4 = dta[i];
                        v2s[i] = new Vector2(v4.x, v4.y);
                    }

                    switch (MyUVChanel())
                    {
                        case 0:
                            CurMeshDta.mesh.uv = v2s; break;
                        case 1:
                            CurMeshDta.mesh.uv2 = v2s; break;
                        case 2:
                            CurMeshDta.mesh.uv3 = v2s; break;
                        case 3:
                            CurMeshDta.mesh.uv3 = v2s; break;
                    }
                }
            }

            public override string Name()
            {
                return "UV" + MyUVChanel().ToString();
            }


            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);

                if (myIndex != 1) return;

                to.enabled = true;

                int ind = VertexUV.inst[0].myIndex;

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

        public class VertexTangentTrg : VertexDataTarget
        {
            const int dataSize = 4;

            public override void Set(Vector4[] dta)
            {
                CurMeshDta.mesh.tangents = dta;
            }

            public override string Name()
            {
                return "tangent";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexTangent.inst.myIndex;
            }

            public VertexTangentTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class VertexNormalTrg : VertexDataTarget
        {
            const int dataSize = 3;

            public override void Set(Vector3[] dta)
            {
                CurMeshDta.mesh.normals = dta;
            }

            public override string Name()
            {
                return "normal";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexNormal.inst.myIndex;
            }

            public VertexNormalTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class VertexColorTrg : VertexDataTarget
        {
            const int dataSize = 4;

            public override void Set(Vector4[] dta)
            {
                Color[] cols = new Color[dta.Length];
                for (int i = 0; i < dta.Length; i++)
                    cols[i] = dta[i];

                CurMeshDta.mesh.colors = cols;
            }

            public override string Name()
            {
                return "color";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "R";
                    case 1: return "G";
                    case 2: return "B";
                    case 3: return "A";
                }
                return "Error";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = VertexColor.inst.myIndex;
                for (int i = 0; i < 4; i++)
                    to.vals[i].typeIndex = VertexColor.inst.myIndex;
            }

            public VertexColorTrg(int index) : base(dataSize, index)
            {

            }
        }


        public static VertexDataTarget[] targets = {
        new VertexPosTrg(0) , new VertexUVTrg(1) , new VertexUVTrg(2) , new VertexUVTrg(3),
        new VertexUVTrg(4),   new VertexNormalTrg(5), new VertexTangentTrg(6),  new VertexColorTrg(7)

    };


        // ******************************************************** DATA Types

        public class VertexPos : VertexDataType
        {
            public static VertexPos inst;
            const int dataSize = 3;

            Vector3[] vertices;

            public override void GenerateIfNull()
            {
                if (vertices == null)
                    vertices = CurMeshDta._position;
 
            }

            public override float[] GetValue(int no)
            {
                Vector3[] vrts = vertices;
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = vrts[i][no];

                return chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return vertices;
            }

            public override string ToString()
            {
                return "position";
            }

            public VertexPos(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear()
            {
                vertices = null;
            }

        }

        public class VertexUV : VertexDataType
        {
            static int uvEnum = 0;
            int myUVindex;
            public static VertexUV[] inst = new VertexUV[2];

            const int dataSize = 2;

            Vector2[] v2s;

            public override void GenerateIfNull() {
                if (v2s == null)
                    v2s =  (myUVindex == 0) ? CurMeshDta._uv : CurMeshDta._uv1;
            }

            public override Vector2[] GetV2(VertexDataTarget trg) {
                return v2s;
            }

            public override float[] GetValue(int no)  {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v2s[i][no];

                return chanelMedium;
            }

            public override void Clear() {
                v2s = null;
            }

            public override string ToString()
            {
                return "uv" + myUVindex.ToString();
            }

            public VertexUV(int index) : base(dataSize, index)
            {

                myUVindex = uvEnum;
                inst[myUVindex] = this;
                uvEnum++;
            }

        }

        public class VertexTangent : VertexDataType
        {
            public static VertexTangent inst;
            const int dataSize = 4;

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

                //Debug.Log("Manual tangent recalculation not implemented yet.");
               // return null;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v4s[i][no];

                return chanelMedium;
            }

            public override void Clear()
            {
                v4s = null;
            }

            public override string ToString()
            {
                return "tangent";
            }

            public VertexTangent(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class VertexNormal : VertexDataType
        {
            public static VertexNormal inst;
            const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull() {
                if (v3norms == null)
                    v3norms = CurMeshDta._normals;
            }

           

            public override float[] GetValue(int no)
            {
               

                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v3norms[i][no];

                return chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
               // if (trg.GetType() == typeof(vertexNormalTrg))
               // {
                  //  curMeshDta.mesh.RecalculateNormals();
                 //   return null;
               // }

                GenerateIfNull();


                return v3norms;
            }

            public override void Clear()
            {
                v3norms = null;
            }

            public override string ToString()
            {
                return "normal";
            }

            public VertexNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class VertexSharpNormal : VertexDataType
        {
            public static VertexSharpNormal inst;
            const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull()
            {
                if (v3norms == null)
                    v3norms = CurMeshDta._sharpNormals;

            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v3norms[i][no];

                return chanelMedium;
            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return v3norms;
            }

            public override void Clear()
            {
                v3norms = null;
            }
            
            public override string ToString()
            {
                return "SharpNormal";
            }

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

            public override void GenerateIfNull() {
                
                if (cols == null) {
                    Color[] tmp = CurMeshDta._colors;

                    cols = new Vector4[vcnt];

                    for (int i = 0; i < vcnt; i++)
                        cols[i] = tmp[i].ToVector4();

                }
            }

            public override Vector4[] GetV4(VertexDataTarget trg)
            {
                return cols;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = cols[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "Color";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "R";
                    case 1: return "G";
                    case 2: return "B";
                    case 3: return "A";
                }
                return "Error";
            }

            public VertexColor(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                cols = null;
            }
        }

        public class VertexIndex : VertexDataType
        {
            public static VertexIndex inst;
            const int dataSize = 1;

            int[] inds;

            public override void GenerateIfNull()
            {
                if (inds == null) 
                    inds = CurMeshDta._vertexIndex;
                
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = inds[i];

                return chanelMedium;
            }



            public override string GetFieldName(int ind)
            {
                return "index";
            }

            public override string ToString()
            {
                return "vertexIndex";
            }

            public VertexIndex(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                inds = null;
            }
        }

        public class VertexShadow : VertexDataType
        {
            public static VertexShadow inst;
            const int dataSize = 4;

            Vector4[] shads;

            public override void GenerateIfNull() {
                if (shads == null)
                        shads = CurMeshDta._shadowBake;

            }

            public override Vector4[] GetV4(VertexDataTarget trg)
            {
                return shads;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = shads[i][no];

                return chanelMedium;
            }

            public override string GetFieldName(int ind)
            {
                return "light " + ind;
            }

            public override string ToString()
            {
                return "shadow";
            }

            public VertexShadow(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                shads = null;
            }
        }

        public class VertexAtlasedTextures : VertexDataType
        {
            public static VertexAtlasedTextures inst;
            const int dataSize = 4;

            Vector4[] textureNumbers;


            public override void GenerateIfNull() {
                if (textureNumbers == null)
                    textureNumbers = CurMeshDta._trisTextures;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = textureNumbers[i][no];

                return chanelMedium;
            }

            public override Vector4[] GetV4(VertexDataTarget trg)
            {
                return textureNumbers;
            }


            public override void Clear()
            {
                textureNumbers = null;
            }

            public override string GetFieldName(int ind)
            {
                return "tex " + ind;
            }

            public override string ToString ()
            {
                return "Atlas Texture";
            }

            public VertexAtlasedTextures(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }


        public class VertexNull : VertexDataType
        {
            public static VertexNull inst;
            const int dataSize = 1;
            public float[] zeroVal = null;

            public override void GenerateIfNull()
            {
                zeroVal = new float[vcnt];

            }

            public override float[] GetValue(int no)
            {

                return zeroVal;
            }



            public override string GetFieldName(int ind)
            {
                return "null";
            }

            public override string ToString()
            {
                return "null";
            }

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
            public static VertexEdge inst;
            const int dataSize = 4;

            Vector4[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeData;
                
            }

            public override Vector4[] GetV4(VertexDataTarget trg) {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "Edge";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                    case 3: return "Strength";
                }
                return "Error";
            }

            public VertexEdge(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class VertexEdgeByWeight : VertexDataType
        {
            public static VertexEdgeByWeight inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeDataByWeight;

            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "Edge * weight";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
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

        public class EdgeNormal_0 : VertexDataType
        {
            public static EdgeNormal_0 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeNormal_0_OrSharp;

            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_0";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public EdgeNormal_0(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class EdgeNormal_1 : VertexDataType
        {
            public static EdgeNormal_1 inst;
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
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_1";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public EdgeNormal_1(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class EdgeNormal_2 : VertexDataType
        {
            public static EdgeNormal_2 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = CurMeshDta._edgeNormal_2_OrSharp;

            }

            public override Vector3[] GetV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] GetValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_2";
            }

            public override string GetFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public EdgeNormal_2(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public static VertexDataType[] types = {

        new VertexPos(0), new VertexUV(1), new VertexUV(2), new VertexNormal(3),

        new VertexTangent(4), new VertexSharpNormal(5), new VertexColor(6), new VertexIndex(7),

        new VertexShadow(8), new VertexAtlasedTextures(9),  new VertexNull(10), new VertexEdge(11),

        new EdgeNormal_0(12), new EdgeNormal_1(13), new EdgeNormal_2(14), new VertexEdgeByWeight(15)

    };

        static string[] typesNames;
        
        public static string[] GetAllTypesNames()
        {
            if (typesNames == null)
            {
                typesNames = new string[types.Length];

                for (int i = 0; i < types.Length; i++)
                    typesNames[i] = types[i].ToString();
            }

            return typesNames;
        }

        public static List<VertexDataType> GetTypesBySize(int size)
        {

            List<VertexDataType> tmp = new List<VertexDataType>();

            for (int i = 0; i < types.Length; i++)
                if (types[i].chanelsNeed == size)
                    tmp.Add(types[i]);

            return tmp;
        }

    }
}