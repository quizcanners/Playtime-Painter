using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using UnityEngine;

namespace QuizCanners.Utils 
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0018 // Inline variable declaration

    public abstract class CountlessBase : IPEGI, IGotCount 
    {
        private static VariableBranch[] _branchPool = new VariableBranch[32];
        private static VariableBranch[] _fruitPool = new VariableBranch[32];
        private static int _brPoolMax;
        private static int _frPoolMax;
        protected static readonly int BranchSize = 8;

     
        protected void DiscardFruit(VariableBranch b, int no)
        {
            if ((_frPoolMax + 1) >= _fruitPool.Length)
                QcSharp.ExpandBy(ref _fruitPool, 32);

            _fruitPool[_frPoolMax] = b.br[no];
            var vb = _fruitPool[_frPoolMax];
            vb.value = 0;
            b.br[no] = null;
            b.value--;
            _frPoolMax++;
            count--;
        }

        protected static void DiscardBranch(VariableBranch b, int no)
        {
            if ((_brPoolMax + 1) >= _branchPool.Length)
                QcSharp.ExpandBy(ref _branchPool, 32);
            
            _branchPool[_brPoolMax] = b.br[no];
            VariableBranch vb = _branchPool[_brPoolMax];
            if (vb.value != 0)
                Debug.LogError("Value is " + vb.value + " on delete ");

            b.value--;
            b.br[no] = null;
            _brPoolMax++;
        }
        protected void TryReduceDepth()
        {
            while ((br.value < 2) && (br.br[0] != null) && (depth > 0))
            {
                _branchPool[_brPoolMax] = br;
                _brPoolMax++;
                var tmp = br.br[0];
                br.br[0] = null;
                br.value = 0;
                br = tmp;
                depth--;
                max /= BranchSize;
            }
        }

        private void DiscardCascade(VariableBranch b, int dep)
        {
            if ((_brPoolMax + 1) >= _branchPool.Length)
                QcSharp.ExpandBy(ref _branchPool, 32);
            
            if (dep > 0) {
                for (var i = 0; i < BranchSize; i++)
                {
                    if (b.br[i] == null) continue;
                    DiscardCascade(b.br[i], dep - 1);
                    DiscardBranch(b, i);
                }
            }
            else
            {
                for (var i = 0; i < 8; i++)
                    if (b.br[i] != null)
                        DiscardFruit(b, i);
            }

        }
        protected static VariableBranch GetNewBranch()
        {
            if (_brPoolMax == 0)
            {
                var vb = new VariableBranch
                {
                    br = new VariableBranch[BranchSize]
                };

                return vb;
            }

            _brPoolMax--;
            return _branchPool[_brPoolMax];
        }
     
        protected VariableBranch GetNewFruit()
        {
            count++;
            if (_frPoolMax == 0)
            {
                var vb = new VariableBranch();
                return vb;
            }
            _frPoolMax--;
           

            return _fruitPool[_frPoolMax];
        }

        public virtual void Clear()
        {
            DiscardCascade(br, depth);
            Reset();
        }

        protected void Reset() 
        {
           
            count = 0;
            depth = 0;
            path = new VariableBranch[0];
            pathInd = new int[0];
            lastFreeIndex = 0;

            max = BranchSize;
            br = GetNewBranch();
        }

        protected int firstFree;
        protected int depth;
        protected int max;
        protected int count;
        protected VariableBranch[] path;
        protected int[] pathInd;
        protected VariableBranch br;
        protected int lastFreeIndex;


        #region Inspector

        public int GetCount() => count;

        public virtual bool IsDefault => count == 0;
        
        public virtual void Inspect()
        {
            if ("Clear".PegiLabel().Click().Nl())
                Clear();
            ("Depth: " + depth).PegiLabel().Nl();
            ("First free: " + firstFree).PegiLabel().Nl();
            "Count: {0}".F(count).PegiLabel().Nl();
        }
        #endregion



        protected CountlessBase() 
        {
            Reset();
        }
        
    }


    public abstract class CfgCountlessBase : CountlessBase, ICfgCustom, ICanBeDefaultCfg
    {
        public override bool IsDefault { get {
                var def = (br == null || br.value == 0);
                return def;

            }
        }

        public abstract CfgEncoder Encode();

        public abstract void DecodeTag(string key, CfgData data);

        public void DecodeInternal(CfgData data)
        {
            Clear();
            this.DecodeTagsFrom(data);
        }
    }

    public class CountlessInt : CfgCountlessBase {
        private List<int> inds;

      
        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "inds": data.ToList(out inds); break;
                case "vals":
                    List<int> vals; data.ToList(out vals);
                    for (int i = 0; i < vals.Count; i++)
                        Set(inds[i], vals[i]);
                    inds = null;
                    count = vals.Count;
                    break;
                case "last":  data.ToInt(ref lastFreeIndex); break;
            }

        }

        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder();

            List<int> values;

            GetItAll(out inds, out values);

            cody.Add("inds", inds);
            cody.Add("vals", values);
            cody.Add("last", lastFreeIndex);


            inds = null;

            return cody;
        }

        public void GetItAll(out List<int> indexes, out List<int> vals)
        {
            indexes = new List<int>();
            vals = new List<int>();
            GetItAllCascadeInt(ref indexes, ref vals, br, depth, 0, max);
        }

        private void GetItAllCascadeInt(ref List<int> indexes, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeInt(ref indexes, ref vals, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != BranchSize)
                    Debug.Log("Error in range: " + range);

                for (var i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        indexes.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }


        }

        public int this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        private int Get(int ind)
        {
            if (ind >= max || ind<0)
                return 0;

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return 0;

                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? 0 : vb.br[ind].value;
        }

        private void Set(int ind, int val)
        {

            //Debug.Log("Setting "+ind+" to "+val);

            if (ind >= max)
            {
                if (val == 0)
                    return;
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newBranch = GetNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newBranch.br[0] = br;
                    newBranch.value++;
                    br = newBranch;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;

            if (val != 0)
            {
                while (d > 0)
                {

                    subSize /= BranchSize;
                    var no = ind / subSize;
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
                    subSize /= BranchSize;
                    var no = ind / subSize;
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

            if (ind >= max)
            {
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;


            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
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
            for (var i = 0; i < vals.Count; i++)  {
                currentEnumerationIndex = indx[i];
                yield return vals[i];
            }
        }

        public int currentEnumerationIndex;
    }

    public class CountlessBool : CfgCountlessBase
    {
        
        #region Encode & Decode
        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "inds":
                    List<int> inds;
                    data.ToList(out inds);
                    foreach (int i in inds)
                        Set(i, true);
                    count = inds.Count;
                    //inds = null;
                    break;
                case "last":  data.ToInt(ref lastFreeIndex); break;
            }

        }

        public override CfgEncoder Encode() => new CfgEncoder().Add("inds", GetItAll()).Add("last", lastFreeIndex);
        #endregion

        public List<int> GetItAll()
        {
            var inds = new List<int>();
            GetItAllCascadeBool(ref inds, br, depth, 0, max);
            return inds;
        }

        private void GetItAllCascadeBool(ref List<int> inds, VariableBranch b, int dp, int start, int range)
        {

            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeBool(ref inds, b.br[i], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != BranchSize)
                    Debug.Log("Error in range: " + range);

                for (var i = 0; i < 8; i++)
                {
                    var branch = b.br[i];
                    
                    if (branch == null) continue;
                    
                    var value = branch.value;

                    for (var j = 0; j < 32; j++)
                        if ((value & 0x00000001 << j) != 0)
                            inds.Add((start + i) * 32 + j);
                }
            }
        }

        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        private bool Get(int ind)
        {

            if (ind < 0)
                return false;

            var bitNo = ind % 32;
            ind /= 32;
            if (ind >= max)
                return false;

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return false;
                d--;
                vb = vb.br[no];
            }

            if (vb.br[ind] == null)
                return false;

            var fvb = vb.br[ind];

            return ((fvb.value & 0x00000001 << bitNo) != 0);

        }

        private void Set(int ind, bool val)
        {
            var bitNo = ind % 32;
            ind /= 32;

            if (ind >= max)
            {
                if (!val)
                    return;
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;


            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
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

            var fvb = vb.br[ind];
            
            if (val)
                fvb.value |= 0x00000001 << bitNo;
            else
                fvb.value &= ~(0x00000001 << bitNo);
           
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



            var bitNo = ind % 32;
            ind /= 32;

            if (ind >= max)
            {
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newbr = GetNewBranch();
                    newbr.br[0] = br;
                    newbr.value++;
                    br = newbr;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;


            while (d > 0)
            {

                subSize /= BranchSize;
                var no = ind / subSize;
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

            var fvb = vb.br[ind];
            
            fvb.value ^= 0x00000001 << bitNo;

            var result = ((fvb.value & 0x00000001 << bitNo) != 0);

            if (fvb.value == 0)
                DiscardFruit(vb, ind);

            while (d < depth)
            {
                if (vb.value > 0)
                    return result;
                vb = path[d];
                DiscardBranch(vb, pathInd[d]);
                d++;
            }

            TryReduceDepth();

            return result;
        }

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var i in GetItAll())
                yield return i;
        }
    }

///  Generic Trees
    public class Countless<T> : CountlessBase {
        
        protected T[] objs = new T[0];
        private int _firstFreeObj;

        public pegi.ChangesToken Select<T>(ref int no)
        {
            List<int> indexes;
            var objs = GetAllObjs(out indexes);
            var filtered = new List<string>();
            var current = -1;

            for (var i = 0; i < objs.Count; i++)
            {
                if (no == indexes[i])
                    current = i;
                filtered.Add(objs[i].GetNameForInspector());
            }

            if (pegi.Select(ref current, filtered))
            {
                no = indexes[current];
                return pegi.ChangesToken.True;
            }
            return pegi.ChangesToken.False;
        }


        private static void Expand(ref T[] args, int add)  {
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
        
        protected virtual void Set(int ind, T obj)
        {
            
            if (ind >= max)
            {
                if (QcSharp.IsDefaultOrNull(obj))
                    return;
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newBranch = GetNewBranch();
                    newBranch.br[0] = br;
                    newBranch.value++;
                    br = newBranch;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;

            if (!QcSharp.IsDefaultOrNull(obj))
            {
                while (d > 0)
                {
                    subSize /= BranchSize;
                    var no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    var cnt = objs.Length;
                    while (_firstFreeObj < cnt && !QcSharp.IsDefaultOrNull(objs[_firstFreeObj])) _firstFreeObj++;
                    if (_firstFreeObj >= cnt)
                        Expand(ref objs, BranchSize);

                    objs[_firstFreeObj] = obj;
                    vb.br[ind].value = _firstFreeObj;
                }
                else
                    objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= BranchSize;
                    var no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;

                var ar = vb.br[ind].value;

                objs[ar] = default;
                _firstFreeObj = Mathf.Min(_firstFreeObj, ar);

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

        public bool TryGet(int index, out T value)
        {
            value = default(T);

            if (index >= max || index < 0)
                return false;

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = index / subSize;
                index -= no * subSize;
                if (vb.br[no] == null)
                    return false;
                d--;
                vb = vb.br[no];
            }

            if (vb.br[index] == null)
                return false;
            else
            {
                value = objs[vb.br[index].value];
                return true;
            }
        }

        protected virtual T Get(int ind)
        {
            if (ind >= max || ind<0)
                return default;

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default;
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? default : objs[vb.br[ind].value];
        }

        public List<T> GetAllObjs(out List<int> inds)
        {
            var objects = new List<T>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (var i in vals)
                objects.Add(objs[i]);

            return objects;
        }

        private void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, max);
        }

        private void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != BranchSize)
                    Debug.Log("Error in range: " + range);

                for (var i = 0; i < 8; i++)
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
            _firstFreeObj = 0;
        }

        public IEnumerator<T> GetEnumerator() {
            List<int> indx;
            var all = GetAllObjs(out indx);
            for (var i = 0; i < all.Count; i++) {
                var e = all[i];
                if (QcSharp.IsDefaultOrNull(e)) continue;
                currentEnumerationIndex = indx[i];
                yield return e;
            }
        }

        [NonSerialized] public int currentEnumerationIndex;

        public virtual T GetIfExists(int ind) => Get(ind);


        #region Inspector
        private int _edited = -1;
        private int _testIndex = 100;

        public override void Inspect()
        {
             base.Inspect();

            if (_edited == -1)
            {

                List<int> indxs;
                var allElements = GetAllObjs(out indxs);

                for (var i = 0; i < allElements.Count; i++)
                {
                    var ind = indxs[i];
                    var el = allElements[i];

                    ind.ToString().PegiLabel(60).Write();

                    if (Icon.Delete.Click())
                        this[ind] = default;
                    else if (pegi.InspectValueInCollection(ref el, ind, ref _edited).Nl() && typeof(T).IsValueType)
                        this[ind] = el;
                }


                if (objs.Length > 0)
                {
                    "Test: ".PegiLabel().Edit(ref _testIndex);
                    if (Icon.Add.Click().Nl())
                    {
                        this[_testIndex] = objs[0];
                    }
                }
            }
            else
            {
                if (Icon.List.Click("Back to elements window"))
                    _edited = -1;
                else
                {
                    object el = this[_edited];
                    if (pegi.Nested_Inspect(ref el).Nl())
                        this[_edited] = (T)el;
                }
            }
        }
        
        #endregion
    }

    

    // Unnulable classes will create new instances
    public class UnNullable<T> : Countless<T> where T : new()
    {
        private int indexOfCurrentlyCreatedUnNullable;

        private T Create(int ind)
        {
            indexOfCurrentlyCreatedUnNullable = ind;
            var tmp = new T();
            Set(ind, tmp);
            return tmp;
        }

        public int AddNew()
        {
            indexOfCurrentlyCreatedUnNullable = -1;

            while (indexOfCurrentlyCreatedUnNullable == -1)
            {
                Get(firstFree);
                firstFree++;
            }

            return indexOfCurrentlyCreatedUnNullable;
        }

        protected override T Get(int ind)
        {
            var originalIndex = ind;

            if (ind >= max)
                return Create(originalIndex);

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? Create(originalIndex) : objs[vb.br[ind].value];
        }

        public override T GetIfExists(int ind)
        {
            // int originalIndex = ind;

            if (ind >= max)
                return default;

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default;
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? default : objs[vb.br[ind].value];
        }

    }

    // List trees
    public class UnNullableLists<T> : CountlessBase, IEnumerable {
        private List<T>[] _objs = new List<T>[0];
        private int _firstFreeObj;

        private static void Expand(ref List<T>[] args, int add) // no instantiating
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

        private void Set(int ind, List<T> obj)
        {
            if (ind >= max)
            {
                if (obj == null)
                    return;
                while (ind >= max)
                {
                    depth++;
                    max *= BranchSize;
                    var newBranch = GetNewBranch();
                    //newbr.br = new VariableBranch[branchSize];
                    newBranch.br[0] = br;
                    newBranch.value++;
                    br = newBranch;
                }
                path = new VariableBranch[depth];
                pathInd = new int[depth];
            }

            var d = depth;
            var vb = br;
            var subSize = max;

            if (obj != null)
            {
                while (d > 0)
                {
                    subSize /= BranchSize;
                    var no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) { vb.br[no] = GetNewBranch(); vb.value++; }
                    d--;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    var cnt = _objs.Length;
                    while ((_firstFreeObj < cnt) && (_objs[_firstFreeObj] != null)) _firstFreeObj++;
                    if (_firstFreeObj >= cnt)
                        Expand(ref _objs, BranchSize);

                    _objs[_firstFreeObj] = obj;
                    vb.br[ind].value = _firstFreeObj;
                }
                else
                    _objs[vb.br[ind].value] = obj;

            }
            else
            {
                while (d > 0)
                {
                    subSize /= BranchSize;
                    var no = ind / subSize;
                    ind -= no * subSize;
                    if (vb.br[no] == null) return;
                    d--;
                    path[d] = vb;
                    pathInd[d] = no;
                    vb = vb.br[no];
                }

                if (vb.br[ind] == null)
                    return;


                var ar = vb.br[ind].value;

                _objs[ar] = default;
                _firstFreeObj = Mathf.Min(_firstFreeObj, ar);

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

        public List<List<T>> GetAllObjsNoOrder() => _objs.Where(t => t != null).ToList();
        
        public List<List<T>> GetAllObjs(out List<int> inds)
        {
            var objects = new List<List<T>>();
            List<int> vals;
            GetAllOrdered(out inds, out vals);

            foreach (var i in vals)
                objects.Add(_objs[i]);

            return objects;
        }

        public void GetAllOrdered(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetAllCascadeInt(ref inds, ref vals, br, depth, 0, max);
        }

        private static void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);
            }
            else
            {

                if (range != BranchSize)
                    Debug.Log("Error in range: " + range);

                for (var i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        inds.Add(start + i);
                        vals.Add(b.br[i].value);
                    }
            }
        }

        private List<T> Create(int ind)
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

        private List<T> Get(int ind)
        {
            if (ind < 0)
                return null;

            var originalIndex = ind;

            if (ind >= max)
                return Create(originalIndex);//default(List<T>);

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return Create(originalIndex);
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? Create(originalIndex) : _objs[vb.br[ind].value];
        }

        public override void Clear()
        {
            base.Clear();
            _objs = new List<T>[0];
            _firstFreeObj = 0;
        }

        public IEnumerator GetEnumerator() => _objs.Where(e => !QcSharp.IsDefaultOrNull(e)).GetEnumerator();
        
    }

    [Serializable]
    public class VariableBranch
    {
        public int value;
        public VariableBranch[] br;
    }

    public static class ExtensionsForGenericCountless
    {

        public static CfgEncoder Encode(this Countless<Color> c)
        {
            var cody = new CfgEncoder();
            if (c != null)
            {
                List<int> inds;
                List<Color> vals = c.GetAllObjs(out inds);
                for (int i = 0; i < inds.Count; i++)
                    cody.Add(inds[i].ToString(), vals[i]);
            }
            return cody;
        }

        public static void ToCountless(this CfgData data, out Countless<Color> c)
        {
            c = new Countless<Color>();
            var cody = new CfgDecoder(data);
            foreach (var tag in cody)
            {
                int variable;
                int.TryParse(tag, out variable);
                c[variable] = cody.GetData().ToColor();
            }

        }

    }

#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0018 // Inline variable declaration
}