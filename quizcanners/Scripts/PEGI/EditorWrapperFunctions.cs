#if PEGI && UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using QuizCannersUtilities;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

#pragma warning disable IDE1006 // Naming Styles

namespace PlayerAndEditorGUI {

    public static class ef {

        public enum EditorType { Mono, ScriptableObject, Material, Unknown }

        public static EditorType editorType = EditorType.Unknown;

        private static bool _lineOpen = false;
        private static int _selectedFold = -1;
        private static int _elementIndex;
        public static SerializedObject serObj;
        private static Editor _editor;
        private static PEGI_Inspector_Material _materialEditor;

        public static bool DefaultInspector() {
            newLine();

            EditorGUI.BeginChangeCheck();
            switch (editorType) {
                case EditorType.Material:  _materialEditor?.DrawDefaultInspector(); break;
                default: if (_editor!= null) _editor.DrawDefaultInspector(); break;
            }
            return EditorGUI.EndChangeCheck();

        }

        public static bool Inspect<T>(Editor editor) where T : MonoBehaviour
        {
            _editor = editor;

            editorType = EditorType.Mono;

            var o = (T)editor.target;
            var so = editor.serializedObject;

            var go = o.gameObject;
            
            if (go.IsPrefab())
                return false;

            var pgi = o as IPEGI;
            
            if (pgi != null)
            {
                start(so);

#if UNITY_2018_3_OR_NEWER
                var isPrefab = PrefabUtility.IsPartOfAnyPrefab(o);
                

                if (isPrefab &&
                    PrefabUtility.HasPrefabInstanceAnyOverrides( PrefabUtility.GetNearestPrefabInstanceRoot(o), false) &&
                    icon.Save.Click("Update Prefab")) 
                        PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
#endif


                var changed = pgi.Inspect();


#if UNITY_2018_3_OR_NEWER

                if (changed && isPrefab)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(o);

#endif

                if (changes)
                {
                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(go ? SceneManager.GetActiveScene() : go.scene);

                    EditorUtility.SetDirty(go);
                }

                newLine();
                
                return changed;
            }
            else editor.DrawDefaultInspector();

            return false;
        }

        public static bool Inspect_so<T>(Editor editor) where T : ScriptableObject
        {
            _editor = editor;

            editorType = EditorType.ScriptableObject;

            var o = (T)editor.target;
            var so = editor.serializedObject;

            var pgi = o as IPEGI;
            if (pgi != null)
            {
                start(so);
                var changed = pgi.Inspect();
                end(o);
                return changed;
            }
            else editor.DrawDefaultInspector();

            return false;
        }
        
        public static bool Inspect_Material(PEGI_Inspector_Material editor) {

            _materialEditor = editor;

            editorType = EditorType.Material;

            var mat = editor.unityMaterialEditor.target as Material;

            start();

            editor.Inspect(mat);

            end(mat);
            
            return changes;
        }

        public static bool toggleDefaultInspector() =>
            editorType == EditorType.Material 
                ? pegi.toggle(ref PEGI_Inspector_Material.drawDefaultInspector, icon.Config, icon.Debug, "Toggle Between regular and PEGI Material inspector", 20).nl() 
                : pegi.toggle(ref PEGI_Inspector_Base.drawDefaultInspector, icon.Config, icon.Debug, "Toggle Between regular and PEGI inspector", 20).nl();
            

        static void start(SerializedObject so = null) {
            _elementIndex = 0;
            PEGI_Extensions.focusInd = 0;
            _lineOpen = false;
            serObj = so;
            pegi.globChanged = false;
        }

        static bool end(UnityEngine.Object obj)
        {

            if (changes)
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                EditorUtility.SetDirty(obj);
            }
            newLine();

            return changes;
        }
        
      private static bool change { get { pegi.globChanged = true; return true; } }

        private static bool Dirty(this bool val) { pegi.globChanged |= val; return val; }

        private static bool changes => pegi.globChanged;

        private static void BeginCheckLine() { checkLine(); EditorGUI.BeginChangeCheck(); }

        private static bool EndCheckLine() => EditorGUI.EndChangeCheck().Dirty(); 

        public static void checkLine()
        {
            if (_lineOpen) return;
            
            pegi.tabIndex = 0;
            EditorGUILayout.BeginHorizontal();
            _lineOpen = true;
            
        }

        public static void newLine()
        {
            if (!_lineOpen) return;
            
            _lineOpen = false;
            EditorGUILayout.EndHorizontal();
            
        }
        
        private static bool IsFoldedOut { get { return pegi.isFoldedOutOrEntered; } set { pegi.isFoldedOutOrEntered = value; } }

        private static bool StylizedFoldOut(bool foldedOut, string txt, string hint = "FoldIn/FoldOut") {

            var cnt = new GUIContent
            {
                text = txt,
                tooltip = hint
            };

            return EditorGUILayout.Foldout(foldedOut, cnt, true);
        }

        public static bool foldout(string txt, ref bool state)
        {
            checkLine();
            state = StylizedFoldOut(state, txt);
            if (IsFoldedOut != state)
                Dirty(true);
            IsFoldedOut = state;
            return IsFoldedOut;
        }

        public static bool foldout(string txt, ref int selected, int current)
        {
            checkLine();

            IsFoldedOut = (selected == current);

            if (StylizedFoldOut(IsFoldedOut, txt))
                selected = current;
            else
                if (IsFoldedOut) selected = -1;


            if (IsFoldedOut != (selected == current))
                Dirty(true);

            IsFoldedOut = selected == current;

            return IsFoldedOut;
        }
        
        public static bool foldout(string txt)
        {
            checkLine();

            IsFoldedOut = foldout(txt, ref _selectedFold, _elementIndex);

            _elementIndex++;

            return IsFoldedOut;
        }

        public static bool select<T>(ref int no, List<T> lst, int width)
        {
            checkLine();

            var listNames = new List<string>();
            var listIndexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                if (lst[j] == null) continue;
                
                if (no == j)
                    current = listIndexes.Count;
                listNames.Add("{0}: {1}".F(j,lst[j].ToPEGIstring()));
                listIndexes.Add(j);

            }



            if (select(ref current, listNames.ToArray(), width))
            {
                no = listIndexes[current];
                return change;
            }

            return false;

        }

        public static bool select<T>(ref int no, CountlessStd<T> tree) where T : ISTD
            , new()
        {
            List<int> indexes;
            var objs = tree.GetAllObjs(out indexes);
            var filtered = new List<string>();
            var current = -1;
            
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == indexes[i])
                    current = i;
                filtered.Add("{0}: {1}".F(i, objs[i].ToPEGIstring()));
            }

            if (select(ref current, filtered.ToArray()))
            {
                no = indexes[current];
                return true;
            }
            return false;
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
            List<int> indexes;
            var objs = tree.GetAllObjs(out indexes);
            var filtered = new List<string>();
            var current = -1;
            
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == indexes[i])
                    current = i;
                filtered.Add(objs[i].ToPEGIstring());
            }

            if (select(ref current, filtered.ToArray()))
            {
                no = indexes[current];
                return true;
            }
            return false;
        }
        
        public static bool select(ref int no, string[] from, int width)
        {
            checkLine();

            var newNo = EditorGUILayout.Popup(no, from, GUILayout.MaxWidth(width));
            if (newNo != no)
            {
                no = newNo;
                return change;
            }
            return false;
        }

        public static bool select(ref int no, string[] from)
        {
            checkLine();

            var newNo = EditorGUILayout.Popup(no, from);
            if (newNo != no)
            {
                no = newNo;
                return change;
            }
            return false;
            //to = from[repName];
        }
        
        public static bool select(ref int no, Dictionary<int, string> from)
        {
            checkLine();
            var options = new string[from.Count];

            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown);
            if (newInd != ind)
            {
                no = from.ElementAt(newInd).Key;
                return change;
            }
            return false;
        }

        public static bool select(ref int no, Dictionary<int, string> from, int width)
        {
            checkLine();
            var options = new string[from.Count];

            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(width));
            if (newInd != ind)
            {
                no = from.ElementAt(newInd).Key;
                return change;
            }
            return false;
        }
        
        public static bool select(ref int no, Texture[] tex)
        {
            if (tex.Length == 0) return false;

            checkLine();
            var before = no;
            var tnames = new List<string>();
            var tnumbers = new List<int>();

            var curno = 0;
            for (var i = 0; i < tex.Length; i++)
                if (tex[i])
                {
                    tnumbers.Add(i);
                    tnames.Add("{0}: {1}".F(i, tex[i].name));
                    if (no == i) curno = tnames.Count - 1;
                }

            curno = EditorGUILayout.Popup(curno, tnames.ToArray());

            if ((curno >= 0) && (curno < tnames.Count))
                no = tnumbers[curno];

            return (before != no);
        }
   
        private static bool select_Type(ref Type current, IReadOnlyList<Type> others, Rect rect)
        {

            var names = new string[others.Count];

            var ind = -1;

            for (var i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = el.ToPEGIstring_Type();
                if (el != null && el == current)
                    ind = i;
            }

            var newNo = EditorGUI.Popup(rect, ind, names);
            if (newNo != ind)
            {
                current = others[newNo];
                return change;
            }

            return false;
        }

        public static void Space()
        {
            checkLine();
            EditorGUILayout.Separator();
            newLine();
        }

        public static bool edit<T>(ref T field) where T : UnityEngine.Object
        {
            checkLine();
            var tmp = field;
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), true);
            return tmp != field;
        }

        public static bool edit<T>(ref T field, int width) where T : UnityEngine.Object
        {
            checkLine();
            var tmp = field;
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), true, GUILayout.MaxWidth(width));
            return tmp != field;
        }

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            checkLine();
            var tmp = field;
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowDrop);
            return tmp != field;
        }
        
        public static bool edit(string label, ref float val)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.FloatField(label, val);
            return (val != before) ? change : false;
        }

        public static bool edit(ref float val)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.FloatField(val);
            return (val != before).Dirty(); 
        }
        
        public static bool edit(ref float val, int width)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(width));
            return (val != before).Dirty();
        }

        public static bool edit(ref double val, int width)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.DoubleField(val, GUILayout.MaxWidth(width));
            return (val != before).Dirty();
        }

        public static bool edit(ref double val)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.DoubleField(val);
            return (val != before).Dirty();
        }
        
        public static bool edit(ref int val, int min, int max)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.IntSlider(val, min, max); //Slider(val, min, max);
            return (val != before).Dirty();
        }

        public static bool edit(ref uint val, uint min, uint max)
        {
            checkLine();
            var before = (int)val;
            val = (uint)EditorGUILayout.IntSlider(before, (int)min, (int)max); //Slider(val, min, max);
            return (val != before).Dirty();
        }


        public static bool editPOW(ref float val, float min, float max)
        {

            checkLine();
            var before = Mathf.Sqrt(val);
            var after = EditorGUILayout.Slider(before, min, max);
            if (before != after)
            {
                val = after * after;
                return change;
            }
            return false;
        }

        public static bool edit(ref float val, float min, float max)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.Slider(val, min, max);
            return (val != before).Dirty();
        }

        public static bool edit(ref Color col)
        {

            checkLine();
            var before = col;
            col = EditorGUILayout.ColorField(col);

            return (before != col).Dirty();

        }

        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {
            checkLine();
            var before = key;

            if (editDelayed(ref key, 40))
                return dic.TryChangeKey(before, key) ? change : false;
  
            return false;
        }
        
        public static bool edit(ref Dictionary<int, string> dic, int atKey)
        {
            var before = dic[atKey];
            if (editDelayed(ref before))
            {
                dic[atKey] = before;
                return change;
            }
            return false;
        }

        public static bool edit(ref int val)
        {
            checkLine();
            var pre = val;
            val = EditorGUILayout.IntField(val);
            return (val != pre).Dirty();
        }

        public static bool edit(ref uint val)
        {
            checkLine();
            var pre = (int)val;
            val = (uint)EditorGUILayout.IntField(pre);
            return (val != pre).Dirty();
        }

        public static bool edit(ref int val, int width)
        {
            checkLine();
            var pre = val;
            val = EditorGUILayout.IntField(val, GUILayout.MaxWidth(width));
            return (val != pre).Dirty();
        }
        
        public static bool edit(ref uint val, int width)
        {
            checkLine();
            var pre = (int)val;
            val = (uint)EditorGUILayout.IntField(pre, GUILayout.MaxWidth(width));
            return (val != pre).Dirty();
        }
        
        public static bool edit(string name, ref AnimationCurve val) {

            BeginCheckLine();
            val = EditorGUILayout.CurveField(name,val);
            return EndCheckLine();
        }

        public static bool edit(string label, ref Vector4 val)
        {
            checkLine();

            var oldVal = val;
            val = EditorGUILayout.Vector2Field(label, val);

            return (oldVal != val).Dirty();
        }

        public static bool edit(string label, ref Vector2 val) {
            checkLine();

            var oldVal = val;
            val = EditorGUILayout.Vector2Field(label, val);

            return (oldVal != val).Dirty();
        }

        public static bool edit(ref Vector2 val) => edit(ref val.x) || edit(ref val.y).Dirty();
        
        public static bool edit(ref MyIntVec2 val) => edit(ref val.x) || edit(ref val.y).Dirty();
        
        public static bool edit(ref MyIntVec2 val, int min, int max) => edit(ref val.x, min, max) || edit(ref val.y, min, max).Dirty();
        
        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max) => edit(ref val.x, min, max.x) || edit(ref val.y, min, max.y).Dirty();
        

        public static bool edit(ref Vector4 val) => "X".edit(ref val.x).nl() || "Y".edit(ref val.y).nl() || "Z".edit(ref val.z).nl() || "W".edit(ref val.w).nl().Dirty();
        
        
        private static string _editedText;
        private static string _editedHash = "";
        public static bool editDelayed(ref string text)
        {
           
            if (KeyCode.Return.IsDown())
            {
                if (text.GetHashCode().ToString() == _editedHash)
                {
                    checkLine();
                    EditorGUILayout.TextField(text);
                    text = _editedText;
                    return change;
                }
            }

            var tmp = text;
            if (edit(ref tmp))
            {
                _editedText = tmp;
                _editedHash = text.GetHashCode().ToString();
                pegi.globChanged = false;
            }
            
            return false;//(String.Compare(before, text) != 0);
        }

        public static bool editDelayed(ref string text, int width)
        {
          

            if (KeyCode.Return.IsDown() && (text.GetHashCode().ToString() == _editedHash))
            {
                checkLine();
                EditorGUILayout.TextField(text);
                text = _editedText;
                return change;
            }

            var tmp = text;
            if (edit(ref tmp, width))
            {
                _editedText = tmp;
                _editedHash = text.GetHashCode().ToString();
                pegi.globChanged = false;
            }



            return false;//(String.Compare(before, text) != 0);
        }
        
        static int editedIntegerIndex;
        static int editedInteger;
        public static bool editDelayed(ref int val, int width)
        {
          

            if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
            {
                checkLine();
                EditorGUILayout.IntField(val, GUILayout.Width(width));
                val = editedInteger;
                _elementIndex++; editedIntegerIndex = -1;
                return change;
            }

            var tmp = val;
            if (edit(ref tmp))
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

            _elementIndex++;

            return false;//(String.Compare(before, text) != 0);
        }

        private static int _editedFloatIndex;
        private static float _editedFloat;
        public static bool editDelayed(ref float val, int width)
        {
           

            if (KeyCode.Return.IsDown() && (_elementIndex == _editedFloatIndex))
            {
                checkLine();
                EditorGUILayout.FloatField(val, GUILayout.Width(width));
                val = _editedFloat;
                _elementIndex++;
                _editedFloatIndex = -1;
                return change;
            }

            var tmp = val;
            if (edit(ref tmp))
            {
                _editedFloat = tmp;
                _editedFloatIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
        }

        public static bool edit(ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text);
            return EndCheckLine();
        }

        public static bool edit(ref string text, int width)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool editBig(ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextArea(text, GUILayout.MaxHeight(100));
            return EndCheckLine();
        }

        public static bool edit(ref string[] texts, int no)
        {
            BeginCheckLine();
            texts[no] = EditorGUILayout.TextField(texts[no]);
            return EndCheckLine();
        }

        public static bool edit(List<string> texts, int no)
        {
            BeginCheckLine();
            texts[no] = EditorGUILayout.TextField(texts[no]);
            return EndCheckLine();
        }

        public static bool editPowOf2(ref int i, int min, int max)
        {
            checkLine();
            var before = i;
            i = Mathf.ClosestPowerOfTwo((int)Mathf.Clamp(EditorGUILayout.IntField(i), min, max));
            return (i != before).Dirty();
        }

        public static bool toggleInt(ref int val)
        {
            checkLine();
            var before = val > 0;
            if (toggle(ref before))
            {
                val = before ? 1 : 0;
                return change;
            }
            return false;
        }

        public static bool toggle(ref bool val)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.Toggle(val, GUILayout.MaxWidth(40));
            return (before != val).Dirty();
        }

        public static bool toggle(int ind, CountlessBool tb)
        {
            var has = tb[ind];
            if (toggle(ref has))
            {
                tb.Toggle(ind);
                return true;
            }
            return false;
        }

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width, GUIStyle style = null) {
            var before = val;

            if (style == null)
                style = PEGI_Styles.ImageButton;


            if (val)
            {
                if (Click(TrueIcon, tip, width, style)) val = false;
            }
            else if (Click(FalseIcon, tip, width, style)) val = true;
            

            return (before != val);
        }

        public static bool toggle(ref bool val, string text)
        {
            checkLine();
            var before = val;
            val = EditorGUILayout.Toggle(text, val);
            return (before != val) ? change : false;
        }

        public static bool toggle(ref bool val, string text, string tip)
        {
            checkLine();

            var before = val;
            val = EditorGUILayout.Toggle(new GUIContent{ text = text,  tooltip = tip }, val);
            
            return (before != val) ? change : false;
        }
        
        public static bool Click(string txt, int width)
        {
            checkLine();
            return GUILayout.Button(txt, GUILayout.MaxWidth(width)) ? change : false;
        }

        public static bool Click(string txt)
        {
            checkLine();
            return GUILayout.Button(txt) ? change : false;
        }

        public static bool Click(string txt, string tip)
        {
            checkLine();
            return GUILayout.Button(new GUIContent { text = txt, tooltip = tip}) ? change : false;
        }
        
        public static bool Click(string txt, string tip, GUIStyle style)
        {
            checkLine();
            return GUILayout.Button( new GUIContent {  text = txt, tooltip = tip}, style) ? change : false;
        }

        public static bool Click(string txt, string tip, int width, GUIStyle style)
        {
            checkLine();

            return GUILayout.Button(new GUIContent {  text = txt, tooltip = tip}, style, GUILayout.MaxWidth(width)) ? change : false;
        }


        public static bool Click(string txt, string tip, int width)
        {
            checkLine();

            return GUILayout.Button(new GUIContent {  text = txt, tooltip = tip}, GUILayout.MaxWidth(width)) ? change : false;
        }

        public static bool Click(Texture img, int width, GUIStyle style = null)   {
            if (style == null)
                style = PEGI_Styles.ImageButton;
            checkLine();
            return GUILayout.Button(img, style, GUILayout.MaxHeight(width), GUILayout.MaxWidth(width + 10)) ? change : false;
        }

        public static bool Click(Texture img, string tip, int width, GUIStyle style = null) => Click(img, tip, width, width, style);

        public static bool Click(Texture img, string tip, int width, int height, GUIStyle style = null)
        {
            if (style == null)
                style = PEGI_Styles.ImageButton;

            checkLine();

            return GUILayout.Button(new GUIContent {   image = img, tooltip = tip}, style, GUILayout.MaxWidth(width+10), GUILayout.MaxHeight(height)).Dirty(); 
        }

        public static void write<T>(T field) where T : UnityEngine.Object
        {
            checkLine();
            EditorGUILayout.ObjectField(field, typeof(T), false);
        }

        public static void write<T>(T field, int width) where T : UnityEngine.Object
        {
            checkLine();
            EditorGUILayout.ObjectField(field, typeof(T), false, GUILayout.MaxWidth(width));
        }

        public static void write(Texture icon, int width) => write(icon, icon ? icon.name : "Null Icon", width, width);
 
        public static void write(Texture icon, string tip, int width) => write(icon, tip, width, width);
      
        public static void write(Texture icon, string tip, int width, int height)
        {

            checkLine();

            GUI.enabled = false;
            Color.clear.SetBgColor();
            GUILayout.Button(new GUIContent {    image = icon, tooltip = tip}, PEGI_Styles.ImageButton ,GUILayout.MaxWidth(width+10), GUILayout.MaxHeight(height));
            pegi.PreviousBgColor();
            GUI.enabled = true;
            
        }
        
        public static void write(string text, int width)
        {

            checkLine();

            EditorGUILayout.LabelField(text, EditorStyles.miniLabel, GUILayout.MaxWidth(width));
        }

        public static void write(string text, string tip)
        {

            checkLine();

            EditorGUILayout.LabelField(new GUIContent { text = text, tooltip = tip}, PEGI_Styles.WrappingText);
        }

        public static void write(string text, string tip, int width)
        {

            checkLine();

            EditorGUILayout.LabelField(new GUIContent { text = text, tooltip = tip}, PEGI_Styles.WrappingText, GUILayout.MaxWidth(width));
        }

        public static void write(string text) => write(text, PEGI_Styles.WrappingText);

        public static void write(string text, GUIStyle style )  {
            checkLine();
            EditorGUILayout.LabelField(text, style);
        }

        public static void write(string text, int width, GUIStyle style) {
            checkLine();
            EditorGUILayout.LabelField(text, style, GUILayout.MaxWidth(width));
        }

        public static void write(string text, string hint, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(new GUIContent(text, hint), style);
        }

        public static void write(string text, string hint, int width , GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(new GUIContent(text, hint), style, GUILayout.MaxWidth(width));
        }

        public static void writeHint(string text, MessageType type)  {
            checkLine();
            EditorGUILayout.HelpBox(text, type);
        }
  
        public static IEnumerable<T> DropAreaGUI<T>() where T : UnityEngine.Object
        {
            newLine();

            var evt = Event.current;
            var drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            
            GUILayout.Box("Drag & Drop");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        yield break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var o in DragAndDrop.objectReferences)
                        {
                            var cnvrt = o as T;
                            if (cnvrt)
                                yield return cnvrt;
                        }
                    }
                    break;
            }
        }
        
        #region Reordable List
        
        private static readonly Dictionary<IList, ReorderableList> ReorderableList = new Dictionary<IList, ReorderableList>();

        static ReorderableList GetReordable<T>(this List<T> list, ListMetaData metaDatas)
        {
            ReorderableList rl;
            ReorderableList.TryGetValue(list, out rl);

            if (rl != null) return rl;
            
            rl = new ReorderableList(list, typeof(T), metaDatas == null || metaDatas.allowReorder, true, false, metaDatas == null || metaDatas.allowDelete);
            ReorderableList.Add(list, rl);

            rl.drawHeaderCallback += DrawHeader;
            rl.drawElementCallback += DrawElement;
            rl.onRemoveCallback += RemoveItem;
            
            return rl;
        }
        
        private static IList _currentReorderedList;
        private static Type _currentReorderedType;
        private static List<Type> _currentReorderedListTypes;
        private static TaggedTypes_STD _currentTaggedTypes;
        private static ListMetaData _listMetaData;

        private static bool GetIsSelected(int ind) => (_listMetaData != null) ? _listMetaData.GetIsSelected(ind) : pegi.selectedEls[ind];

        private static void SetIsSelected(int ind, bool val) {
            if (_listMetaData != null)
                _listMetaData.SetIsSelected(ind, val);
            else
                pegi.selectedEls[ind] = val;
        }
        
        public static bool reorder_List<T>(List<T> l, ListMetaData metas)
        {
            _listMetaData = metas;
    
            EditorGUI.BeginChangeCheck();

            if (_currentReorderedList != l) {

                _currentReorderedListTypes = typeof(T).TryGetDerivedClasses();

                if (_currentReorderedListTypes == null)
                {
                    _currentTaggedTypes = typeof(T).TryGetTaggedClasses();
                    if (_currentTaggedTypes != null)
                        _currentReorderedListTypes = _currentTaggedTypes.Types;
                }
                else _currentTaggedTypes = null;

                _currentReorderedType = typeof(T);
                _currentReorderedList = l;
                if (metas == null)
                    pegi.selectedEls.Clear();

            }

            l.GetReordable(metas).DoLayoutList();
            return EditorGUI.EndChangeCheck();
        }
        
        private static void DrawHeader(Rect rect) => GUI.Label(rect, "Ordering {0} {1}s".F(_currentReorderedList.Count.ToString(), _currentReorderedType.ToPEGIstring_Type()));

        private static void DrawElement(Rect rect, int index, bool active, bool focused) {
            
            var el = _currentReorderedList[index];

            var selected = GetIsSelected(index);

            var after = EditorGUI.Toggle(new Rect(rect.x, rect.y, 30, rect.height), selected);

            if (after != selected)
                SetIsSelected(index, after);

            rect.x += 30;
            rect.width -= 30;

            if (el != null) {

                if (_currentReorderedListTypes != null) {
                    var ty = el.GetType();

                    var cont = new GUIContent {
                        tooltip = ty.ToString(),
                        text = el.ToPEGIstring()
                    };

                    var uo = el as UnityEngine.Object;
                    if (uo)
                        EditorGUI.ObjectField(rect, cont, uo, _currentReorderedType, true);
                    else
                    {
                        rect.width = 100;
                        EditorGUI.LabelField(rect, cont);
                        rect.x += 100;
                        rect.width = 100;

                        if (select_Type(ref ty, _currentReorderedListTypes, rect)) 
                            _currentReorderedList.TryChangeObjectType(index, ty, _listMetaData);
                    }
                }
                else
                {
                    rect.width = 200;
                    EditorGUI.LabelField(rect, el.ToPEGIstring());
                }
            }
            else
            {
                var ed = _listMetaData.TryGetElement(index);
                
                if (ed != null && ed.unrecognized) {

                    if (_currentTaggedTypes != null) {

                        var cont = new GUIContent {
                            tooltip = "Select New Class",
                            text = "UNREC {0}".F(ed.unrecognizedUnderTag)
                        };

                        rect.width = 100;
                        EditorGUI.LabelField(rect, cont);
                        rect.x += 100;
                        rect.width = 100;

                        Type ty = null;

                        if (select_Type(ref ty, _currentReorderedListTypes, rect)) {
                            el = Activator.CreateInstance(ty);
                            _currentReorderedList[index] = el;

                            var std = el as ISTD;

                            if (std != null) 
                                 std.Decode(ed.SetRecognized().stdDta);
                            
                        }
                    }
                    
                } else 
                EditorGUI.LabelField(rect, "Empty {0}".F(_currentReorderedType.ToPEGIstring_Type()));
            }
        }
        
        private static void RemoveItem(ReorderableList list)
        {
            var i = list.index;
            var el = _currentReorderedList[i];
            if (el != null && _currentReorderedType.IsUnityObject())
                _currentReorderedList[i] = null;
            else
                _currentReorderedList.RemoveAt(i);
        }
        
        #endregion

    }
}
#pragma warning restore IDE1006 // Naming Styles
#endif