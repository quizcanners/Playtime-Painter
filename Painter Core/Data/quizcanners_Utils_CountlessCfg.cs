using System.Collections.Generic;
using System.Linq;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using UnityEngine;

namespace QuizCanners.Utils
{

    #pragma warning disable IDE0018 // Inline variable declaration


    public class CountlessCfg<T> : CfgCountlessBase where T : ICfg, new() {
        
        protected T[] objs = new T[0];
        private int _firstFreeObj;

        private bool _allowAdd;
        private bool _allowDelete;

        protected static bool IsDefaultOrNull(T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);

        #region Encode & Decode

        private List<int> _tmpDecodeInds;
        public override void DecodeTag(string key, CfgData data) {

            switch (key) {

                case "inds": data.ToList(out _tmpDecodeInds); break;
                case "vals": List<T> tmps; data.ToList(out tmps);
                    for (int i = 0; i < tmps.Count; i++) {
                        var tmp = tmps[i];
                        if (!tmp.Equals(default(T)))
                            this[_tmpDecodeInds[i]] = tmp;
                            
                        
                    }
                    count = tmps.Count;
                    _tmpDecodeInds = null;
                    break;
                case "brws": _edited = data.ToInt(); break;
                case "last": lastFreeIndex = data.ToInt(); break;
                case "add": _allowAdd = data.ToBool(); break;
                case "del": _allowDelete = data.ToBool(); break;
                default:
                    break;
            }
        }

        public override CfgEncoder Encode()
        {
          
            List<int> indexes;
            var values = GetAllObjs(out indexes);

            var cody = new CfgEncoder()
                .Add("inds", indexes)
                .Add("vals", values)
                .Add_IfNotNegative("brws", _edited)
                .Add("last", lastFreeIndex)
                .Add_Bool("add", _allowAdd)
                .Add_Bool("del", _allowDelete);

            return cody;
        }
        #endregion

        private static void Expand(ref T[] args, int add) {
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
            if (ind >= max)
            {
                if (obj == null)
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

                if (vb.br[ind] == null) {
                    vb.br[ind] = GetNewFruit();
                    vb.value += 1;

                    var cnt = objs.Length;
                    while ((_firstFreeObj < cnt) && (objs[_firstFreeObj] != null)) _firstFreeObj++;
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

        public List<T> GetAllObjsNoOrder() => objs.Where(t => t != null).ToList();
        
        public List<T> GetAllObjs(out List<int> indexes)
        {
            var objects = new List<T>();
            List<int> values;
            GetAllOrdered(out indexes, out values);

            foreach (var i in values)
                objects.Add(objs[i]);

            return objects;
        }

        private void GetAllOrdered(out List<int> indexes, out List<int> values)
        {
            indexes = new List<int>();
            values = new List<int>();
            GetAllCascadeInt(ref indexes, ref values, br, depth, 0, max);
        }

        private static void GetAllCascadeInt(ref List<int> indexes, ref List<int> values, VariableBranch b, int dp, int start, int range)
        {
            var step = range / BranchSize;
            if (dp > 0)
            {
                for (var i = 0; i < BranchSize; i++)
                    if (b.br[i] != null)
                        GetAllCascadeInt(ref indexes, ref values, b.br[i], dp - 1, start + step * i, step);


            }
            else
            {

                if (range != BranchSize)
                    Debug.LogError("Error in range: " + range);

                for (var i = 0; i < 8; i++)
                    if (b.br[i] != null)
                    {
                        indexes.Add(start + i);
                        values.Add(b.br[i].value);
                    }
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

        public override void Clear()
        {
            base.Clear();
            objs = new T[0];
            _firstFreeObj = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<int> indexes;
            var all = GetAllObjs(out indexes);
            for (var i = 0; i < all.Count; i++) {

                var e = all[i];

                if (IsDefaultOrNull(e)) continue;
                
                currentEnumerationIndex = indexes[i];
                
                yield return e;
            }
        }

        public int currentEnumerationIndex;

        private int _edited = -1;

        public virtual T GetIfExists(int ind) => Get(ind);

        public pegi.ChangesToken SelectInspect(ref int no)
        {
            List<int> indexes;
            var objs = GetAllObjs(out indexes);
            var filtered = new List<string>();
            var current = -1;

            for (var i = 0; i < objs.Count; i++)
            {
                if (no == indexes[i])
                    current = i;
                filtered.Add("{0}: {1}".F(i, objs[i].ToString()));
            }

            if (pegi.Select(ref current, filtered))
            {
                no = indexes[current];
                return pegi.ChangesToken.True;
            }
            return pegi.ChangesToken.False;
        }

        public override void Inspect()
        {

            if (_edited == -1)  {

                List<int> indexes;
                var allElements = GetAllObjs(out indexes);

                if (_allowAdd && Icon.Add.Click("Add "+typeof(T).ToPegiStringType())) {
                    while (!IsDefaultOrNull(GetIfExists(lastFreeIndex)))
                        lastFreeIndex++;

                    this[lastFreeIndex] = new T();
                    
                }

                pegi.Nl();

                for (var i = 0; i < allElements.Count; i++)  {
                    var ind = indexes[i];
                    var el = allElements[i];

                    if (_allowDelete && Icon.Delete.Click("Clear element without shifting the rest"))
                        this[ind] = default;
                    else
                    {
                        "{0}".F(ind).PL(20).Write();
                        if (pegi.InspectValueInCollection(ref el, ind, ref _edited) && typeof(T).IsValueType)
                            this[ind] = el;
                    }

                    pegi.Nl();
                }

            } 
            else
            {
                if (Icon.List.Click("Back to elements window"))
                    _edited = -1;
                else
                {
                    object el = this[_edited];
                    if (pegi.Nested_Inspect(ref el))
                        this[_edited] = (T)el;
                }
            }
        }

    }
    
    
    public class UnNullableCfg<T> : CountlessCfg<T> where T : ICfg, new()  {
        
        public int indexOfCurrentlyCreatedUnnulable;

        private T Create(int ind) {
            indexOfCurrentlyCreatedUnnulable = ind;
            var tmp = new T();
            Set(ind, tmp);
            return tmp;
        }

        public int AddNew()
        {
            indexOfCurrentlyCreatedUnnulable = -1;

            while (indexOfCurrentlyCreatedUnnulable == -1)
            {
                Get(firstFree);
                firstFree++;
            }

            return indexOfCurrentlyCreatedUnnulable;
        }

        protected override T Get(int ind)  {
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

    
    
    public class UnNullableCfgLists<T> : UnNullableLists<T>, ICfgCustom where T : class, ICfg, IPEGI, new() {

        private int ToInt(string text)
        {
            int variable;
            int.TryParse(text, out variable);
            return variable;
        }


        public void DecodeTag(string key, CfgData data)
        {
            var index = ToInt(key);
            List<T> tmp;// = new List<T>();
            data.ToList(out tmp);
            this[index] = tmp;
        }

        public void DecodeInternal(CfgData data) {
            Clear();
            this.DecodeTagsFrom(data);
        }
        public CfgEncoder Encode()
        {
            var cody = new CfgEncoder();

            List<int> indexes;
            var values = GetAllObjs(out indexes);

            for (var i = 0; i < indexes.Count; i++)
                cody.Add_IfNotEmpty(indexes[i].ToString(), values[i]);

            return cody;
        }

    }

    
}