using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization;
using PlayerAndEditorGUI;



[Serializable]
public class OAMetaBase {

 // Implement only if Large Data marked [NenSerializable] has to be loaded upon request 
    public virtual void SaveSUBData(String name, String OAname) {

    }

    public virtual bool isLoaded() {
        return true;
    }

    public virtual void LoadSUBData(String name, String OAname) {
        Debug.Log("Loading "+name + "  " + OAname);

        return;
    }
}

[Serializable]
public class OrganizedArrayElement<T> {
    public string Name;
    public T data;
    public int Category;
}

[Serializable]
public class OrganisedArray<T> 
#if UNITY_EDITOR
    : IDeserializationCallback where T: OAMetaBase, new() {

    void IDeserializationCallback.OnDeserialization(object sender) {
            ed_PreviousInd = -1;
            NumberHolder = -1;
    }
#else
     where T: OAMetaBase, new()
    {

#endif

    public string SaveName;
    public string[] CategoryNms;
    public OrganizedArrayElement<T>[] elements;



    [NonSerialized]
    public int NumberHolder = -1;
    [NonSerialized]
    public String NameHolder="";
    [NonSerialized]
    public bool ShowCategory;
    [NonSerialized]
    public int CategoryFilter = 0;

    public string[] FilteredNames;
    public int[] FilteredIndexes;

    public void FilterByName(string search, int cur) {
        if (search == null) search = "";
        List<string> lst = new List<string>();
        List<int> ilst = new List<int>();

        for (int i = 0; i < elements.Length; i++)
            if (((i != cur) || (cur < 0) || (String.Compare(elements[cur].Name,search)!= 0)) && ((elements[i].Category == CategoryFilter) || (CategoryFilter == 0) || (search.Length > 1)) && ((search.Length == 0) || (search.isIncludedIn(elements[i].Name)))) {
                lst.Add(elements[i].Name);
                ilst.Add(i);
            }

        FilteredNames = lst.ToArray();
        FilteredIndexes = ilst.ToArray();
    }
    public int ConvertFromFilteredIndex(int no)
    {
        return FilteredIndexes[no];
    }

    public void ReloadSUBData(int no)  {
        elements[no].data.LoadSUBData(elements[no].Name, SaveName);
    }

    public T getSUBData(int no) {
        if (!ValidIndex(no)) {
#if UNITY_EDITOR
                SaveItem(no, "Created On Request " + no);
#endif
             
        }

        if (!elements[no].data.isLoaded())
            elements[no].data.LoadSUBData(elements[no].Name, SaveName);

        return elements[no].data;
    }




    public String GetName() {
        return GetName(NumberHolder);
    }

    public String GetName(int no)
    {
        if (!ValidIndex(no)) return "Empty";
        else return elements[no].Name;
    }

    public T GetData(int no)  {
        if (!ValidIndex(no)) return null; 
        else return elements[no].data;
    }

    public T GetData() {
        if (!ValidIndex(NumberHolder)) return null; 
        else return elements[NumberHolder].data;
    }

    [NonSerialized]
    public int ed_PreviousInd = -1;



    public void Save()  {
#if UNITY_EDITOR
        ResourceSaver.SaveToResources("OrganisedArrays/" + SaveName, this);

        for (int i = 0; i < elements.Length; i++)
            if ((elements[i].data!= null) && (elements[i].data.isLoaded())) elements[i].data.SaveSUBData(elements[i].Name, SaveName);
#endif
    }


    public void SaveCategory(int no, String name)  {
        if (no >= CategoryNms.Length)
        {

            String[] temp = new String[no + 1];

            if (CategoryNms != null)
                Array.Copy(CategoryNms, 0, temp, 0, Mathf.Min(no + 1, CategoryNms.Length));

            CategoryNms = temp;

        }
        CategoryNms[no] = name;

        Save();
    }

    public T SaveItem() {
       if (NumberHolder<0)  NumberHolder =  elements.Length;
        if (NameHolder == null) NameHolder = "Unnamed";
        SaveItem(NumberHolder, NameHolder);
        return elements[NumberHolder].data;
    }

    public void SaveItem(int no) {
        SaveItem(no, elements[no].Name);
    }
    public void SaveItem(int no, String name)  {
        if (no == -1) no = 0;
        int len = elements.Length;
        if (no >= elements.Length)  {

            OrganizedArrayElement<T>[] temp = new OrganizedArrayElement<T>[no + 1];

            Array.Copy(elements, 0, temp, 0, Mathf.Min(no + 1, elements.Length));

            elements = temp;

            elements[no].Category = CategoryFilter;

            for (int i = len; i < elements.Length; i++)
                if (elements[i] == null) 
                    elements[i] = new OrganizedArrayElement<T>();     
        }

        elements[no].Name = name;

            if (elements[no].data.isLoaded())
                elements[no].data.SaveSUBData(elements[no].Name, SaveName);

        Save();
    }



    public OrganisedArray(string Name) {
        SaveName = Name;
        CategoryNms = new string[1];
        CategoryNms[0] = "Any";
        elements = new OrganizedArrayElement<T>[1];
        elements[0] = new OrganizedArrayElement<T>();
    }

    public bool ValidIndex(int no){
        if (no == -1) return false;
        return (no<elements.Length);

    }

    public int Count() {
        return elements.Length;
    }

    public bool PEGI( ref int no)  {
        int before = no;
        if (ed_PreviousInd != no) {
            NameHolder = GetName(no);
            ed_PreviousInd = no;
        }


        pegi.foldout(SaveName + " CATEGORIES", ref ShowCategory);
        pegi.select(ref CategoryFilter, CategoryNms);

        if (ShowCategory) {
            if (pegi.Click("NEW CATEGORY")) {
                SaveCategory(CategoryNms.Length, "NEW CATEGORY " + CategoryNms.Length);
                CategoryFilter = CategoryNms.Length - 1;
                GUI.FocusControl(null);
            }

            if ((CategoryFilter != 0) && (CategoryFilter < CategoryNms.Length))
                pegi.edit(ref CategoryNms[CategoryFilter]);

        }

        pegi.newLine();

        GUI.SetNextControlName(SaveName);
        pegi.edit(ref NameHolder);

        if (GUI.GetNameOfFocusedControl() == SaveName) {
            pegi.newLine();
            FilterByName(NameHolder, no);

            for (int i = 0; i < FilteredNames.Length; i++) {
                if (pegi.Click(FilteredNames[i])) {
                    no = ConvertFromFilteredIndex(i);
                    NameHolder = FilteredNames[i];
                    pegi.FocusControl("none");
                }
                pegi.newLine();
            }
        } else {
            if (CategoryFilter != 0)
                select(ref no);

            if ((ValidIndex(no)) && (ShowCategory)) {
                if ((pegi.select(ref elements[no].Category, CategoryNms)) && (CategoryFilter != 0))
                    CategoryFilter = 0; ;
            }


            pegi.newLine();
        }

        //    OrganisedArrayEditing = false;

        return before != no;
    }

    public bool select(ref int i)  {

#if UNITY_EDITOR
        if (pegi.paintingPlayAreaGUI == false) {
            return ef.select<T>(ref i, this);
        } else
#endif

          {

         
            FilterByName("", i);

            int newNo = -1;
            pegi.select(ref newNo, FilteredNames);
            if (newNo != -1) {
                i = ConvertFromFilteredIndex(newNo);
                NameHolder = FilteredNames[newNo];
                return true;
            }
            return false;
        }
    }

    public bool DrawOrganisedArrayEditing( ref int no, bool ShowSaveButton)  {
        int before = no;
        PEGI(ref no);

        if (!ShowCategory) return (no != before);

        pegi.newLine();

        if (pegi.Click("New")) {
            NameHolder = "NO NAME";
            no = elements.Length;
            NumberHolder = no;
            GUI.FocusControl(null);
        }

        if ((ShowSaveButton) && (pegi.Click("Save")))
            SaveItem(NumberHolder, NameHolder);

        pegi.newLine();
        //  EditorGUILayout.EndHorizontal();

        return (no != before);

    }



}
