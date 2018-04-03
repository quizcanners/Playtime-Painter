using System;
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

    public abstract class VertexDataTarget
    {
        public byte chanelsHas;
        public int myIndex;

        public virtual void set(Vector3[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for " + this.GetType());
        }

        public virtual void set(Vector4[] dta)
        {
            Debug.Log(dta.GetType() + " input not implemented for array of " + this.GetType());
        }

        public abstract string name();

        public virtual void SetDefaults(VertexSolution to)
        {
            for (int i = 0; i < to.vals.Count; i++)
                to.vals[i].valueIndex = i;
        }

        public virtual string getFieldName(int ind)
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

        public VertexSolution getMySolution()
        {
            MeshPackagingProfile pf = MeshSolutions.curMeshDta.profile;
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

        public virtual string getFieldName(int ind)
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

        public virtual float[] getValue(int no)
        {
            return null;
        }



        public virtual Vector2[] getV2(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector2 array");
            return null;
        }

        public virtual Vector3[] getV3(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector3 array");
            return null;
        }

        public virtual Vector4[] getV4(VertexDataTarget trg)
        {
            Debug.Log("Mesh Data type " + this.GetType() + " does not provide Vector4 array");
            return null;
        }

    }


    [Serializable]
    public class VertexDataValue : abstract_STD {

        public int typeIndex;
        public int valueIndex;

        public VertexDataType vertDataType { get { return MeshSolutions.types[typeIndex]; } }

        public float[] getDataArray() {
            vertDataType.GenerateIfNull();
            return vertDataType.getValue(valueIndex);
        }

        public override stdEncoder Encode() {
            stdEncoder cody = new stdEncoder();
            cody.Add("t", typeIndex);
            cody.Add("v", valueIndex);
            return cody;
        }

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "t": typeIndex = data.ToInt(); break;
                case "v": valueIndex = data.ToInt(); break;
            }
        }

        public override string getDefaultTagName()
        {
            Debug.Log("Shouldn't be calling this");
            return "none";
        }

    

    }


    [Serializable]
    public class VertexSolution : abstract_STD 
    {
        public int sameSizeDataIndex;
        public int targetIndex;
        public bool enabled;
        public static bool showHint;


        public VertexDataType sameSizeValue { get { if (sameSizeDataIndex >= 0) return MeshSolutions.types[sameSizeDataIndex]; else return null; } }
        public VertexDataTarget target { get { return MeshSolutions.targets[targetIndex]; } set { targetIndex = value.myIndex; } }

        public List<VertexDataValue> vals;

        public override bool PEGI()
        {
            bool changed = false;

            (target.name() + ":").toggle(80, ref enabled);

            if (enabled)
            {

                List<VertexDataType> tps = MeshSolutions.getTypesBySize(vals.Count);
                string[] nms = new string[tps.Count + 1];

                for (int i = 0; i < tps.Count; i++)
                    nms[i] = tps[i].ToString();

                nms[tps.Count] = "Other";

                int selected = tps.Count;

                if (sameSizeValue != null)
                    for (int i = 0; i < tps.Count; i++)
                        if (tps[i] == sameSizeValue)
                        {
                            selected = i;
                            break;
                        }

                changed |= pegi.select(ref selected, nms).nl();

                if (selected >= tps.Count) sameSizeDataIndex = -1;
                else
                    sameSizeDataIndex = tps[selected].myIndex;

                string[] allDataNames = MeshSolutions.getAllTypesNames();

                if (sameSizeValue == null)
                {
                    for (int i = 0; i < vals.Count; i++) {
                        VertexDataValue v = vals[i];

                        changed |= target.getFieldName(i).select(40, ref v.typeIndex, allDataNames);

                        string[] typeFields = new string[v.vertDataType.chanelsNeed];

                        for (int j = 0; j < typeFields.Length; j++)
                            typeFields[j] = v.vertDataType.getFieldName(j);



                        changed |= pegi.select(ref v.valueIndex, typeFields).nl();

                        v.valueIndex = Mathf.Clamp(v.valueIndex, 0, typeFields.Length - 1);
                    }
                }
                "**************************************************".nl();
            }

            return changed;
        }

        public VertexSolution() {

        }

        public VertexSolution(VertexDataTarget ntrg) {
            target = ntrg;

            initVals();
            sameSizeDataIndex = -1;
         
            ntrg.SetDefaults(this);
        }

        void initVals() {
            vals = new List<VertexDataValue>();
            for (int i = 0; i < target.chanelsHas; i++)
                vals.Add(new VertexDataValue());
        }

        public void Pack()
        {
            switch (target.chanelsHas)
            {
                case 3: PackVector3(); break;
                case 4: PackVector4(); break;
                default: Debug.Log("No packaging function for Vector" + target.chanelsHas + " taget."); break;
            }
        }

        void PackVector3()
        {

            Vector3[] ar;

            if (sameSizeValue != null)
            {
                sameSizeValue.GenerateIfNull();

                ar = sameSizeValue.getV3(target);

            }
            else
            {

                ar = new Vector3[MeshSolutions.vcnt];

                for (int i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector3();

                for (int i = 0; i < 3; i++)
                {
                    VertexDataValue v = vals[i];
                    float[] tmp = v.getDataArray();

                    if (tmp != null)
                        for (int j = 0; j < MeshSolutions.vcnt; j++)
                            ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                target.set(ar);

        }

        void PackVector4()
        {

            Vector4[] ar;

            if (sameSizeValue != null)
            {
                sameSizeValue.GenerateIfNull();

                ar = sameSizeValue.getV4(target);

            }
            else
            {

                ar = new Vector4[MeshSolutions.vcnt];

                for (int i = 0; i < MeshSolutions.vcnt; i++)
                    ar[i] = new Vector4();

                for (int i = 0; i < 4; i++)
                {
                    VertexDataValue v = vals[i];
                    float[] tmp = v.getDataArray();

                    if (tmp != null)
                        for (int j = 0; j < MeshSolutions.vcnt; j++)
                            ar[j][i] = tmp[j];

                }
            }

            if (ar != null)
                target.set(ar);

        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

           
            cody.Add("en", enabled);
            cody.Add("t", targetIndex);

            if (enabled) {
                if (sameSizeDataIndex == -1)
                    cody.AddIfNotEmpty("vals", vals);
                else
                    cody.Add("sameSize", sameSizeDataIndex);

               
            }
            return cody;
        }

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "en": enabled = data.ToBool();  break;
                case "t": targetIndex = data.ToInt(); if (!enabled) initVals(); break;
                case "vals": vals = data.ToListOf_STD<VertexDataValue>(); sameSizeDataIndex = -1; break;
                case "sameSize": sameSizeDataIndex = data.ToInt(); initVals(); break;
              
            }
        }

        public override string getDefaultTagName()
        {
            return target.name();
        }
    }

    
    [Serializable]
    public class MeshPackagingProfile: abstract_STD {
        public List<VertexSolution> sln;

        public string name = "";

        public const string folderName = "Mesh Profiles";

        public override bool PEGI() {
            
            "Profile Name: ".edit(80, ref name);

#if UNITY_EDITOR

            string path = PainterConfig.inst.meshesFolderName + "/" + folderName;
            if (icon.save.Click("Save To:" + path, 25).nl()) {
                this.SaveToAssets(path, name).RefreshAssetDatabase();
                (name + " Saved to " + path).showNotification();
                AssetDatabase.Refresh();
            }


            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType).nl()) {
                var msol = (MeshPackagingProfile)(new MeshPackagingProfile().Reboot(ResourceLoader.LoadStory(myType)));
                
                PainterConfig.inst.meshPackagingSolutions.Add(msol);
                PlaytimePainter.inspectedPainter.selectedMeshProfile = PainterConfig.inst.meshPackagingSolutions.Count - 1;
            }
#endif


            bool changed = false;
            for (int i = 0; i < sln.Count; i++)
                changed |= sln[i].PEGI().nl();

            return changed;
        }

        public override string ToString() {
            return name;
        }

        public void StartPacking(MeshConstructor sm) {

            if (!sm.valid){//(sm.verts == null) || (sm.tris == null) || (sm.verts.Length < 3) || (sm.tris.TotalCount() < 3)) {
                Debug.Log("Got no stuff to regenerate mesh. ");
                return;
            }

            sm.mesh.Clear();

            MeshSolutions.curMeshDta = sm;

            foreach (VertexSolution vs in sln)
                if (vs.enabled) vs.Pack();

            foreach (VertexDataType vt in MeshSolutions.types)
                vt.Clear();
        }

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            cody.AddText("n", name);
            cody.AddIfNotEmpty("sln", sln);


            return cody;
        }

        public override void Decode(string tag, string data)  {
            switch (tag) {
                case "n": name = data; break;
                case "sln": sln = data.ToListOf_STD<VertexSolution>(); break;
            }
        }

        public const string stdTag_vertSol = "vertSol";

        public override string getDefaultTagName()
        {
            return stdTag_vertSol;
        }

        public MeshPackagingProfile() {
            VertexDataTarget[] trgs = MeshSolutions.targets;
            sln = new List<VertexSolution>(); //[trgs.Length];
            name = "unnamedd";
            for (int i = 0; i < trgs.Length; i++)
                sln.Add(new VertexSolution(trgs[i]));
        }

    }

    public static class MeshSolutions
    {

        public const string shaderPreferedPackagingSolution = "Solution";

        public static int getMeshProfileByTag(this Material mat)
        {

            var name = mat.GetTag(shaderPreferedPackagingSolution, false, "Standard");

            var prf = PainterConfig.inst.meshPackagingSolutions;

            for (int i = 0; i < prf.Count; i++)// (var s in PainterConfig.inst.meshProfileSolutions)
                if (String.Compare(prf[i].name, name) == 0) return i;

            return 0;//PainterConfig.inst.meshProfileSolutions[0];
        }

        private static MeshConstructor _curMeshDra;

        public static MeshConstructor curMeshDta
        {

            get { return _curMeshDra; }

            set { _curMeshDra = value; vcnt = _curMeshDra.verts.Length; chanelMedium = new float[vcnt]; }

        }

        public static int vcnt = 0;
        public static float[] chanelMedium = null;
        // ********************************************************* DATA Targets

        public class vertexPosTrg : VertexDataTarget
        {
            const int dataSize = 3;

            public override void set(Vector3[] dta)
            {
                curMeshDta.mesh.vertices = dta;
                curMeshDta.mesh.subMeshCount = curMeshDta.tris.Length;
                for (int sm = 0; sm < curMeshDta.tris.Length; sm++)
                    curMeshDta.mesh.SetTriangles(curMeshDta.tris[sm], sm, true, 0);
            }

            public override string name()
            {
                return "position";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = vertexPos.inst.myIndex;
            }

            public vertexPosTrg(int index) : base(dataSize, index)
            {

            }

        }

        public class vertexUVTrg : VertexDataTarget
        {
            const int dataSize = 4;

            int MyUVChanel() { return (myIndex - 1); }

            public override void set(Vector4[] dta)
            {

                VertexSolution vs = getMySolution();

                // Debug.Log("got vals: " + vs.vals.Length + " name " + vs.ToString());

                if ((vs.sameSizeValue != null) || (vs.vals[2].vertDataType != vertexNull.inst) || (vs.vals[3].vertDataType != vertexNull.inst))
                    curMeshDta.mesh.SetUVs(MyUVChanel(), new List<Vector4>(dta));
                else
                {
                    // Debug.Log("Packing UV as Vector 2 for "+MyUVChanel());
                    Vector2[] v2s = new Vector2[dta.Length];

                    for (int i = 0; i < dta.Length; i++)
                    {
                        Vector4 v4 = dta[i];
                        v2s[i] = new Vector2(v4.x, v4.y);
                    }

                    switch (MyUVChanel())
                    {
                        case 0:
                            curMeshDta.mesh.uv = v2s; break;
                        case 1:
                            curMeshDta.mesh.uv2 = v2s; break;
                        case 2:
                            curMeshDta.mesh.uv3 = v2s; break;
                        case 3:
                            curMeshDta.mesh.uv3 = v2s; break;
                    }
                }
            }

            public override string name()
            {
                return "UV" + MyUVChanel().ToString();
            }


            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);

                if (myIndex != 1) return;

                to.enabled = true;

                int ind = vertexUV.inst[0].myIndex;

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

            public vertexUVTrg(int index) : base(dataSize, index)
            {

            }

        }

        public class vertexTangentTrg : VertexDataTarget
        {
            const int dataSize = 4;

            public override void set(Vector4[] dta)
            {
                curMeshDta.mesh.tangents = dta;
            }

            public override string name()
            {
                return "tangent";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = vertexTangent.inst.myIndex;
            }

            public vertexTangentTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class vertexNormalTrg : VertexDataTarget
        {
            const int dataSize = 3;

            public override void set(Vector3[] dta)
            {
                curMeshDta.mesh.normals = dta;
            }

            public override string name()
            {
                return "normal";
            }

            public override void SetDefaults(VertexSolution to)
            {
                base.SetDefaults(to);
                to.enabled = true;
                to.sameSizeDataIndex = vertexNormal.inst.myIndex;
            }

            public vertexNormalTrg(int index) : base(dataSize, index)
            {

            }
        }

        public class vertexColorTrg : VertexDataTarget
        {
            const int dataSize = 4;

            public override void set(Vector4[] dta)
            {
                Color[] cols = new Color[dta.Length];
                for (int i = 0; i < dta.Length; i++)
                    cols[i] = dta[i];

                curMeshDta.mesh.colors = cols;
            }

            public override string name()
            {
                return "color";
            }

            public override string getFieldName(int ind)
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
                to.sameSizeDataIndex = vertexColor.inst.myIndex;
                for (int i = 0; i < 4; i++)
                    to.vals[i].typeIndex = vertexColor.inst.myIndex;
            }

            public vertexColorTrg(int index) : base(dataSize, index)
            {

            }
        }


        public static VertexDataTarget[] targets = {
        new vertexPosTrg(0) , new vertexUVTrg(1) , new vertexUVTrg(2) , new vertexUVTrg(3),
        new vertexUVTrg(4),   new vertexNormalTrg(5), new vertexTangentTrg(6),  new vertexColorTrg(7)

    };


        // ******************************************************** DATA Types

        public class vertexPos : VertexDataType
        {
            public static vertexPos inst;
            const int dataSize = 3;

            Vector3[] vertices;

            public override void GenerateIfNull()
            {
                if (vertices == null)
                    vertices = curMeshDta.verts;
 
            }

            public override float[] getValue(int no)
            {
                Vector3[] vrts = vertices;
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = vrts[i][no];

                return chanelMedium;
            }

            public override Vector3[] getV3(VertexDataTarget trg)
            {
                return vertices;
            }

            public override string ToString()
            {
                return "position";
            }

            public vertexPos(int index) : base(dataSize, index)
            {
                inst = this;
            }

            public override void Clear()
            {
                vertices = null;
            }

        }

        public class vertexUV : VertexDataType
        {
            static int uvEnum = 0;
            int myUVindex;
            public static vertexUV[] inst = new vertexUV[2];

            const int dataSize = 2;

            Vector2[] v2s;

            public override void GenerateIfNull() {
                if (v2s == null)
                    v2s =  (myUVindex == 0) ? curMeshDta._uv : curMeshDta._uv1;
            }

            public override Vector2[] getV2(VertexDataTarget trg) {
                return v2s;
            }

            public override float[] getValue(int no)  {
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

            public vertexUV(int index) : base(dataSize, index)
            {

                myUVindex = uvEnum;
                inst[myUVindex] = this;
                uvEnum++;
            }

        }

        public class vertexTangent : VertexDataType
        {
            public static vertexTangent inst;
            const int dataSize = 4;

            Vector4[] v4s;

            public override void GenerateIfNull() {
                if (v4s == null)
                    v4s = curMeshDta._tangents;
            }

            public override Vector4[] getV4(VertexDataTarget trg)
            {
                if (trg.GetType() == typeof(vertexTangentTrg))
                {
                    curMeshDta.mesh.RecalculateTangents();
                    return null;
                }

                return v4s;

                //Debug.Log("Manual tangent recalculation not implemented yet.");
               // return null;
            }

            public override float[] getValue(int no)
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

            public vertexTangent(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class vertexNormal : VertexDataType
        {
            public static vertexNormal inst;
            const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull() {
                if (v3norms == null)
                    v3norms = curMeshDta.normals;
            }

           

            public override float[] getValue(int no)
            {
               

                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v3norms[i][no];

                return chanelMedium;
            }

            public override Vector3[] getV3(VertexDataTarget trg)
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

            public vertexNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class vertexSharpNormal : VertexDataType
        {
            public static vertexSharpNormal inst;
            const int dataSize = 3;

            Vector3[] v3norms;

            public override void GenerateIfNull()
            {
                if (v3norms == null)
                    v3norms = curMeshDta.sharpNormals;

            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = v3norms[i][no];

                return chanelMedium;
            }

            public override Vector3[] getV3(VertexDataTarget trg)
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

            public vertexSharpNormal(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }

        public class vertexColor : VertexDataType
        {
            public static vertexColor inst;
            const int dataSize = 4;

            Vector4[] cols;

            public override void GenerateIfNull() {
                
                if (cols == null) {
                    Color[] tmp = curMeshDta._colors;

                    cols = new Vector4[vcnt];

                    for (int i = 0; i < vcnt; i++)
                        cols[i] = tmp[i].ToVector4();

                }
            }

            public override Vector4[] getV4(VertexDataTarget trg)
            {
                return cols;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = cols[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "Color";
            }

            public override string getFieldName(int ind)
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

            public vertexColor(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                cols = null;
            }
        }

        public class vertexIndex : VertexDataType
        {
            public static vertexIndex inst;
            const int dataSize = 1;

            int[] inds;

            public override void GenerateIfNull()
            {
                if (inds == null) 
                    inds = curMeshDta.originalIndex;
                
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = inds[i];

                return chanelMedium;
            }



            public override string getFieldName(int ind)
            {
                return "index";
            }

            public override string ToString()
            {
                return "vertexIndex";
            }

            public vertexIndex(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                inds = null;
            }
        }

        public class vertexShadow : VertexDataType
        {
            public static vertexShadow inst;
            const int dataSize = 4;

            Vector4[] shads;

            public override void GenerateIfNull() {
                if (shads == null)
                        shads = curMeshDta._shadowBake;

            }

            public override Vector4[] getV4(VertexDataTarget trg)
            {
                return shads;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = shads[i][no];

                return chanelMedium;
            }

            public override string getFieldName(int ind)
            {
                return "light " + ind;
            }

            public override string ToString()
            {
                return "shadow";
            }

            public vertexShadow(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                shads = null;
            }
        }

        public class vertexAtlasedTextures : VertexDataType
        {
            public static vertexAtlasedTextures inst;
            const int dataSize = 4;

            Vector4[] textureNumbers;


            public override void GenerateIfNull() {
                if (textureNumbers == null)
                    textureNumbers = curMeshDta._trisTextures;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = textureNumbers[i][no];

                return chanelMedium;
            }

            public override Vector4[] getV4(VertexDataTarget trg)
            {
                return textureNumbers;
            }


            public override void Clear()
            {
                textureNumbers = null;
            }

            public override string getFieldName(int ind)
            {
                return "tex " + ind;
            }

            public override string ToString ()
            {
                return "Atlas Texture";
            }

            public vertexAtlasedTextures(int index) : base(dataSize, index)
            {
                inst = this;
            }

        }


        public class vertexNull : VertexDataType
        {
            public static vertexNull inst;
            const int dataSize = 1;
            public float[] zeroVal = null;

            public override void GenerateIfNull()
            {
                zeroVal = new float[vcnt];

            }

            public override float[] getValue(int no)
            {

                return zeroVal;
            }



            public override string getFieldName(int ind)
            {
                return "null";
            }

            public override string ToString()
            {
                return "null";
            }

            public vertexNull(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                zeroVal = null;
            }
        }

        public class vertexEdge : VertexDataType
        {
            public static vertexEdge inst;
            const int dataSize = 4;

            Vector4[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = curMeshDta._edgeData;
                
            }

            public override Vector4[] getV4(VertexDataTarget trg) {
                return edges;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "Edge";
            }

            public override string getFieldName(int ind)
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

            public vertexEdge(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class edgeNormal_0 : VertexDataType
        {
            public static edgeNormal_0 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = curMeshDta._edgeNormal_0_OrSharp;

            }

            public override Vector3[] getV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_0";
            }

            public override string getFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public edgeNormal_0(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class edgeNormal_1 : VertexDataType
        {
            public static edgeNormal_1 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = curMeshDta._edgeNormal_1_OrSharp;

            }

            public override Vector3[] getV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_1";
            }

            public override string getFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public edgeNormal_1(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public class edgeNormal_2 : VertexDataType
        {
            public static edgeNormal_2 inst;
            const int dataSize = 3;

            Vector3[] edges;

            public override void GenerateIfNull()
            {

                if (edges == null)
                    edges = curMeshDta._edgeNormal_2_OrSharp;

            }

            public override Vector3[] getV3(VertexDataTarget trg)
            {
                return edges;
            }

            public override float[] getValue(int no)
            {
                for (int i = 0; i < vcnt; i++)
                    chanelMedium[i] = edges[i][no];

                return chanelMedium;
            }

            public override string ToString()
            {
                return "LineNormal_2";
            }

            public override string getFieldName(int ind)
            {
                switch (ind)
                {
                    case 0: return "x";
                    case 1: return "y";
                    case 2: return "z";
                }
                return "Error";
            }

            public edgeNormal_2(int index) : base(dataSize, index)
            {
                inst = this;
            }
            public override void Clear()
            {
                edges = null;
            }
        }

        public static VertexDataType[] types = {

        new vertexPos(0), new vertexUV(1), new vertexUV(2), new vertexNormal(3),

        new vertexTangent(4), new vertexSharpNormal(5), new vertexColor(6), new vertexIndex(7),

        new vertexShadow(8), new vertexAtlasedTextures(9),  new vertexNull(10), new vertexEdge(11),

        new edgeNormal_0(12), new edgeNormal_1(13), new edgeNormal_2(14)

    };

        static string[] typesNames;
        
        public static string[] getAllTypesNames()
        {
            if (typesNames == null)
            {
                typesNames = new string[types.Length];

                for (int i = 0; i < types.Length; i++)
                    typesNames[i] = types[i].ToString();
            }

            return typesNames;
        }

        public static List<VertexDataType> getTypesBySize(int size)
        {

            List<VertexDataType> tmp = new List<VertexDataType>();

            for (int i = 0; i < types.Length; i++)
                if (types[i].chanelsNeed == size)
                    tmp.Add(types[i]);

            return tmp;
        }

    }
}