using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System;

[ExecuteInEditMode]
public class PEGI_SimpleInspectorsBrowser : ComponentSTD, IPEGI, IKeepMySTD {

    public List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
    
    [SerializeField] string stdData = "";

    public string Config_STD { get { return stdData; } set { stdData = value; } }

    void OnEnable() => this.Load_STDdata();

    void OnDisable() => this.Save_STDdata();
    
    public override bool Decode(string tag, string data)
    {
        switch (tag)
        {
            case "ld": references_Meta.Decode(data); break;
            default: return false;
        }
        return true;
    }

    public override StdEncoder Encode() => 
        this.EncodeUnrecognized().Add("ld", references_Meta);

    #region Inspector
#if PEGI
    public override bool Inspect() {
        bool changed = base.Inspect();
        references_Meta.enter_List_Obj(ref objects, ref inspectedStuff, 3);
        return changed;
    }
#endif
    #endregion
}
