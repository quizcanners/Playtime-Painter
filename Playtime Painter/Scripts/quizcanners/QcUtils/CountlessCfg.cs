using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using UnityEngine;

namespace QuizCannersUtilities
{
    
    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration


    public class CountlessCfg<T> : CfgCountlessBase where T : ICfg , new() {
        
        protected T[] objs = new T[0];
        private int _firstFreeObj;

        private bool _allowAdd;
        private bool _allowDelete;

        protected static bool IsDefaultOrNull(T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default(T));


        #region Encode & Decode

        private static List<int> _tmpDecodeInds;
        public override bool Decode(string tg, string data) {

            switch (tg) {

                case "inds": data.Decode_List(out _tmpDecodeInds); break;
                case "vals": List<T> tmps; data.Decode_List(out tmps);
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
                    // Legacy method:
                    this[tg.ToInt()] = data.DecodeInto<T>(); break;
            }
            return true;
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

                //  if (ar == 0)
                //    Debug.Log("ar is zero");

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
                    Debug.Log("Error in range: " + range);

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


#if !NO_PEGI
        public override bool Inspect()
        {
            var changed = false;

            if (_edited == -1)  {

                List<int> indexes;
                var allElements = GetAllObjs(out indexes);

                if (_allowAdd && icon.Add.Click("Add "+typeof(T).ToPegiStringType(), ref changed)) {
                    while (!IsDefaultOrNull(GetIfExists(lastFreeIndex)))
                        lastFreeIndex++;

                    this[lastFreeIndex] = new T();
                    
                }

                pegi.nl();

                for (var i = 0; i < allElements.Count; i++)  {
                    var ind = indexes[i];
                    var el = allElements[i];

                    if (_allowDelete && icon.Delete.Click("Clear element without shifting the rest",ref changed))
                        this[ind] = default(T);
                    else
                    {
                        "{0}".F(ind).write(20);
                        pegi.InspectValueInList(el, null, ind, ref _edited);
                    }

                    pegi.nl();

                }

            } else
            {
                if (icon.List.Click("Back to elements window"))
                    _edited = -1;
                else
                    pegi.Try_Nested_Inspect(this[_edited]);
            }
            return changed;
        }
#endif
    }
    
    
    public class UnNullableCfg<T> : CountlessCfg<T> where T : ICfg, new()  {
        
        public static int indexOfCurrentlyCreatedUnnulable;

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

    
    
    public class UnNullableCfgLists<T> : UnNullableLists<T>, ICfg where T : ICfg, IPEGI, new() {

        public bool Decode(string tg, string data)
        {
            List<T> el; 
            var index = tg.ToInt();
            this[index] = data.Decode_List(out el);
            return true;
        }

        public void Decode(string data) {
            Clear();
            data.DecodeTagsFor(this);
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

        //   public const string storyTag = "TreeObj";
        //  public override string getDefaultTagName() { return storyTag; }
    }

    
}