using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff
{

    public interface CanCopy<T> where T : new()
    {
        T DeepCopy();
        ArrayManager<T> getArrMan();

    }

    [Serializable]
    public abstract class SRLZTreeBase
    { // Not for dynamic work, but for storing large info and quick access


        [Serializable]
        public class SRLZ_Branch
        {
            public int[] br;

            public SRLZ_Branch(int size)
            {
                br = new int[size];
            }

            public SRLZ_Branch()
            {

            }

            public SRLZ_Branch DeepCopy()
            {
                SRLZ_Branch tmp = new SRLZ_Branch(br.Length);
                for (int i = 0; i < br.Length; i++)
                    tmp.br[i] = br[i];

                return tmp;
            }
        }

        protected static ArrayManager<SRLZ_Branch> array = new ArrayManager<SRLZ_Branch>();
        protected static int branchSize = 8;

        protected SRLZ_Branch[] branches;
        protected int depth;
        protected int Max;
        protected int root;
        protected SRLZ_Branch[] path;
        protected int[] pathInd;

#if !NO_PEGI
        void branchPEGI(SRLZ_Branch b, int tdepth)
        {
            pegi.newLine();
            pegi.write("Exploring depth" + tdepth);
            pegi.newLine();

            for (int i = 0; i < branchSize; i++)
            {
                int index = b.br[i];
                if (index != -1)
                {
                    SRLZ_Branch nextbr = branches[index];
                    if (nextbr.br.Length == 2)
                        pegi.write("_Fruit: " + index);
                    else
                    {
                        pegi.write("branch" + index);
                        pegi.newLine();
                        branchPEGI(nextbr, tdepth - 1);
                    }
                    pegi.newLine();
                }
            }
        }

        public void PEGI()
        {
            pegi.write("ROOT: " + root);
            branchPEGI(branches[root], depth);
        }

#endif

        protected void RemoveFromArray(int no)
        {

            for (int i = 0; i < branches.Length; i++)
            {
                int[] tmp = branches[i].br;
                if (tmp.Length == branchSize)
                {
                    for (int j = 0; j < branchSize; j++)
                        if (tmp[j] > no)
                            branches[i].br[j] -= 1;
                        else if (tmp[j] == no)
                            branches[i].br[j] = -1;
                }
            }

            if (root > no)
                root--;

            //Debug.Log("Removing "+no);

            array.Remove(ref branches, no);
        }

        protected void TryReduceDepth()
        {
            while (depth > 0)
            {
                SRLZ_Branch br = branches[root];

                for (int i = 1; i < branchSize; i++)
                    if (br.br[i] != -1)
                        return;

                RemoveFromArray(root);
                root = br.br[0];
                if (root == -1)
                    root = getNewBranch();

                depth--;
                Max /= branchSize;

                Debug.Log("SRLZ Reducing depth to " + depth + " new Range : " + Max);
            }
        }

        protected void TryRemoveBranches(int d, SRLZ_Branch vb)
        {
            while (d < depth)
            {
                for (int i = 0; i < branchSize; i++)
                    if (vb.br[i] != -1)
                        break;
                vb = path[d];
                RemoveFromArray(vb.br[pathInd[d]]);
                d++;
            }

            TryReduceDepth();
        }

        protected void TryRemoveBranch(int ind)
        {
            // This was a problem
            SRLZ_Branch b = branches[ind];
            for (int i = 0; i < branchSize; i++)
                if (b.br[i] != -1)
                    return;
            RemoveFromArray(ind);
        }

        protected int getNewBranch()
        {
            SRLZ_Branch tmp = array.AddAndInit(ref branches);//new SRLZ_Branch();
            tmp.br = new int[branchSize];
            for (int i = 0; i < branchSize; i++)
                tmp.br[i] = -1;

            int ind = branches.Length - 1;

            //Debug.Log("Adding brunch "+ind);

            return ind;
        }

        protected int getNewFruit(int ind)
        {

            SRLZ_Branch tmp = array.AddAndInit(ref branches);
            tmp.br = new int[2];
            tmp.br[0] = 0;
            tmp.br[1] = ind;

            int index = branches.Length - 1;

            //  Debug.Log("Adding fruit " + index);

            return index;
        }

        public abstract void GetItAll(out List<int> inds, out List<int> vals);

        public virtual void Clear()
        {
            CleanOrInit();

        }

        void CleanOrInit()
        {
            Max = branchSize;
            depth = 0;
            branches = new SRLZ_Branch[0];
            path = new SRLZ_Branch[0];
            pathInd = new int[0];
            root = getNewBranch();
        }

        public SRLZTreeBase()
        {
            CleanOrInit();
        }

        public delegate void SRLZ_TreeFunks(ref int dst, int ind, int val);

        protected void ExpandForIndex(int ind)
        {
            while (ind >= Max)
            {
                depth++;
                Max *= branchSize;
                int newbr = getNewBranch();
                branches[newbr].br[0] = root;
                int oldroot = root;
                root = newbr;

                TryRemoveBranch(oldroot);
            }
            path = new SRLZ_Branch[depth];
            pathInd = new int[depth];
        }

        public bool ProcessValue(int ind, int bitNo, int val, SRLZ_TreeFunks bitFunk, bool exitOnZero)
        {
            if (ind >= Max)
            {
                if (exitOnZero) return false;
                ExpandForIndex(ind);
            }

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;
            int orInd = ind;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == -1) { if (exitOnZero) return false; vb.br[no] = getNewBranch(); }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = branches[vb.br[no]];
            }

            if (vb.br[ind] == -1)
            {
                if (exitOnZero) return false;
                vb.br[ind] = getNewFruit(orInd);
            }

            SRLZ_Branch fvb = branches[vb.br[ind]];
            bitFunk(ref fvb.br[0], bitNo, val);

            bool rslt = (((fvb.br[0] & (int)(0x00000001 << bitNo)) != 0) ? true : false);

            if (fvb.br[0] == 0)
            {
                RemoveFromArray(vb.br[ind]);
                TryRemoveBranches(d, vb);
            }

            return rslt;
        }

        protected void CopyFrom(SRLZTreeBase other)
        {

            depth = other.depth;
            Max = other.Max;
            root = other.root;
            if (other.path == null) other.path = new SRLZ_Branch[0]; // TEMP
            if (other.pathInd == null) other.pathInd = new int[0]; // TEMP
            path = new SRLZ_Branch[other.path.Length];
            pathInd = new int[other.pathInd.Length];

            branches = new SRLZ_Branch[other.branches.Length];
            for (int i = 0; i < other.branches.Length; i++)
                branches[i] = other.branches[i].DeepCopy();

        }

    }

    [Serializable]
    public class SRLZ_TreeInt : SRLZTreeBase
    {

        public override void GetItAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetItAllCascadeInt(ref inds, ref vals, branches[root], depth, 0, Max);
        }
        void GetItAllCascadeInt(ref List<int> inds, ref List<int> vals, SRLZ_Branch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != -1)
                        GetItAllCascadeInt(ref inds, ref vals, branches[b.br[i]], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                int[] tmp = b.br;

                for (int i = 0; i < 8; i++)
                    if (tmp[i] != -1)
                    {
                        inds.Add(start + i);
                        vals.Add(branches[tmp[i]].br[0]);
                    }
            }


        }

        int Get(int ind)
        {
            if (ind >= Max)
                return 0;

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == -1)
                    return 0;

                d--;
                vb = branches[vb.br[no]];
            }

            if (vb.br[ind] == -1)
                return 0;

            SRLZ_Branch fvb = branches[vb.br[ind]];

            return fvb.br[0];
        }

        void Set(int ind, int val)
        {

            if (val != 0)
            {
                if (ind >= Max)
                    ExpandForIndex(ind);

                //  Debug.Log("Setting "+val+" to "+ind);

                int d = depth;
                SRLZ_Branch vb = branches[root];
                int subSize = Max;
                int orInd = ind;

                while (d > 0)
                {

                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == -1) { vb.br[no] = getNewBranch(); }
                    d--;
                    vb = branches[vb.br[no]];
                }

                if (vb.br[ind] == -1)
                    vb.br[ind] = getNewFruit(orInd);

                //  Debug.Log(val + " ind " + ind+" goes to "+ vb.br[ind]); //3 0 1 ... 3  3  6

                branches[vb.br[ind]].br[0] = val;
            }
            else
            {
                if (ind >= Max)
                    return;

                int d = depth;
                SRLZ_Branch vb = branches[root];
                int subSize = Max;

                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == -1) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = branches[vb.br[no]];
                }

                if (vb.br[ind] == -1)
                    return;

                RemoveFromArray(vb.br[ind]);
                TryRemoveBranches(d, vb);
            }
        }

        public void Add(int ind, int val)
        {

            if (ind >= Max)
                ExpandForIndex(ind);

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;
            int orInd = ind;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == -1) { vb.br[no] = getNewBranch(); }
                d--;
                vb = branches[vb.br[no]];
            }

            if (vb.br[ind] == -1)
                vb.br[ind] = getNewFruit(orInd);

            branches[vb.br[ind]].br[0] += val;

            if (branches[vb.br[ind]].br[0] == 0)
            {
                RemoveFromArray(vb.br[ind]);
                TryRemoveBranches(d, vb);
            }
        }


        public int this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public SRLZ_TreeInt DeepCopy()
        {
            SRLZ_TreeInt tmp = new SRLZ_TreeInt();
            tmp.CopyFrom(this);
            return tmp;
        }

    }

    [Serializable]
    public class SRLZ_TreeBool : SRLZTreeBase
    {

        public List<int> GetAllBool()
        {
            List<int> inds = new List<int>();
            GetItAllCascadeBool(ref inds, branches[root], depth, 0, Max);
            return inds;
        }

        public override void GetItAll(out List<int> inds, out List<int> vals)
        {

            Debug.Log("Use GetAllBool for SRLZ_TreeBool");
            inds = new List<int>();
            vals = new List<int>();
            // GetItAllCascadeBool(ref inds, ref vals, branches[root], depth, 0, Max);
        }

        void GetItAllCascadeBool(ref List<int> inds, SRLZ_Branch b, int dp, int start, int range)
        {

            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != -1)
                        GetItAllCascadeBool(ref inds, branches[b.br[i]], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != -1)
                    {
                        int value = branches[b.br[i]].br[0];

                        for (int j = 0; j < 32; j++)
                            if ((value & (int)(0x00000001 << j)) != 0)
                                inds.Add((start + i) * 32 + j);
                    }
            }
        }

        bool Get(int ind)
        {
            int bitNo = ind % 32;
            ind /= 32;
            if (ind >= Max)
                return false;

            //Debug.Log("Getting item " + ind + " depth " + depth + " max " + Max + " branches " + branches.Length);

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;

                if (vb.br.Length < branchSize)
                    Debug.Log("Size is " + vb.br.Length);

                if (vb.br[no] == -1) // This is likely to be a fruit
                    return false;
                d--;
                vb = branches[vb.br[no]];
            }

            if (vb.br[ind] == -1)
                return false;

            SRLZ_Branch fvb = branches[vb.br[ind]];

            return ((fvb.br[0] & (int)(0x00000001 << bitNo)) != 0);
            //return Get(ind) > 0;
        }

        void Set(int ind, bool val)
        {
            //  Debug.Log("Setting at index " + ind);

            int orInd = ind;
            int bitNo = ind % 32;
            ind /= 32;

            if (ind >= Max)
            {
                if (!val) return;
                ExpandForIndex(ind);
            }

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == -1) { if (!val) return; vb.br[no] = getNewBranch(); }
                d--;
                path[d] = vb;
                pathInd[d] = no;
                vb = branches[vb.br[no]];
            }

            if (vb.br[ind] == -1)
            {
                if (!val) return;
                vb.br[ind] = getNewFruit(orInd);
            }

            int fruitIndex = vb.br[ind];

            SRLZ_Branch fruit = branches[fruitIndex];
            if (val)
                fruit.br[0] |= (int)(0x00000001 << bitNo);
            else
                fruit.br[0] &= (int)(~(0x00000001 << bitNo));


            if (fruit.br[0] == 0)
            {
                RemoveFromArray(fruitIndex);
                TryRemoveBranches(d, vb);
            }

        }

        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public SRLZ_TreeBool DeepCopy()
        {
            SRLZ_TreeBool tmp = new SRLZ_TreeBool();
            tmp.CopyFrom(this);
            return tmp;
        }

    }

    [Serializable]
    public class SRLZ_TreeGeneric<T> : SRLZTreeBase, CanCopy<SRLZ_TreeGeneric<T>> where T : CanCopy<T>, new()
    {

        T[] objs = new T[0];

        public static ArrayManager<SRLZ_TreeGeneric<T>> oarray = new ArrayManager<SRLZ_TreeGeneric<T>>();

        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
        }

        void RemoveObjectFromArray(int no)
        {

            for (int i = 0; i < branches.Length; i++)
            {
                int[] tmp = branches[i].br;
                if ((tmp.Length != branchSize) && (tmp[0] > no))
                    branches[i].br[0] -= 1;

            }

            objs[no].getArrMan().Remove(ref objs, no);
            //array.Remove(no, ref branches);



        }

        public ArrayManager<SRLZ_TreeGeneric<T>> getArrMan()
        {
            return oarray;
        }

        public void Expand(ref T[] args, int add) // no instantiating
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            // for (int i = args.Length - add; i < args.Length; i++)
            //   args[i] = new T();
        }

        void Set(int ind, T obj)
        {

            if (obj != null)
            {

                //  Debug.Log("Setting object into "+ind);

                if (ind >= Max)
                    ExpandForIndex(ind);

                int d = depth;
                SRLZ_Branch vb = branches[root];
                int subSize = Max;
                int orInd = ind;

                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == -1) { vb.br[no] = getNewBranch(); }
                    d--;
                    vb = branches[vb.br[no]];
                }

                int frNo = vb.br[ind];

                if (frNo == -1)
                {
                    frNo = getNewFruit(orInd);
                    vb.br[ind] = frNo;
                    Expand(ref objs, 1);
                    int oind = objs.Length - 1;
                    branches[frNo].br[0] = oind;
                    objs[oind] = obj;
                    //  Debug.Log("Creating new at " + oind);

                }
                else
                {
                    //Debug.Log("overiding at "+ branches[frNo].br[0]);
                    objs[branches[frNo].br[0]] = obj;
                }

            }
            else
            {

                if (ind >= Max)
                    return;


                int d = depth;
                SRLZ_Branch vb = branches[root];
                int subSize = Max;
                //  int orInd = ind;

                while (d > 0)
                {
                    subSize /= branchSize;
                    int no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == -1) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = branches[vb.br[no]];
                }

                int frInd = vb.br[ind];

                if (frInd == -1)
                    return;


                int ar = branches[frInd].br[0];
                // if (ar == 0)
                //   Debug.Log("ar is zero SRLZ");

                //objs[ar] = default(T);
                //firstFreeObj = Mathf.Min(firstFreeObj, ar);

                //  Debug.Log("Deleting object "+ind);

                RemoveObjectFromArray(ar);
                // T.getArrMan()

                RemoveFromArray(frInd);
                //DiscardFruit(vb, ind);
                TryRemoveBranches(d, vb);

                /*  while (d < depth)
                  {
                      if (vb.value > 0)
                          return;
                      vb = path[d];
                      DiscardBranch(vb, pathInd[d]);
                      d++;
                  }

                  TryReduceDepth();*/

            }
        }

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public T[] GetAllUnsorted(out int[] inds)
        {
            inds = new int[objs.Length];
            T[] uns = new T[objs.Length];

            int cnt = 0;
            for (int i = 0; i < branches.Length; i++)
            {
                SRLZ_Branch tmp = branches[i];
                if (tmp.br.Length < branchSize)
                {
                    inds[cnt] = tmp.br[1];
                    uns[cnt] = objs[tmp.br[0]];
                    cnt++;
                }
            }
            return uns;
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            List<T> objects = new List<T>();
            List<int> vals;
            GetAll(out inds, out vals);

            foreach (int i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        void GetAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, branches[root], depth, 0, Max);
        }

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, SRLZ_Branch b, int dp, int start, int range)
        {
            int step = range / branchSize;
            if (dp > 0)
            {
                for (int i = 0; i < branchSize; i++)
                    if (b.br[i] != -1)
                        GetAllCascadeInt(ref inds, ref vals, branches[b.br[i]], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != branchSize)
                    Debug.Log("Error in range: " + range);

                for (int i = 0; i < 8; i++)
                    if (b.br[i] != -1)
                    {
                        inds.Add(start + i);
                        vals.Add(branches[b.br[i]].br[0]);
                    }
            }


        }

        T Get(int ind)
        {
            if (ind >= Max)
                return default(T);

            int d = depth;
            SRLZ_Branch vb = branches[root];
            int subSize = Max;

            while (d > 0)
            {
                subSize /= branchSize;
                int no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == -1)
                    return default(T);
                d--;
                vb = branches[vb.br[no]];
            }

            //   Debug.Log("vb is null "+(vb==null));
            //  Debug.Log("Length is "+vb.br.Length);
            int frInd = vb.br[ind];

            if (frInd == -1)
                return default(T);

            return objs[branches[frInd].br[0]];
        }

        public override void GetItAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            Debug.Log("Using wrong function GetItAll for " + typeof(T).ToString());
        }

        public SRLZ_TreeGeneric<T> DeepCopy()
        {
            SRLZ_TreeGeneric<T> tmp = new SRLZ_TreeGeneric<T>();

            tmp.objs = new T[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                tmp.objs[i] = objs[i].DeepCopy();
            //   tmp.firstFreeObj = firstFreeObj;

            tmp.CopyFrom(this);

            //    tmp.depth = depth;
            //   tmp.Max = Max;
            //   tmp.root = root;
            //  tmp.path = new SRLZ_Branch[path.Length];
            //  tmp.pathInd = new int[pathInd.Length];

            //  tmp.branches = new SRLZ_Branch[branches.Length];
            //for (int i = 0; i < branches.Length; i++)
            //  tmp.branches[i] = branches[i].DeepCopy();

            return tmp;
        }

    }

    public static class SRLZ_TreeFunks
    {


        public static void ToggleBool(ref int dst, int bitNo, int val)
        {
            dst ^= (0x00000001 << bitNo);//1 << bitNo;//(int)glob.BoolAccessFlags[bitNo];
        }

        /* public static void GetBool(ref int dst, int bitNo, int val)  {

         }
         public static void SetBool(ref int dst, int bitNo, int val)
         {
             if (val > 0)
                 dst |= (int)glob.BoolAccessFlags[bitNo];
             else
                 dst &= (int)(~glob.BoolAccessFlags[bitNo]);
         }*/

        public static void Add(ref int dst, int bitNo, int val)
        {
            dst += val;
        }

    }



}