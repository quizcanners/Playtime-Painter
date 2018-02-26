using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class OrganizedArrayEditor  {


    public static bool DrawOrganisedArray<T>(OrganisedArray<T> oa, ref int no) where T : OAMetaBase, new() {
        int before = no;
        if (oa.ed_PreviousInd != no)
        {
            oa.NameHolder = oa.GetName(no);
            oa.ed_PreviousInd = no;
        }


        ef.foldout(oa.SaveName + " CATEGORIES", ref oa.ShowCategory);
        ef.select(ref oa.CategoryFilter, oa.CategoryNms);

        if (oa.ShowCategory)
        {
            if (ef.Click("NEW CATEGORY"))
            {
                oa.SaveCategory(oa.CategoryNms.Length, "NEW CATEGORY " + oa.CategoryNms.Length);
                oa.CategoryFilter = oa.CategoryNms.Length - 1;
                GUI.FocusControl(null);
            }

            if ((oa.CategoryFilter != 0) && (oa.CategoryFilter < oa.CategoryNms.Length))
                oa.CategoryNms[oa.CategoryFilter] = EditorGUILayout.TextField(oa.CategoryNms[oa.CategoryFilter]);

        }

        ef.newLine();

        GUI.SetNextControlName(oa.SaveName);
        ef.edit(ref oa.NameHolder);

        if (GUI.GetNameOfFocusedControl() == oa.SaveName)
        {
            ef.newLine();
            oa.FilterByName(oa.NameHolder, no);

            for (int i = 0; i < oa.FilteredNames.Length; i++)
            {
                if (ef.Click(oa.FilteredNames[i]))
                {
                    no = oa.ConvertFromFilteredIndex(i);
                    oa.NameHolder = oa.FilteredNames[i];
                    EditorGUI.FocusTextInControl("none");
                }
                ef.newLine();
            }
        }
        else
        {
            if (oa.CategoryFilter != 0)
                ef.select<T>(ref no, oa);

            if ((oa.ValidIndex(no)) && (oa.ShowCategory))
            {
                int icbefore = oa.elements[no].Category;
                oa.elements[no].Category = EditorGUILayout.Popup(icbefore, oa.CategoryNms);
                if ((icbefore != oa.elements[no].Category) && (oa.CategoryFilter != 0))
                    oa.CategoryFilter = 0;// oa.CategoryNo[no];
            }


            ef.newLine();
        }

        //    OrganisedArrayEditing = false;

        return before != no;
    }

    public static bool DrawOrganisedArrayEditing<T>(OrganisedArray<T> oa, ref int no, bool ShowSaveButton) where T : OAMetaBase, new()
    {
        int before = no;
        //   OrganisedArrayEditing = true;
        DrawOrganisedArray<T>(oa, ref no);

        if (!oa.ShowCategory) return (no != before);

        // EditorGUILayout.BeginHorizontal();
        ef.newLine();

        if (ef.Click("New"))
        {
            oa.NameHolder = "NO NAME";
            no = oa.elements.Length;
            oa.NumberHolder = no;
            GUI.FocusControl(null);
        }

        if ((ShowSaveButton) && (ef.Click("Save")))
            oa.SaveItem(oa.NumberHolder, oa.NameHolder);

        ef.newLine();
        //  EditorGUILayout.EndHorizontal();

        return (no != before);

    }

}
