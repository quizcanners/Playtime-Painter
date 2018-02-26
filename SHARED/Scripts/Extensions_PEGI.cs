using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Linq;
using PlayerAndEditorGUI;



public static class Extensions_PEGI  {

    public static Dictionary<int, string> editedDic;
    public static bool select_or_Edit_PEGI(this Dictionary<int, string> dic, ref int selected) {
        bool changed = false;

        if (editedDic != dic) {
            changed |= pegi.select(ref selected, dic);
            if (icon.Add.Click(20)) {
                editedDic = dic;
                changed = true;
                SetNewKeyToMax(dic);
            }
        } else {
            if (icon.Close.Click(20)) { editedDic = null; changed = true; }
            else 
                changed |= dic.newElement_PEGI();
            
        }

        return changed;
    }


    public static string newEnumName = "UNNAMED";
    public static int newEnumKey = 1;
    public static bool edit_PEGI(this Dictionary<int, string> dic) {

        bool changed = false; 

        pegi.newLine();

        for (int i = 0; i < dic.Count; i++) {

            var e = dic.ElementAt(i);
            if (icon.Delete.Click(20)) 
                changed |= dic.Remove(e.Key);

            else {
                changed |= pegi.editKey(ref dic, e.Key);
                if (!changed)
                changed |= pegi.edit(ref dic, e.Key);
            }
            pegi.newLine();
        }
        pegi.newLine();

        changed |= dic.newElement_PEGI();

        return changed;
    }

    public static void SetNewKeyToMax(Dictionary<int,string> dic) {
        newEnumKey = 1;
        string dummy;
        while (dic.TryGetValue(newEnumKey, out dummy)) newEnumKey++;
    }

    public static bool newElement_PEGI(this Dictionary<int, string> dic) {
        bool changed = false;
        pegi.newLine();
        pegi.write("______ [Key, Value]");
        pegi.newLine();
        changed |= pegi.edit(ref newEnumKey); changed |= pegi.edit(ref newEnumName);
        string dummy;
        bool isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
        bool isNewValue = !dic.ContainsValue(newEnumName);

        if ((isNewIndex) && (isNewValue) && (pegi.Click("New", 25))) {
            dic.Add(newEnumKey, newEnumName);
            changed = true;
            SetNewKeyToMax(dic);
            newEnumName = "UNNAMED";
        }

        if (!isNewIndex)
            pegi.write("Index Takken by "+dummy);
        else if (!isNewValue)
            pegi.write("Value already assigned ");

        pegi.newLine();

        return changed;
    }


    public static bool TryChangeKey(this Dictionary<int, string> dic, int before, int now) {
        string value;
        if ((!dic.TryGetValue(now, out value)) && dic.TryGetValue(before, out value)) {
            dic.Remove(before);
            dic.Add(now, value);
            return true;
        }
        return false;
    }


}
