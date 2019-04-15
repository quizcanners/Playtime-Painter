using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PlayerAndEditorGUI;


namespace QuizCannersUtilities {

    public interface ICountlessIndex
    {
        int CountlessIndex { get; set; }
    }
    
    public abstract class CountlessBase : IPEGI, IGotCount {
        private static VariableBranch[] _branchPool = new VariableBranch[32];
        private static VariableBranch[] _fruitPool = new VariableBranch[32];
        private static int _brPoolMax;
        private static int _frPoolMax;
        protected static readonly int BranchSize = 8;

        protected void DiscardFruit(VariableBranch b, int no)
        {
            if ((_frPoolMax + 1) >= _fruitPool.Length)
                _fruitPool = _fruitPool.ExpandBy(32);

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
            {
                _branchPool = _branchPool.ExpandBy(32);
            }
            //Debug.Log("Deleting branch ");
            _branchPool[_brPoolMax] = b.br[no];
            VariableBranch vb = _branchPool[_brPoolMax];
            if (vb.value != 0)
                Debug.Log("Value is " + vb.value + " on delete ");
            //vb.value = 0;
            b.value--;
            b.br[no] = null;
            _brPoolMax++;
        }
        protected void TryReduceDepth()
        {
            while ((br.value < 2) && (br.br[0] != null) && (depth > 0))
            {
                // if (br.value < 1) Debug.Log("Reducing depth on branch with " + br.value);
                _branchPool[_brPoolMax] = br;
                _brPoolMax++;
                var tmp = br.br[0];
                br.br[0] = null;
                br.value = 0;
                br = tmp;
                depth--;
                max /= BranchSize;

                // Debug.Log("Reducing depth to " + depth + " new Range: " + Max);
            }
        }

        private void DiscardCascade(VariableBranch b, int depth)
        {
            if ((_brPoolMax + 1) >= _branchPool.Length)
                _branchPool = _branchPool.ExpandBy(32);
            
            if (depth > 0) {
                for (var i = 0; i < BranchSize; i++)
                {
                    if (b.br[i] == null) continue;
                    DiscardCascade(b.br[i], depth - 1);
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
                var vb = new VariableBranch() {
                    br = new VariableBranch[BranchSize]
            };
                //Debug.Log("Creating new branch ");
                return vb;
            }
            _brPoolMax--;
            //Debug.Log("Returning existing branch");
            return _branchPool[_brPoolMax];
        }
     

        protected VariableBranch GetNewFruit()
        {

            if (_frPoolMax == 0)
            {
                var vb = new VariableBranch();
                //   Debug.Log("Creating new fruit ");
                return vb;
            }
            _frPoolMax--;
            count++;
            // Debug.Log("Returning existing fruit");
            return _fruitPool[_frPoolMax];
        }

        public virtual void Clear()
        {
            DiscardCascade(br, depth);
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

        public int CountForInspector => count;

        public virtual bool IsDefault => count == 0;


#if PEGI
        public virtual bool Inspect()
        {
            ("Depth: " + depth).nl();
            ("First free: " + firstFree).nl();

            return false;
        }
#endif
        #endregion


        protected CountlessBase() {
            max = BranchSize;
            br = GetNewBranch();
        }

      //  public delegate void VariableTreeFunk(ref int dst, int ind, int val);
    }


    public abstract class CfgCountlessBase : CountlessBase, ICanBeDefaultCfg
    {
        public override bool IsDefault { get {
                var def = (br == null || br.value == 0);
              //  if (def) Debug.Log("Found default Countless");
                return def;

            }
        }

        public abstract CfgEncoder Encode();

        public abstract bool Decode(string tg, string data);

        public void Decode(string data)
        {
            Clear();
            data.DecodeTagsFor(this);
        }
    }

    public class CountlessInt : CfgCountlessBase {

        List<int> inds;

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "inds": data.Decode_List(out inds); break;
                case "vals":
                    List<int> vals; data.Decode_List(out vals);
                    for (int i = 0; i < vals.Count; i++)
                        Set(inds[i], vals[i]);
                    inds = null;
                    count = vals.Count;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

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

        public void GetItAll(out List<int> inds, out List<int> vals)
        {
            inds = new List<int>();
            vals = new List<int>();
            GetItAllCascadeInt(ref inds, ref vals, br, depth, 0, max);
        }

        private void GetItAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
        {
            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetItAllCascadeInt(ref inds, ref vals, b.br[i], dp - 1, start + step * i, step);


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
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "inds":
                    List<int> inds; data.Decode_List(out inds);
                    foreach (int i in inds)
                        Set(i, true);
                    count = inds.Count;
                    inds = null;
                    break;
                case "last": lastFreeIndex = data.ToInt(); break;
                default: return false;
            }
            return true;

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
        
        protected virtual void Set(int ind, T obj)
        {
            
            if (ind >= max)
            {
                if (obj.IsDefaultOrNull())
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

            if (!obj.IsDefaultOrNull())
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
                    while (_firstFreeObj < cnt && !objs[_firstFreeObj].IsDefaultOrNull()) _firstFreeObj++;
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

                objs[ar] = default(T);
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

        protected virtual T Get(int ind)
        {
            if (ind >= max || ind<0)
                return default(T);

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? default(T) : objs[vb.br[ind].value];
        }

        public List<T> GetAllObjsNoOrder() => objs.Where(t => !t.IsDefaultOrNull()).ToList();
        
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

        void GetAllCascadeInt(ref List<int> inds, ref List<int> vals, VariableBranch b, int dp, int start, int range)
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

        public virtual bool NotEmpty => objs.Length > 0;

        public IEnumerator<T> GetEnumerator() {
            List<int> indx;
            var all = GetAllObjs(out indx);
            for (var i = 0; i < all.Count; i++) {
                var e = all[i];
                if (e.IsDefaultOrNull()) continue;
                currentEnumerationIndex = indx[i];
                yield return e;
            }
        }

        public int currentEnumerationIndex;

        public virtual T GetIfExists(int ind) => Get(ind);


        #region Inspector
#if PEGI
        public T this[IGotIndex i]
        {
            get { return Get(i.IndexForPEGI); }
            set { Set(i.IndexForPEGI, value); }
        }

        private int _edited = -1;

        public override bool Inspect()
        {
            var changed = false;

            if (_edited == -1)
            {

                List<int> indxs;
                var allElements = GetAllObjs(out indxs);

                for (var i = 0; i < allElements.Count; i++)
                {
                    var ind = indxs[i];
                    var el = allElements[i];

                    if (icon.Delete.Click())
                        this[ind] = default(T);
                    else
                        el.Name_ClickInspect_PEGI<T>(null, ind, ref _edited);
                }

            }
            else
            {
                if (icon.List.Click("Back to elements window", ref changed))
                    _edited = -1;
                else
                    this[_edited].Try_Nested_Inspect();
            }
            return changed;
        }

     
#endif
        #endregion
    }

    // Unnulable classes will create new instances
    public class UnNullable<T> : Countless<T> where T : new()
    {
        private static int indexOfCurrentlyCreatedUnNullable;

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
                return default(T);

            var d = depth;
            var vb = br;
            var subSize = max;

            while (d > 0)
            {
                subSize /= BranchSize;
                var no = ind / subSize;
                ind -= no * subSize;
                if (vb.br[no] == null)
                    return default(T);
                d--;
                vb = vb.br[no];
            }

            return vb.br[ind] == null ? default(T) : objs[vb.br[ind].value];
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

                _objs[ar] = default(List<T>);
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

        public IEnumerator GetEnumerator() => _objs.Where(e => !e.IsDefaultOrNull()).GetEnumerator();
        
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
        public static bool Inspect<TG, T>(this TG countless, ref int inspected) where TG : CountlessCfg<T> where T: ICfg, IPEGI, new() {

            var changed = false;
            
            if (inspected > -1) {
                var e = countless[inspected];
                if (e.IsDefaultOrNull() || icon.Back.ClickUnFocus())
                    inspected = -1;
                else
                    changed |= e.Try_Nested_Inspect();
            }

            var deleted = -1;

            if (inspected == -1)
                foreach (var e in countless) {
                    if (icon.Delete.Click()) deleted = countless.currentEnumerationIndex;
                    "{0}: ".F(countless.currentEnumerationIndex).write(35);
                    changed |= e.Name_ClickInspect_PEGI<T>(null, countless.currentEnumerationIndex, ref inspected).nl();
                }
            if (deleted != -1)
                countless[deleted] = default(T);
            
            pegi.newLine();
            return changed;
        }
#endif
        #endregion

        #region Encode & Decode
        public static CfgEncoder Encode(this Countless<string> c)
        {
            var cody = new CfgEncoder();
            List<int> inds;
            List<string> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add_String(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<string> c)
        {
            c = new Countless<string>();
            var cody = new CfgDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData();

        }

        public static CfgEncoder Encode(this Countless<float> c)
        {
            var cody = new CfgEncoder();
            List<int> inds;
            List<float> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<float> c)
        {
            c = new Countless<float>();
            var cody = new CfgDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToFloat();

        }

        public static CfgEncoder Encode(this Countless<Vector3> c)
        {
            var cody = new CfgEncoder();
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
            var cody = new CfgDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToVector3();

        }

        public static CfgEncoder Encode(this Countless<Quaternion> c)
        {
            var cody = new CfgEncoder();
            List<int> inds;
            List<Quaternion> vals = c.GetAllObjs(out inds);
            for (int i = 0; i < inds.Count; i++)
                cody.Add(inds[i].ToString(), vals[i]);
            return cody;
        }

        public static void DecodeInto_Countless(this string data, out Countless<Quaternion> c)
        {
            c = new Countless<Quaternion>();
            var cody = new CfgDecoder(data);
            foreach (var tag in cody)
                c[tag.ToInt()] = cody.GetData().ToQuaternion();

        }

        #endregion

        /*
        public static int Get(this UnNullableCfg<CountlessInt> unn, int group, int index)
        {
            var tg = TryGet(unn, group);
            return tg?[index] ?? 0;
        }

        public static bool Get(this UnNullableCfg<CountlessBool> unn, int group, int index) {
            var tg = TryGet(unn, group);
            return tg != null && tg[index];
        }

        public static T Get<T>(this UnNullableCfg<CountlessCfg<T>> unn, int group, int index) where T: ISTD, new()
        {
            var tg = TryGet(unn, group);
            return tg == null ? default(T) : tg[index];
        }
*/
        public static T TryGet<T>(this UnNullableCfg<T> unn, int index) where T : ICfg, new() => unn != null ? unn.GetIfExists(index) : default(T);
        

    }

}