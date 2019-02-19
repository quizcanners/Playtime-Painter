using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic {


    public class Values : AbstractKeepUnrecognized_STD, IPEGI, IGotCount
    {

        public static Values global = new Values();

        public UnNullableStd<CountlessBool> booleans = new UnNullableStd<CountlessBool>();
        public UnNullableStd<CountlessInt> ints = new UnNullableStd<CountlessInt>();
  //      UnnullableSTD<CountlessInt> enumTags = new UnnullableSTD<CountlessInt>();
  //      UnnullableSTD<CountlessBool> boolTags = new UnnullableSTD<CountlessBool>();

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotDefault("ints", ints)
            .Add_IfNotDefault("bools", booleans);
          //  .Add_IfNotDefault("tags", boolTags)
          //  .Add_IfNotDefault("enumTags", enumTags);
           
        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "ints": data.DecodeInto(out ints); break;
                case "bools": data.DecodeInto(out booleans); break;
            //    case "tags": data.DecodeInto(out boolTags); break;
             //   case "enumTags": data.DecodeInto(out enumTags); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data) {

            booleans = new UnNullableStd<CountlessBool>();
            ints = new UnNullableStd<CountlessInt>();
           // enumTags = new UnnullableSTD<CountlessInt>();
          //  boolTags = new UnnullableSTD<CountlessBool>();

            base.Decode(data);
        }

        #endregion

        #region Get/Set
    //    public bool GetTagBool(ValueIndex ind) => boolTags.Get(ind);

   //     public int GetTagEnum(ValueIndex ind) => enumTags.Get(ind);

       // public void SetTagBool(ValueIndex ind, bool value) => SetTagBool(ind.groupIndex, ind.triggerIndex, value);

     //   public void SetTagBool(TriggerGroup gr, int tagIndex, bool value) => SetTagBool(gr.IndexForPEGI , tagIndex, value);

 /*
        public void SetTagBool(int groupIndex, int tagIndex, bool value) {

            boolTags[groupIndex][tagIndex] = value;

            var s = TriggerGroup.all[groupIndex];

            if (s.taggedBool[tagIndex].Contains(this))
            {
                if (value)
                    return;
                else
                    s.taggedBool[tagIndex].Remove(this);

            }
            else if (value)
                s.taggedBool[tagIndex].Add(this);
        }
*/
      //  public void SetTagEnum(TriggerGroup gr, int tagIndex, int value) => SetTagEnum(gr.IndexForPEGI, tagIndex, value);

      //  public void SetTagEnum(ValueIndex ind, int value) => SetTagEnum(ind.groupIndex, ind.triggerIndex, value);

      /*  public void SetTagEnum(int groupIndex, int tagIndex, int value) {

            enumTags[groupIndex][tagIndex] = value;

            TriggerGroup s = TriggerGroup.all[groupIndex];

            if (s.taggedInts[tagIndex][value].Contains(this)) {
                if (value != 0)
                    return;
                else
                    s.taggedInts[tagIndex][value].Remove(this);

            }
            else if (value != 0)
                s.taggedInts[tagIndex][value].Add(this);
        }*/
        #endregion

        public void Clear()
        {
            ints.Clear();
            booleans.Clear();
        //   RemoveAllTags();
          
        }

     /*   public void RemoveAllTags() {
            List<int> groupInds;
            List<CountlessBool> lsts = boolTags.GetAllObjs(out groupInds);
            //Stories.all.GetAllObjs(out inds);

            for (int i = 0; i < groupInds.Count; i++)
            {
                CountlessBool vb = lsts[i];
                List<int> tag = vb.GetItAll();

                foreach (int t in tag)
                    SetTagBool(groupInds[i], t, false);

            }


            enumTags.Clear();
            boolTags.Clear(); // = new UnnullableSTD<CountlessBool>();
        }*/

        #region Inspector

        public int CountForInspector => booleans.CountForInspector + ints.CountForInspector;// + enumTags.CountForInspector + boolTags.CountForInspector; 

#if PEGI


        public override bool Inspect() {
            
            var changed = false;


            if (icon.Next.Click("Add 1 to logic version (will cause conditions to be reevaluated)").nl())
                    LogicMGMT.AddLogicVersion();

            foreach (var bGr in booleans) {
                var group = TriggerGroup.all[booleans.currentEnumerationIndex];
                foreach (var b in bGr)
                    group[b].Inspect_AsInList().nl(ref changed);
            }

            foreach (var iGr in ints) {
                var group = TriggerGroup.all[ints.currentEnumerationIndex];
                foreach (var i in iGr) 
                   group[iGr.currentEnumerationIndex].Inspect_AsInList().nl(ref changed);
                
            }


            return changed;
        }
        #endif
        #endregion
    }


   

}