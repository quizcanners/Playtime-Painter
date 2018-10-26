using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;


namespace SharedTools_Stuff {

    public interface ICountlessIndex
    {
        int CountlessIndex { get; set; }
    }
    
    public abstract class CountlessBase : IPEGI
    {

        protected static VariableBranch[] branchPool = new VariableBranch[32];
        protected static VariableBranch[] fruitPool = new VariableBranch[32];
        protected static int brPoolMax = 0;
        protected static int frPoolMax = 0;
        protected static ArrayManager<VariableBranch> array = new ArrayManager<VariableBranch>();
        protected static int branchSize = 8;
        protected static void DiscardFruit(VariableBranch b, int no)
        {
            if ((frPoolMax + 1) >= fruitPool.Length)
                array.Expand(ref fruitPool, 32);

            fruitPool[frPoolMax] = b.br[no];
            VariableBranch vb = fruitPool[frPoolMax];
            vb.value = 0;
            b.br[no] = null;
            b.value--;
            frPoolMax++;
        }
        protected static void DiscardBranch(VariableBranch b, int no)
        {
            if ((brPoolMax + 1) >= branchPool.Length)
            {
                array.Expand(ref branchPool, 32);
            }
            //Debug.Log("Deleting branch ");
            branchPool[brPoolMax] = b.br[no];
            VariableBranch vb = branchPool[brPoolMax];
            if (vb.value != 0)
                Debug.Log("Value is " + vb.value + " on delition ");
            //vb.value = 0;
            b.value--;
            b.br[no] = null;
            brPoolMax++;
        }
        protected void TryReduceDepth()
        {
            while ((br.value < 2) && (br.br[0] != null) && (depth > 0))
            {
                // if (br.value < 1) Debug.Log("Reducing depth on branch with " + br.value);
                branchPool[brPoolMax] = br;
                brPoolMax++;
                VariableBranch tmp = br.br[0];
                br.br[0] = null;
                br.value = 0;
                br = tmp;
                depth--;
                Max /= branchSize;

                // Debug.Log("Reducing depth to " + depth + " new Range: " + Max);
            }
        }
        protected static void DiscardCascade(VariableBranch b, int depth)
        {
            if ((brPoolMax + 1) >= branchPool.Length)
            {
                array.Expand(ref branchPool, 32);
            }

            if (depth > 0)
            {
                for (int i = 0; i < branchSize; i++)
                {
                    if (b.br[i] != null)
                    {
                        DiscardCascade(b.br[i], depth - 1);
                        DiscardBranch(b, i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                        DiscardFruit(b, i);
            }

        }
        protected static VariableBranch GetNewBranch()
        {
            if (brPoolMax == 0)
            {
                VariableBranch vb = new VariableBranch() {
                    br = new VariableBranch[branchSize]
            };
                //Debug.Log("Creating new branch ");
                return vb;
            }
            brPoolMax--;
            //Debug.Log("Returning existing branch");
            return branchPool[brPoolMax];
        }
        protected static VariableBranch GetNewFruit()
        {

            if (frPoolMax == 0)
            {
                VariableBranch vb = new VariableBranch();
                //   Debug.Log("Creating new fruit ");
                return vb;
            }
            frPoolMax--;
            // Debug.Log("Returning existing fruit");
            return fruitPool[frPoolMax];
        }

        public virtual void Clear()
        {
            DiscardCascade(br, depth);
        }

        protected int firstFree;
        protected int depth;
        protected int Max;
        protected VariableBranch[] path;
        protected int[] pathInd;
        protected VariableBranch br;
        protected int lastFreeIndex;

        #region Inspector
#if PEGI
        public virtual bool Inspect()
        {
            ("Depth: " + depth).nl();
            ("First free: " + firstFree).nl();

            return false;
        }
#endif
        #endregion

        public CountlessBase()
        {
            Max = branchSize;
            depth = 0;
            br = GetNewBranch();
        }

        public delegate void VariableTreeFunk(ref int dst, int ind, int val);
    }


    public abstract class STDCountlessBase : CountlessBase, ISTD, ICanBeDefault_STD
    {
        public virtual bool IsDefault { get {
                var def = (br == null || br.value == 0);
              //  if (def) Debug.Log("Found default Countless");
                return def;

            } }
        public virtual StdEncoder Encode() {
            return null; }

        public virtual ISTD Decode(string data)
        {
            Clear();
            return data.DecodeTagsFor(this);
        }

        public virtual bool Decode(string subtag, string data) { return true; }


    }

    public class CountlessInt : STDCountlessBase {

        List<int> inds;

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "inds": data.DecodeInto(out inds); break;
                case "vals":
                    List<int> vals; data.DecodeInto(out vals);
                    for (int i = 0; i < vals.Count; i++)
                        Set(inds[i], vals[i]);
                    inds = null;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

        }

        public override StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();

            List<int> vals;

            GetItAll(out inds, out vals);

            cody.Add("inds", inds);
            cody.Add("vals", vals);
            cody.Add("last", lastFreeIndex);


            inds = null;

            return cody;
        }

        public void GetItAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetItAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }
        void GetItAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        public int this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        int Get(int ind)
        {
            if (ind >= Max || ind<0)
                return 0;

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return 0;

                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return 0;

            return vb.br[ind].value;
        }

        void Set(int ind, int val)
        {

            //Debug.Log("Setting "+ind+" to "+val);

            if (ind >= Max)
            {
                if (val == 0)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (val != 0)
            {
                while (d > 0)
                {

                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;
                }


                vb.br[ind].value = val;
            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public void Add(int ind, int val)
        {

            if (ind >= Max)
            {
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {
                vb.br[ind] = GetNewFruit();
                vb.value += 1;
            }

            vb.br[ind].value += val;

        }

        public IEnumerator<int> GetEnumerator() {
            List<int> indx;
            List<int> vals;
             GetItAll(out indx, out vals);
            for (int i = 0; i < vals.Count; i++)  {
                currentEnumerationIndex = indx[i];
                yield return vals[i];
            }
        }

        public int currentEnumerationIndex;
    }

    public class CountlessBool : STDCountlessBase
    {

 

        #region Encode & Decode
        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "inds":
                    List<int> inds; data.DecodeInto(out inds);
                    foreach (int i in inds)
                        Set(i, true);

                    inds = null;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

        }

        public override StdEncoder Encode() => new StdEncoder().Add("inds", GetItAll()).Add("last", lastFreeIndex);
        #endregion

        public List<int> GetItAll()
        {
            List<int> inds = new List<int>();
            GetItAllCascadeBool(ref inds, br, depth, 0, Max);
            return inds;
        }

        void GetItAllCascadeBool(ref List<int> inds, VariableBranch b, int dp, int start, int range)
        {

            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeBool(ref inds, b.br[i], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        int value = b.br[i].value;

                        for (int j = 0; j < 32; j++)
                            if ((value & (int)(0x00000001 << j)) != 0)
                                //{
                                inds.Add((start + i) * 32 + j);
                        //  vals.Add(1);
                        // }
                    }
            }
        }

        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        bool Get(int ind)
        {

            //#if UNITY_EDITOR
            if (ind < 0)
                return false;
            //Debug.LogError("Sending " + ind + " as index to Variable Tree, that is a nono");
            //#endif

            int bitNo = ind % 32;
            ind /= 32;
            if (ind >= Max)
                return false;

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return false;
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return false;

            VariableBranch fvb = vb.br[ind];

            return ((fvb.value & (int)(0x00000001 << bitNo)) != 0);
            //return Get(ind) > 0;
        }

        void Set(int ind, bool val)
        {
            int bitNo = ind % 32;
            ind /= 32;

            if (ind >= Max)
            {
                if (!val)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {
                vb.br[ind] = GetNewFruit();
                vb.value += 1;
            }

            VariableBranch fvb = vb.br[ind];
            if (val)
                fvb.value |= (int)(0x00000001 << bitNo);
            else
                fvb.value &= (int)(~(0x00000001 << bitNo));
            //vb.br[ind].value = val;

            if (fvb.value == 0)
                DiscardFruit(vb, ind);

            while (d < depth)
            {
                if (vb.value > 0)
                    return;
                vb = path[d];
                DiscardBranch(vb, pathInd[d]);
                d++;
            }

            TryReduceDepth();
        }

        public bool Toggle(int ind)
        {



            int bitNo = ind % 32;
            ind /= 32;

            if (ind >= Max)
            {
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;


            while (d > 0)
            {

                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
            {

                vb.br[ind] = GetNewFruit();
                vb.value += 1;
            }

            VariableBranch fvb = vb.br[ind];
            // bitFunk(ref fvb.value, bitNo, val);
            fvb.value ^= (int)(0x00000001 << bitNo);
            //vb.br[ind].value = val;

            bool rslt = (((fvb.value & (int)(0x00000001 << bitNo)) != 0) ? true : false);

            if (fvb.value == 0)
                DiscardFruit(vb, ind);



            while (d < depth)
            {
                if (vb.value > 0)
                    return rslt;
                vb = path[d];
                DiscardBranch(vb, pathInd[d]);
                d++;
            }

            TryReduceDepth();

            return rslt;
        }

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var i in GetItAll())
                yield return i;
        }
    
    

}

///  Generic Trees
public class Countless<T> : CountlessBase //, IEnumerable
    {
        
        T[] objs = new T[0];
        int firstFreeObj = 0;

        public void Expand(ref T[] args, int add)  {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
        }

        public T this[int index]
        {
            get { return Get(index); }
            set {
#if PEGI
                var igi = value as IGotIndex;
                if (igi != null && igi.IndexForPEGI != index)
                {
                    Debug.Log("setting "+value.ToString() + " with ind " + igi.IndexForPEGI + " at "+index);
                 //   igi.index = index;
                }
#endif
                Set(index, value); }
        }

#if PEGI
        public T this[IGotIndex i]
        {
            get { return Get(i.IndexForPEGI); }
            set { Set(i.IndexForPEGI, value); }
        }
#endif
        void Set(int ind, T obj)
        {
            
            if (ind >= Max)
            {
                if (obj.IsDefaultOrNull())
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (!obj.IsDefaultOrNull())
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (!objs[firstFreeObj].IsDefaultOrNull())) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(T);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        T Get(int ind)
        {
            if (ind >= Max || ind<0)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }

        public List<T> GetAllObjsNoOrder()
        {
            List<T> tmp = new List<T>();
            for (int i = 0; i < objs.Length; i++)
                if (!objs[i].IsDefaultOrNull())
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            List<T> objects = new List<T>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }
        
        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
            firstFreeObj = 0;
        }

        public virtual bool NotEmpty => objs.Length > 0;

        public IEnumerator<T> GetEnumerator() {
            List<int> indx;
            List<T> all = GetAllObjs(out indx);
            for (int i = 0; i < all.Count; i++) {
                var e = all[i];
                if (!e.IsDefaultOrNull()) {
                    currentEnumerationIndex = indx[i];
                    yield return e;
                }
            }
        }

        public int currentEnumerationIndex;

#if PEGI

          int edited = -1;

        public override bool Inspect()
        {
            bool changed = false;

            if (edited == -1)
            {

                List<int> indxs;
                var allElements = GetAllObjs(out indxs);

                for (int i = 0; i < allElements.Count; i++)
                {
                    var ind = indxs[i];
                    var el = allElements[i];

                    if (icon.Delete.Click())
                        this[ind] = default(T);
                    else
                        el.Name_ClickInspect_PEGI<T>(null, ind, ref edited);
                }

            }
            else
            {
                if (icon.List.Click("Back to elements window"))
                    edited = -1;
                else
                    this[edited].Try_Nested_Inspect();
            }
            return changed;
        }
#endif

    }

    public class CountlessSTD<T> : STDCountlessBase where T : ISTD , new() {
        
        protected T[] objs = new T[0];
        int firstFreeObj = 0;

        public bool allowAdd;
        public bool allowDelete;

        #region Encode & Decode
        static List<int> tmpDecodeInds;
        public override bool Decode(string tag, string data) {

            switch (tag) {

                case "inds": data.DecodeInto(out tmpDecodeInds); break;
                case "vals": List<T> tmps; data.DecodeInto_List(out tmps);
                    for (int i = 0; i < tmps.Count; i++) {
                        var tmp = tmps[i];
                        if (!tmp.Equals(default(T))) 
                            this[tmpDecodeInds[i]] = tmp;
                    }

                    tmpDecodeInds = null;
                    break;
                case "brws": edited = data.ToInt(); break;
                case "last": lastFreeIndex = data.ToInt(); break;
                case "add": allowAdd = data.ToBool(); break;
                case "del": allowDelete = data.ToBool(); break;
                default: 
                    // Legacy method:
            this[tag.ToInt()] = data.DecodeInto<T>(); break;
        }
            return true;
        }

        public override StdEncoder Encode()
        {
          
            List<int> inds;
            List<T> vals = GetAllObjs(out inds);

            var cody = new StdEncoder()
                .Add("inds", inds)
                .Add("vals", vals)
                .Add_IfNotNegative("brws", edited)
                .Add("last", lastFreeIndex)
                 .Add_Bool("add", allowAdd)
                 .Add_Bool("del", allowDelete);

            return cody;
        }
        #endregion

        public void Expand(ref T[] args, int add) {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;

        }

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        protected void Set(int ind, T obj)
        {
            if (ind >= Max)
            {
                if (obj == null)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (obj != null)
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null) {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (objs[firstFreeObj] != null)) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(T);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public List<T> GetAllObjsNoOrder()
        {
            List<T> tmp = new List<T>();
            for (int i = 0; i < objs.Length; i++)
                if (objs[i] != null)
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            List<T> objects = new List<T>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        protected virtual T Get(int ind)
        {
            if (ind >= Max || ind<0)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }

        public virtual T GetIfExists(int ind) => Get(ind);
        
        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
            firstFreeObj = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<int> indx;
            var all = GetAllObjs(out indx);
            for (int i = 0; i < all.Count; i++) {

                var e = all[i];

                if (!e.IsDefaultOrNull())
                {
                    currentEnumerationIndex = indx[i];
                    yield return e;
                }
            }
        }

        public int currentEnumerationIndex;

        int edited = -1;

#if PEGI
        public override bool Inspect()
        {
            bool changed = false;

            if (edited == -1)  {

                List<int> indxs;
                var allElements = GetAllObjs(out indxs);

                if (allowAdd && icon.Add.Click("Add "+typeof(T).ToPEGIstring())) {
                    while (!this.GetIfExists(lastFreeIndex).IsDefaultOrNull())
                        lastFreeIndex++;

                    this[lastFreeIndex] = new T();
                }

                pegi.nl();

                for (int i = 0; i < allElements.Count; i++)  {
                    var ind = indxs[i];
                    var el = allElements[i];

                    if (allowDelete && icon.Delete.Click("Clear element without shifting the rest"))
                        this[ind] = default(T);
                    else
                    {
                        "{0}".F(ind).write(20);
                        el.Name_ClickInspect_PEGI<T>(null, ind, ref edited);
                    }

                    pegi.nl();

                }

            } else
            {
                if (icon.List.Click("Back to elements window"))
                    edited = -1;
                else
                    this[edited].Try_Nested_Inspect();
            }
            return changed;
        }
#endif
    }

    // Unnulable classes will create new instances
    public class UnnullableSTD<T> : CountlessSTD<T> where T : ISTD, new()  {

        public static int IndexOfCurrentlyCreatedUnnulable;

        T Create(int ind) {
            IndexOfCurrentlyCreatedUnnulable = ind;
            T tmp = new T();
            Set(ind, tmp);
            return tmp;
        }

        public int AddNew()
        {
            IndexOfCurrentlyCreatedUnnulable = -1;

            while (IndexOfCurrentlyCreatedUnnulable == -1)
            {
                Get(firstFree);
                firstFree++;
            }

            return IndexOfCurrentlyCreatedUnnulable;
        }

        protected override T Get(int ind)  {
            int originalIndex = ind;

            if (ind >= Max)
                return Create(originalIndex);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return Create(originalIndex);

            return objs[vb.br[ind].value];
        }

        public override T GetIfExists(int ind)
        {
            // int originalIndex = ind;

            if (ind >= Max)
                return default(T);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return default(T);

            return objs[vb.br[ind].value];
        }
        
    }

    // List trees
    public class UnnullableLists<T> : STDCountlessBase, IEnumerable {

        List<T>[] objs = new List<T>[0];
        int firstFreeObj = 0;

        public void Expand(ref List<T>[] args, int add) // no instantiating
        {
            List<T>[] temp;
            if (args != null)
            {
                temp = new List<T>[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new List<T>[add];
            args = temp;
            // for (int i = args.Length - add; i < args.Length; i++)
            //   args[i] = new T();
        }

        public List<T> this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        void Set(int ind, List<T> obj)
        {
            if (ind >= Max)
            {
                if (obj == null)
                    return;
                while (ind >= Max)
                {
                    depth++;
                    Max *= branchSize;
                    VariableBranch newbr = GetNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            if (obj != null)
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    int cnt = objs.Length;
                    while ((firstFreeObj < cnt) && (objs[firstFreeObj] != null)) firstFreeObj++;
                    if (firstFreeObj >= cnt)
                        Expand(ref objs, branchSize);

                    objs[firstFreeObj] = obj;
                    vb.br[ind].value = firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                int ar = vb.br[ind].value;

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

                objs[ar] = default(List<T>);
                firstFreeObj = Mathf.Min(firstFreeObj, ar);

                DiscardFruit(vb, ind);

                while (d < depth)
                {
                    if (vb.value > 0)
                        return;
                    vb = path[d];
                    DiscardBranch(vb, pathInd[d]);
                    d++;
                }

                TryReduceDepth();

            }
        }

        public List<List<T>> GetAllObjsNoOrder()
        {
            List<List<T>> tmp = new List<List<T>>();
            for (int i = 0; i < objs.Length; i++)
                if (objs[i] != null)
                    tmp.Add(objs[i]);

            return tmp;
        }

        public List<List<T>> GetAllObjs(out List<int> inds)
        {
            List<List<T>> objects = new List<List<T>>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        List<T> Create(int ind)
        {
            if (ind < 0)
            {
                Debug.Log("!Wrong index");
                return null;
            }
            var tmp = new List<T>();
            this[ind] = tmp;
            return tmp;
        }

        List<T> Get(int ind)
        {
            if (ind < 0)
                return null;

            int originalIndex = ind;

            if (ind >= Max)
                return Create(originalIndex);//default(List<T>);

            int d = depth;
            VariableBranch vb = br;
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return Create(originalIndex);

            return objs[vb.br[ind].value];
        }

        public override void Clear()
        {
            base.Clear();
            objs = new List<T>[0];
            firstFreeObj = 0;
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var e in objs)
                if (!e.IsDefaultOrNull())
                    yield return e;
        }
    }

    public class UnnulSTDLists<T> : UnnullableLists<T> where T : ISTD, IPEGI
        , new()
    {

        public override bool Decode(string tag, string data)
        {
            List<T> el; 
            int index = tag.ToInt();
            this[index] = data.DecodeInto_List(out el);
            return true;
        }

        public override StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();

            List<int> inds;
            List<List<T>> vals = GetAllObjs(out inds);

            for (int i = 0; i < inds.Count; i++)
                cody.Add_IfNotEmpty(inds[i].ToString(), vals[i]);

            return cody;
        }

     //   public const string storyTag = "TreeObj";
      //  public override string getDefaultTagName() { return storyTag; }
    }
    
    public class VariableBranch
    {
        public int value;
        public VariableBranch[] br;
    }

    public static class ExtensionsForGenericCountless
    {

        #region Inspector
#if PEGI
        public static bool Inspect<G, T>(this G Cstd, ref int inspected) where G : CountlessSTD<T> where T: ISTD, IPEGI, new() {

            bool changed = false;
            
            if (inspected > -1) {
                var e = Cstd[inspected];
                if (e.IsDefaultOrNull() || icon.Back.ClickUnfocus())
                    inspected = -1;
                else
                    changed |= e.Try_Nested_Inspect();
            }

            if (inspected == -1)
                foreach (var e in Cstd) {
                    "{0}: ".F(Cstd.currentEnumerationIndex).write(35);
                    changed |= e.Name_ClickInspect_PEGI<T>(null, Cstd.currentEnumerationIndex, ref inspected).nl();
                }
            
            pegi.newLine();
            return changed;
        }
#endif
        #endregion

        #region Encode & Decode
        public static StdEncoder Encode(this Countless<string> c)
        {
            var cody = new StdEncoder();
            List<int> inds;
            List<string> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add_String(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<string> c)
        {
            c = new Countless<string>();
            var cody = new StdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData();

        }

        public static StdEncoder Encode(this Countless<float> c)
        {
            var cody = new StdEncoder();
            List<int> inds;
            List<float> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<float> c)
        {
            c = new Countless<float>();
            var cody = new StdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToFloat();

        }

        public static StdEncoder Encode(this Countless<Vector3> c)
        {
            var cody = new StdEncoder();
            if (c != null)
            {
                List<int> inds;
                List<Vector3> vals = c.GetAllObjs(out inds);
                for (int i = 0; i < inds.Count; i++)
                    cody.Add(inds[i].ToString(), vals[i]);
            }
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<Vector3> c)
        {
            c = new Countless<Vector3>();
            var cody = new StdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToVector3();

        }

        public static StdEncoder Encode(this Countless<Quaternion> c)
        {
            var cody = new StdEncoder();
            List<int> inds;
            List<Quaternion> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<Quaternion> c)
        {
            c = new Countless<Quaternion>();
            var cody = new StdDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToQuaternion();

        }

        #endregion

        public static int Get(this UnnullableSTD<CountlessInt> unn, int group, int index)
        {
            var tg = TryGet(unn, group);
            if (tg == null)
                return 0;
            return tg[index];
        }

        public static bool Get(this UnnullableSTD<CountlessBool> unn, int group, int index) {
            var tg = TryGet(unn, group);
            if (tg == null)
                return false;
            return tg[index];
        }

        public static T Get<T>(this UnnullableSTD<CountlessSTD<T>> unn, int group, int index) where T: ISTD, new()
        {
            var tg = TryGet(unn, group);
            if (tg == null)
                return default(T);
            return tg[index];
        }

        public static T TryGet<T>(this UnnullableSTD<T> unn, int index) where T : ISTD, new()
        {
            if (unn != null)
                return unn.GetIfExists(index);
            return default(T);
        }

    }

}