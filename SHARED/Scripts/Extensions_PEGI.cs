using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Linq;
using PlayerAndEditorGUI;
using StoryTriggerData;


public static class Extensions_PEGI  {

    public static Dictionary<int, string> editedDic;

    static void AssignUniqueNameIn<T>(this T el, List<T> list) {

        var named = el as iGotName;
        if (named == null) return;

        string tmpName = named.Name;
        bool duplicate = true;
        int counter = 0;
        
        while (duplicate)
        {
            duplicate = false;

            foreach (var e in list)
            {
                var other = e as iGotName;
                if ((other != null) && (!e.Equals(el)) && (String.Compare(tmpName, other.Name) == 0))
                {
                    duplicate = true;
                    counter++;
                    tmpName = named.Name + counter.ToString();
                    break;
                }
            }
        }

            named.Name = tmpName;

    }

    public static T AddWithUniqueName<T>(this List<T> list) where T : new() {
        T e = new T();
        list.Add(e);
        e.AssignUniqueNameIn(list);
        return e;
    }

    public static T AddUniqueName<T> (this List<T> list, string name) where T: new() {
        T e = new T();
        list.Add(e);
        var named = e as iGotName;
        if (named != null)
            named.Name = name;
        e.AssignUniqueNameIn(list);
        return e;
    }


    public static bool PEGI<T> (this List<T> list, ref int edited) where T: new()
    {
        bool changed = false;

        int before = edited;
        edited = Mathf.Clamp(edited, -1, list.Count);
        changed |= (edited != before);


            if (edited == -1) {
                for (int i = 0; i < list.Count; i++)
                {
                if (icon.Delete.Click(25))
                {
                    list.RemoveAt(i);
                    changed = true;
                    i--;
                }
                else
                {
                    var named = list[i] as iGotName;
                    if (named != null) {
                        var n = named.Name;
                        if (pegi.edit(ref n))
                        {
                            changed = true;
                            named.Name = n;
                        }
                    } else
                        pegi.write(list[i].ToString());

                    if ((list[i] is iPEGI) && icon.Edit.Click(25))
                    {
                        changed = true;
                        edited = i;
                    }
                    }

                pegi.newLine();
                }

            if (icon.Add.Click(25))  {
                changed = true;
                list.AddWithUniqueName();
            }
                
            }
            else
            {
            if (icon.Back.Click(25).nl())
            {
                changed = true;
                edited = -1;
            }
            else
            {
                var std = list[edited] as iPEGI;
                if (std != null) changed |= std.PEGI();
            }
           }

        pegi.newLine();
        return changed;
    }

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
