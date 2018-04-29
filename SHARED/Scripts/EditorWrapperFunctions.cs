#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using StoryTriggerData;
using PlayerAndEditorGUI;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.SceneManagement;

public static class ef {

    public static void start(SerializedObject so) {
        elementIndex = 0;
        searchBarInd = 0;
        lineOpen = false;
        serObj = so;
        changes = false;
        //editedStringIndex = 0;
    }

    public static bool end(GameObject go) {
        if ((changes) && (!Application.isPlaying))
            EditorSceneManager.MarkSceneDirty((go == null) ? EditorSceneManager.GetActiveScene() : go.scene);
           
        
        newLine();
        return changes;
    }

    public static bool end() {
        return end(null);
    }




    static bool change { get { changes = true; return true; } }

    public static void checkLine()
    {
        if (!lineOpen)
        {
            pegi.tabIndex = 0;
            EditorGUILayout.BeginHorizontal();
            lineOpen = true;
        }
    }

    public static void newLine()
    {
        if (lineOpen)
        {
            lineOpen = false;
            EditorGUILayout.EndHorizontal();
        }
    }

    //static int editedStringIndex;
    //static string EditedValue;

    public static bool changes;
    static bool lineOpen = false;
    static int selectedFold = -1;
    public static string searchBarInput = "";
    static int selectedSearchBar = 0;
    static int elementIndex;
    static int searchBarInd;
    public static bool searchInFocus = false;
    public static ArrayManagerAbstract<Texture> tarray = new ArrayManagerAbstract<Texture>();
    public static SerializedObject serObj;

    public static List<int> search(List<string> from) {
        checkLine();
        searchInFocus = false;
        List<int> inds = new List<int>();
        string FullName = "ef_SRCH" + searchBarInd;
        string tmp = "";
        GUI.SetNextControlName(FullName);
        if (selectedSearchBar == searchBarInd)
            searchBarInput = EditorGUILayout.TextField(searchBarInput);
        else
            tmp = EditorGUILayout.TextField(tmp);

        if (GUI.GetNameOfFocusedControl() == FullName) {
            selectedSearchBar = searchBarInd;
            searchInFocus = true;
        }

        if (selectedSearchBar == searchBarInd) {
            if (tmp.Length > 0) searchBarInput = tmp;
            selectedSearchBar = searchBarInd;



            int lim = searchBarInput.Length < 2 ? Mathf.Min(10, from.Count) : from.Count;

            for (int i = 0; i < lim; i++)
                if (String.Compare(searchBarInput, from[i]) == 0)
                    inds.Add(i);

            ef.newLine();
            if ((lim < from.Count) && (searchInFocus)) ef.write("showing " + lim + " out of " + from.Count);

        }

        searchBarInd++;
        return inds;
    }

    public static void focusTextInControl(string name) {
        EditorGUI.FocusTextInControl(name);
    }

    public static void NameNext(string name) {
        GUI.SetNextControlName(name);
    }

    public static string nameFocused {
        get {
            return GUI.GetNameOfFocusedControl();
        }
    }

    public static bool isFoldedOut { get { return pegi.isFoldedOut; } set { pegi.isFoldedOut = value; } }

    public static bool foldout(string txt, ref bool state) {
        checkLine();
        state = EditorGUILayout.Foldout(state, txt);
        if (isFoldedOut != state)
            changes = true;
        isFoldedOut = state;
        return isFoldedOut;
    }

    public static bool foldout(string txt, ref int selected, int current) {
        checkLine();

        isFoldedOut = (selected == current);

        if (EditorGUILayout.Foldout(isFoldedOut, txt))
            selected = current;
        else
            if (isFoldedOut) selected = -1;

        if (isFoldedOut != (selected == current))
            changes = true;

        isFoldedOut = selected == current;

        return isFoldedOut;
    }

    public static bool foldout(string txt) {
        checkLine();

        isFoldedOut = foldout(txt, ref selectedFold, elementIndex);

        elementIndex++;

        return isFoldedOut;
    }

    public static void foldIn() {
        elementIndex = -1;
    }

    public static bool select<T>(ref T val, List<T> lst) {
        checkLine();

        List<string> lnms = new List<string>();
        List<int> inxs = new List<int>();
        int jindx = -1;

        for (int j = 0; j < lst.Count; j++) {
            T tmp = lst[j];
            if (!tmp.isGenericNull()) {
                if ((!val.isGenericNull()) && val.Equals(tmp))
                    jindx = lnms.Count;
                lnms.Add(j+": "+tmp.ToString());
                inxs.Add(j);

            }
        }

        if (select(ref jindx, lnms.ToArray())) {
            val = lst[inxs[jindx]];
            return change;
        }
        return false;
    }

    public static bool select<T>(ref T val, T[] lst)
    {
        checkLine();

        List<string> lnms = new List<string>();
        List<int> inxs = new List<int>();
        int jindx = -1;

        for (int j = 0; j < lst.Length; j++)
        {
            T tmp = lst[j];
            if (!tmp.isGenericNull())
            {
                if ((!val.isGenericNull()) && val.Equals(tmp))
                    jindx = lnms.Count;
                lnms.Add(j+": "+tmp.ToString());
                inxs.Add(j);

            }
        }

        if (select(ref jindx, lnms.ToArray()))
        {
            val = lst[inxs[jindx]];
            return change;
        }
        return false;
    }

    public static bool select<T>(ref int no, List<T> lst, int width) {
        checkLine();

        List<string> lnms = new List<string>();
        List<int> indxs = new List<int>();

        int jindx = -1;

        for (int j = 0; j < lst.Count; j++) {
            if (lst[j] != null) {
                if (no == j)
                    jindx = indxs.Count;
                lnms.Add(j+": "+lst[j].ToString());
                indxs.Add(j);

            }
        }

        if (select(ref jindx, lnms.ToArray(), width)) {
            no = indxs[jindx];
            return true;
        }

        return false;

    }

    public static bool select<T>(ref int no, List<T> lst) {
        checkLine();

        List<string> lnms = new List<string>();
        List<int> indxs = new List<int>();

        int jindx = -1;

        for (int j = 0; j < lst.Count; j++) {
            if (lst[j] != null) {
                if (no == j)
                    jindx = indxs.Count;
                lnms.Add(j+": "+lst[j].ToString());
                indxs.Add(j);

            }
        }

        if (select(ref jindx, lnms.ToArray())) {
            no = indxs[jindx];
            return true;
        }

        return false;

    }

   


    public static bool select<T>(ref int no, CountlessSTD<T> tree) where T : iSTD, new() {
        List<int> inds;
        List<T> objs = tree.GetAllObjs(out inds);
        List<string> filtered = new List<string>();
        int tmpindex = -1;
        for (int i = 0; i < objs.Count; i++) {
            if (no == inds[i])
                tmpindex = i;
            filtered.Add(i+": "+objs[i].ToString());
        }

        if (select(ref tmpindex, filtered.ToArray())) {
            no = inds[tmpindex];
            return true;
        }
        return false;
    }

    public static bool select<T>(ref int no, Countless<T> tree) {
        List<int> inds;
        List<T> objs = tree.GetAllObjs(out inds);
        List<string> filtered = new List<string>();
        int tmpindex = -1;
        for (int i = 0; i < objs.Count; i++) {
            if (no == inds[i])
                tmpindex = i;
            filtered.Add(objs[i].ToString());
        }

        if (select(ref tmpindex, filtered.ToArray())) {
            no = inds[tmpindex];
            return true;
        }
        return false;
    }


    public static bool select(List<string> name, SRLZ_TreeBool tree, ref int no) {
        List<int> inds = tree.GetAllBool();
        List<string> filtered = new List<string>();
        foreach (int i in inds)
            filtered.Add(name[i]);

        return select(ref no, filtered.ToArray());
    }

    public static bool select<T>(ref int i, OrganisedArray<T> ar) where T : OAMetaBase, new() {
        checkLine();
        ar.FilterByName("", i);

        int newNo = EditorGUILayout.Popup(-1, ar.FilteredNames);
        if (newNo != -1) {
            i = ar.ConvertFromFilteredIndex(newNo);
            ar.NameHolder = ar.FilteredNames[newNo];
            return change;
        }
        return false;
    }



    public static bool select<T>(ref int i, T[] ar, bool clampValue) where T : IeditorDropdown {
        checkLine();

        bool changed = false;

        List<string> lnms = new List<string>();
        List<int> ints = new List<int>();

        int ind = -1;

        if (clampValue) {
            i = Mathf.Clamp(i, 0, ar.Length);
            if (ar[i].showInDropdown() == false)
                for (int v = 0; v < ar.Length; v++) {
                    T val = ar[v];
                    if (val.showInDropdown()) {
                        changed = true;
                        i = v;
                        break;
                    }
                }
        }


        for (int j = 0; j < ar.Length; j++) {
            T val = ar[j];
            if (val.showInDropdown()) {
                if (i == j) ind = ints.Count;
                lnms.Add(j+": "+val.ToString());
                ints.Add(j);
            }
        }

        int newNo = EditorGUILayout.Popup(ind, lnms.ToArray());
        if (newNo != ind) {
            i = ints[newNo];
            changed = change;
        }
        return changed;

    }

    public static bool select<T>(ref int ind, T[] lst)
    {
        checkLine();

        List<string> lnms = new List<string>();
        //List<int> indxs = new List<int>();

        int before = ind;
        ind = Mathf.Clamp(ind, 0, lst.Length);

        for (int i = 0; i < lst.Length; i++)
        {
            var e = lst[i]; 

            lnms.Add(i + ": " + (e == null ? "Nothing" : e.ToString()));
        }
        if (select(ref ind, lnms.ToArray()))
            return true;

        return ind != before;
    }


    public static bool select(ref int no, string[] from, int width) {
        checkLine();

        int newNo = EditorGUILayout.Popup(no, from, GUILayout.MaxWidth(width));
        if (newNo != no) {
            no = newNo;
            return change;
        }
        return false;
        //to = from[repName];
    }

    public static bool select(ref int no, string[] from) {
        checkLine();

        int newNo = EditorGUILayout.Popup(no, from, EditorStyles.toolbarDropDown);
        if (newNo != no) {
            no = newNo;
            return change;
        }
        return false;
        //to = from[repName];
    }


    public static bool select(ref int no, Dictionary<int, string> from) {
        checkLine();
        string[] options = new string[from.Count];

        int ind = no;

        for (int i = 0; i < from.Count; i++) {
            var e = from.ElementAt(i);
            options[i] = e.Value;
            if (no == e.Key)
                ind = i;
        }

        int newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown);
        if (newInd != ind) {
            no = from.ElementAt(newInd).Key;
            return change;
        }
        return false;
    }

    public static bool select(ref int no, Dictionary<int, string> from, int width) {
        checkLine();
        string[] options = new string[from.Count];

        int ind = no;

        for (int i = 0; i < from.Count; i++) {
            var e = from.ElementAt(i);
            options[i] = e.Value;
            if (no == e.Key)
                ind = i;
        }

        int newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(width));
        if (newInd != ind) {
            no = from.ElementAt(newInd).Key;
            return change;
        }
        return false;
    }


    public static bool select(ref int no, Texture[] tex) {
        if (tex.Length == 0) return false;

        checkLine();
        int before = no;
        List<string> tnames = new List<string>();
        List<int> tnumbers = new List<int>();

        int curno = 0;
        for (int i = 0; i < tex.Length; i++)
            if (tex[i] != null) {
                tnumbers.Add(i);
                tnames.Add(i + ": " + tex[i].name);
                if (no == i) curno = tnames.Count - 1;
            }

        curno = EditorGUILayout.Popup(curno, tnames.ToArray());

        if ((curno >= 0) && (curno < tnames.Count))
            no = tnumbers[curno];

        return (before != no);
    }

    public static bool select(ref int current, Type type) {
        checkLine();
        int tmpVal = current;

        string[] name = Enum.GetNames(type);
        int[] val = (int[])Enum.GetValues(type);

        for (int i = 0; i < val.Length; i++)
            if (val[i] == current)
                tmpVal = i;

        if (ef.select(ref tmpVal, name)) {

            current = val[tmpVal];

            return true;
        }

        return false;
    }

    public static bool edit(ref int current, Type type) {
        return select(ref current, type);
    }

    public static bool selectOrAdd(ref int selected, ref Texture[] texes) {

        bool changed = ef.select(ref selected, texes);
        Texture tex = texes[selected];
        Texture newTex = (Texture)EditorGUILayout.ObjectField(tex, typeof(Texture), true);
        if (newTex != tex) {
            for (int i = 0; i < texes.Length; i++)
                if (texes[i] == newTex) {
                    selected = i;
                    ef.newLine();
                    return change;
                }

            bool assigned = false;

            for (int i = 0; i < texes.Length; i++)
                if (texes[i] == null) {
                    assigned = true;
                    selected = i;
                    i = texes.Length;
                }

            if (!assigned) {
                selected = texes.Length;
                tarray.Expand(ref texes, 1);
            }

            texes[selected] = newTex;

            return change;

        }
        ef.newLine();
        return changed;
    }

    public static void tab() {
        checkLine();
        EditorGUILayout.Space();
    }

    public static void Space() {
        checkLine();
        EditorGUILayout.Separator();
        newLine();
    }



    public static void editOrselect(ref string to, string[] from) {
        checkLine();
        to = EditorGUILayout.TextField(to);
        int repName = EditorGUILayout.Popup(-1, from);
        if (repName != -1)
            to = from[repName];
    }



    public static bool edit(this Editor e, string property) {

        // e.serializedObject.Update();
        SerializedProperty tps = e.serializedObject.FindProperty(property);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(tps, true);
        if (EditorGUI.EndChangeCheck()) {
            e.serializedObject.ApplyModifiedProperties();
            return change;
        }
        return false;
    }

    public static bool edit<T>(ref T field) where T : UnityEngine.Object {
        checkLine();
        T tmp = field;
        field = (T)EditorGUILayout.ObjectField(field, typeof(T), true);
        return tmp != field;
    }

    public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
    {
        checkLine();
        T tmp = field;
        field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowDrop);
        return tmp != field;
    }

    public static bool edit<T>(ref T field, string name) where T : UnityEngine.Object {
        checkLine();
        T tmp = field;
        field = (T)EditorGUILayout.ObjectField(name, field, typeof(T), true);
        return tmp != field;
    }

    public static bool edit(int ind, CountlessInt tb) {
        int has = tb[ind];
        if (edit(ref has)) {
            tb[ind] = has;
            return change;
        }
        return false;
    }

    public static bool edit(string label, ref float val)
    {
        checkLine();
        float before = val;
        val = EditorGUILayout.FloatField(label, val);
        return (val != before) ? change : false ;
    }

    public static bool edit(ref float val) {
        checkLine();
        float before = val;
        val = EditorGUILayout.FloatField(val);
        return (val != before) ? change : false;
    }

    public static bool edit(ref int val, int min, int max) {
        checkLine();
        float before = val;
        val = EditorGUILayout.IntSlider(val, min, max); //Slider(val, min, max);
        return (val != before) ? change : false;
    }

    public static bool editPOW(ref float val, float min, float max) {

        checkLine();
        float before = Mathf.Sqrt(val);
        float after = EditorGUILayout.Slider(before, min, max);
        if (before != after) {
            val = after * after;
            return change;
        }
        return false;
    }

    public static bool edit(ref float val, float min, float max) {
        checkLine();
        float before = val;
        val = EditorGUILayout.Slider(val, min, max);
        return (val != before) ? change : false;
    }

    public static bool edit(ref Color col) {

        checkLine();
        Color before = col;
        col = EditorGUILayout.ColorField(col);

        return (before != col) ? change : false;

    }

    public static bool editKey(ref Dictionary<int, string> dic, int key) {
        checkLine();
        int before = key;

        if (editDelayed(ref key, 40))
            return dic.TryChangeKey(before, key) ? change : false;
        /*{
        Debug.Log("Edited to "+key);
        string value;
        if (dic.TryGetValue(pre, out value)) {
            dic.Remove(pre);
            dic.Add(key, value);
            return true;
        }
    }*/
        return false;
    }






    public static bool edit(ref Dictionary<int, string> dic, int atKey) {
        string before = dic[atKey];
        if (editDelayed(ref before)) {
            dic[atKey] = before;
            return change;
        }
        return false;
    }

    public static bool edit(ref int val) {
        checkLine();
        int pre = val;
        val = EditorGUILayout.IntField(val);
        return (val != pre) ? change : false;
    }

    public static bool edit(ref int val, int width) {
        checkLine();
        int pre = val;
        val = EditorGUILayout.IntField(val, GUILayout.MaxWidth(width));
        return (val != pre) ? change : false;
    }

    static int editedIntegerIndex;
    static int editedInteger;
    public static bool editDelayed(ref int val, int width) {
        checkLine();

        if (KeyCode.Return.isDown() && (elementIndex == editedIntegerIndex)) {
            EditorGUILayout.IntField(val, GUILayout.Width(width));
            val = editedInteger;
            elementIndex++;
            return change;
        }

        int tmp = val;
        if (edit(ref tmp)) {
            editedInteger = tmp;
            editedIntegerIndex = elementIndex;//val.GetHashCode().ToString();
        }

        elementIndex++;

        return false;//(String.Compare(before, text) != 0);
    }

    public static bool editDelayed(ref int val)
    {
        checkLine();

        if (KeyCode.Return.isDown() && (elementIndex == editedIntegerIndex))
        {
            EditorGUILayout.IntField(val);
            val = editedInteger;
            elementIndex++;
            return change;
        }

        int tmp = val;
        if (edit(ref tmp))
        {
            editedInteger = tmp;
            editedIntegerIndex = elementIndex;//val.GetHashCode().ToString();
        }

        elementIndex++;

        return false;//(String.Compare(before, text) != 0);
    }

    public static bool edit(string label, ref Vector2 val)
    {
        checkLine();

        var oldVal = val;
        val = EditorGUILayout.Vector2Field(label, val );
      
        return oldVal != val;
    }

    public static bool edit(ref Vector2 val) {
        checkLine();
        bool modified = false;

        modified |= edit(ref val.x);
        modified |= edit(ref val.y);
        return modified ? change : false;
    }

    public static bool edit(ref myIntVec2 val)
    {
        checkLine();
        bool modified = false;
        modified |= edit(ref val.x);
        modified |= edit(ref val.y);
        return modified ? change : false;
    }

    public static bool edit(ref myIntVec2 val, int min, int max)
    {
        checkLine();
        bool modified = false;
        modified |= edit(ref val.x, min, max);
        modified |= edit(ref val.y, min, max);
        return modified ? change : false;
    }

    public static bool edit(ref myIntVec2 val, int min, myIntVec2 max)
    {
        checkLine();
        bool modified = false;
        modified |= edit(ref val.x, min, max.x);
        modified |= edit(ref val.y, min, max.y);
        return modified ? change : false;
    }

    public static bool edit(ref Vector3 val) {
        checkLine();
        bool modified = false;
        modified |= "X".edit(ref val.x).nl() | "Y".edit(ref val.y).nl() | "Z".edit(ref val.z).nl();
        return modified ? change : false;
    }

    static string editedText;
    static string editedHash = "";
    public static bool editDelayed(ref string text) {
        checkLine();


        if (KeyCode.Return.isDown()){
            if (text.GetHashCode().ToString() == editedHash)
            {
                EditorGUILayout.TextField(text);
                text = editedText;
                return change;
            } 
        }

        string tmp = text;
        if (edit(ref tmp)) {
            editedText = tmp;
            editedHash = text.GetHashCode().ToString();
            changes = false;
        }



        return false;//(String.Compare(before, text) != 0);
    }

    public static bool editDelayed(ref string text, int width) {
        checkLine();

        if (KeyCode.Return.isDown() && (text.GetHashCode().ToString() == editedHash)) {
            EditorGUILayout.TextField(text);
            text = editedText;
            return change;
        }

        string tmp = text;
        if (edit(ref tmp, width)) {
            editedText = tmp;
            editedHash = text.GetHashCode().ToString();
            changes = false;
        }



        return false;//(String.Compare(before, text) != 0);
    }

    public static bool edit(Sentance val) {
        string before = val.ToString();
        if (edit(ref before)) {
            val.setTranslation(before);
            return change;
        }
        return false;
    }

    public static bool edit(ref string text) {
        checkLine();
        string before = text;
        text = EditorGUILayout.TextField(text);
        return (String.Compare(before, text) != 0) ? change : false;
    }

    public static bool edit(ref string text, int width) {
        checkLine();
        string before = text;
        text = EditorGUILayout.TextField(text, GUILayout.MaxWidth(width));
        return (String.Compare(before, text) != 0) ? change : false;
    }

    public static bool editBig(ref string text) {
        checkLine();
        string before = text;
        text = EditorGUILayout.TextArea(text);
        return (String.Compare(before, text) != 0) ? change : false;
    }

    public static bool edit(ref string[] texts, int no) {
        checkLine();
        string before = texts[no];
        texts[no] = EditorGUILayout.TextField(texts[no]);
        return (String.Compare(before, texts[no]) != 0) ? change : false;
    }

    public static bool edit(List<string> texts, int no) {
        checkLine();
        string before = texts[no];
        texts[no] = EditorGUILayout.TextField(texts[no]);
        return (String.Compare(before, texts[no]) != 0) ? change : false;
    }

    public static bool editPowOf2(ref int i, int min, int max) {
        checkLine();
        int before = i;
        i = Mathf.ClosestPowerOfTwo((int)Mathf.Clamp(EditorGUILayout.IntField(i), min, max));
        return (i != before) ? change : false;
    }

    public static bool toggleInt(ref int val) {
        checkLine();
        bool before = val > 0;
        if (toggle(ref before)) {
            val = before ? 1 : 0;
            return change;
        }
        return false;
    }

    public static bool toggle(ref bool val) {
        checkLine();
        bool before = val;
        val = EditorGUILayout.Toggle(val, GUILayout.MaxWidth(40));
        return (before != val) ? change : false;
    }

    public static bool toggle(int ind, CountlessBool tb) {
        bool has = tb[ind];
        if (toggle(ref has)) {
            tb.Toggle(ind);
            return true;
        }
        return false;
    }

    public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width) {
        //checkLine();
        bool before = val;

        if (val) {
            if (Click(TrueIcon, tip, width)) val = false;
        } else {
            if (Click(FalseIcon, tip, width)) val = true;
        }

        return (before != val);
    }

    public static bool toggle(ref bool val, string text) {
        checkLine();
        bool before = val;
        val = EditorGUILayout.Toggle(text, val);
        return (before != val) ? change : false;
    }

    public static bool toggle(ref bool val, string text, string tip) {
        checkLine();

        bool before = val;
        GUIContent cont = new GUIContent();
        cont.text = text;
        cont.tooltip = tip;
        val = EditorGUILayout.Toggle(cont, val);
        return (before != val) ? change : false;
    }

    public static bool toggle(int ind, SRLZ_TreeBool tb) {
        checkLine();
        bool has = tb[ind];
        if (toggle(ref has)) {
            tb[ind] = has;
            return change;
        }
        return false;
    }

    public static bool Click(string txt, int width) {
        checkLine();
        return GUILayout.Button(txt, GUILayout.MaxWidth(width)) ? change : false;
    }

    public static bool Click(string txt) {
        checkLine();
        return GUILayout.Button(txt) ? change : false;
    }

    public static bool Click(string txt, string tip) {
        checkLine();
        GUIContent cont = new GUIContent();
        cont.text = txt;
        cont.tooltip = tip;
        return GUILayout.Button(cont) ? change : false;
    }

    public static bool Click(string txt, string tip, int width) {
        checkLine();
        GUIContent cont = new GUIContent();
        cont.text = txt;
        cont.tooltip = tip;
        return GUILayout.Button(cont, GUILayout.MaxWidth(width)) ? change : false;
    }


    public static bool Click(Texture img, int width) {
        checkLine();
        return GUILayout.Button(img, GUILayout.MaxHeight(width), GUILayout.MaxWidth(width + 10)) ? change : false;
    }

    public static bool Click(Texture img, string tip, int width) {
        checkLine();
        GUIContent cont = new GUIContent();
        cont.tooltip = tip;
        cont.image = img;
        return GUILayout.Button(cont, GUILayout.MaxHeight(width), GUILayout.MaxWidth(width + 10)) ? change : false;
    }

    public static void write<T>(T field) where T : UnityEngine.Object {
        checkLine();
        EditorGUILayout.ObjectField(field, typeof(T), false);
    }

    public static void write(Texture icon, int width) {

        checkLine();

        GUIContent c = new GUIContent();
        c.image = icon;

        EditorGUILayout.LabelField(c, GUILayout.MaxWidth(width));
    }

    public static void write(string text, int width) {

        checkLine();

        EditorGUILayout.LabelField(text, EditorStyles.miniLabel, GUILayout.MaxWidth(width));
    }

    public static void write(string text, string tip) {

        checkLine();
        GUIContent cont = new GUIContent();
        cont.text = text;
        cont.tooltip = tip;
        EditorGUILayout.LabelField(cont);
    }

    public static void write(string text, string tip, int width) {

        checkLine();
        GUIContent cont = new GUIContent();
        cont.text = text;
        cont.tooltip = tip;
        EditorGUILayout.LabelField(cont, GUILayout.MaxWidth(width));
    }

    public static void write(string text) {
        checkLine();

        EditorGUILayout.LabelField(text);
    }

    public static void writeHint(string text, MessageType type) {
        checkLine();

        EditorGUILayout.HelpBox(text, type);
    }


    


    public static void ShowTeture(Texture tex) {
        checkLine();
        //Texture2D tex = (Texture2D)
        EditorGUILayout.ObjectField(tex, typeof(Texture2D), true);
    }



    public static void SetDefine(string val, bool to) {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

        if (defines.Contains(val) == to) return;

        if (to)
            defines += " ; " + val;
        else
            defines = defines.Replace(val, "");


        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
    }

    public static bool GetDefine(string define) {

        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        return defines.Contains(define);
    }

       

   

}

#endif