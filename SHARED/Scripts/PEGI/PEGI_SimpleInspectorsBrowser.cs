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
            case "refs": data.DecodeInto(out nestedReferenceDatas); break;
            default: return false;
        }
        return true;
    }

    public override StdEncoder Encode() => 
        this.EncodeUnrecognized().Add("refs", nestedReferenceDatas);

    #region Inspector
#if PEGI
    public override bool Inspect() {
        bool changed = base.Inspect();
        "inspect Objects:".enter_List_Obj(objects, ref inspectedStuff, 3, nestedReferenceDatas);
        return changed;
    }
#endif
    #endregion
}
