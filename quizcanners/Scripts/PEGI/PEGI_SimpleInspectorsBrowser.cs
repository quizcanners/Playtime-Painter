using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace QuizCannersUtilities
{

    [ExecuteInEditMode]
    public class PEGI_SimpleInspectorsBrowser : ComponentStd, IKeepMyStd
    {

        public List<Object> objects = new List<Object>();

        [SerializeField] string stdData = "";

        public string ConfigStd { get { return stdData; } set { stdData = value; } }

        private void OnEnable() => this.LoadStdData();

        private void OnDisable() => this.SaveStdData();

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "ld": referencesMeta.Decode(data); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() =>
            this.EncodeUnrecognized().Add("ld", referencesMeta);

        #region Inspector
#if PEGI
        public override bool Inspect()
        {
            var changed = base.Inspect();
            referencesMeta.enter_List_UObj(ref objects, ref inspectedItems, 3);
            return changed;
        }
#endif
        #endregion
    }

}