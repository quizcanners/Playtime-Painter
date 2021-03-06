﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using QuizCanners.Utils;

using Type = System.Type;
using ReorderableList = UnityEditorInternal.ReorderableList;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
using Object = UnityEngine.Object;
#endif

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{

    internal static class ef {

        #region Non Editor Only

        public static void ResetInspectionTarget(object target)
        {
            inspectedTarget = target;
            pegi.ResetInspectedChain();
        }

        public static object inspectedTarget;

        public static bool isFoldedOutOrEntered;

        public static bool globChanged; // Some times user can change temporary fields, like delayed Edits

        internal static bool IsNextFoldedOut => _selectedFold == _elementIndex - 1;

        private static int _elementIndex;

        private static int _selectedFold = -1;

        #endregion



#if UNITY_EDITOR

        private static bool _lineOpen;
        public static Object inspectedUnityObject;
        public static SerializedObject serObj;
        private static Editor _editor;
        private static PEGI_Inspector_Material _materialEditor;
        public static Object drawDefaultInspector;

        public static void RepaintEditor() {
            if (_editor)
                _editor.Repaint();
            if (_materialEditor!= null)
                _materialEditor.unityMaterialEditor.Repaint();
        }

        /*
        public static bool DefaultInspector()
        {
            newLine();

            EditorGUI.BeginChangeCheck();

            switch (editorTypeForDefaultInspector)
            {
                case EditorType.Material: _materialEditor?.DrawDefaultInspector(); break;
                default: if (_editor != null) _editor.DrawDefaultInspector(); break;
            }

            return EditorGUI.EndChangeCheck();

        }
        */

     /*   public static bool Inspect_Prop<T>(T val, SerializedProperty prop) {
            var changed = false;
            
            start();

            var pgi = val as IPEGI;

            if (pgi != null)
                pgi.Inspect();
            else {
                var lpgi = val as IPEGI_ListInspect;
                if (lpgi != null)
                    lpgi.Inspect_AsInList();
            }

            newLine();
            
            return changed;
        }*/

        public static void Inspect_MB(Editor editor) 
        {
            _editor = editor;

            MonoBehaviour o = editor.target as MonoBehaviour;
            var so = editor.serializedObject;
            inspectedTarget = editor.target;

            var go = o.gameObject;

            var pgi = o as IPEGI;

            bool paintedPegi = false;

            if (pgi != null && !QcUnity.IsPrefab(go))
            {
                start(so);

                if (!pegi.FullWindow.ShowingPopup())
                {
                    // var isPrefab = PrefabUtility.IsPartOfAnyPrefab(o);

                    pegi.toggleDefaultInspector(o);

                    var change = pegi.ChangeTrackStart();

                    pgi.Inspect();

                    if (change)
                        ClearFromPooledSerializedObjects(o);

                    if (globChanged)// && isPrefab)
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                        // UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                        /*var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                        if (prefabStage != null)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                                prefabStage.scene);
                        }
                        else
                        {
                            UnityEditor.EditorUtility.SetDirty(o.gameObject);
                            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(o.gameObject.scene);
                        }*/
                    }
                }

                newLine();
                paintedPegi = true;
            }

            if (!paintedPegi)
            {
                EditorGUI.BeginChangeCheck();
                editor.DrawDefaultInspector();
                if (EditorGUI.EndChangeCheck())
                {
                    globChanged = true;
                }
            }

            if (globChanged)
            {
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go ? go.scene : SceneManager.GetActiveScene());

                EditorUtility.SetDirty(o);
                EditorUtility.SetDirty(go);
                Undo.RecordObject(o, "PEGI Editor modifications");
            }
        }

        public static void Inspect_SO(Editor editor) 
        {
            _editor = editor;

            var scrObj = editor.target as ScriptableObject;

            if (!scrObj)
            {
                start();

                "Target is not Scriptable Object. Check your PEGI_Inspector_OS.".writeWarning();
                
                end(editor.target);

                editor.DrawDefaultInspector();

                return;
            }

           // var o = (T)editor.target;
            var so = editor.serializedObject;
            inspectedTarget = editor.target;

            var pgi = scrObj as IPEGI;
            if (pgi != null)
            {
                if (!pegi.FullWindow.ShowingPopup())
                {
                    start(so);

                    pegi.toggleDefaultInspector(scrObj);
                    pgi.Inspect();
                    end(scrObj);
                }
                
                return;
            }

            EditorGUI.BeginChangeCheck();
            editor.DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(scrObj);
            }
            
        }

        public static bool Inspect_Material(PEGI_Inspector_Material editor) {
            
            _materialEditor = editor;

            ResetInspectionTarget(editor.unityMaterialEditor.target);

            var mat = editor.unityMaterialEditor.target as Material;

            start();

            var changed = !pegi.FullWindow.ShowingPopup() && editor.Inspect(mat);

            end(mat);

            return globChanged || changed;
        }
        
        public static bool toggleDefaultInspector(Object target)
        {
            var changed = false;

            const string BACK_TO_CUSTOM = "Back to Custom Inspector";
            const int ICON_SIZE_FOR_DEFAULT = 30;
            const int ICON_SIZE_FOR_CUSTOM = 20;

            if (target is Material)
            {
                pegi.toggle(ref PEGI_Inspector_Material.drawDefaultInspector, icon.Exit, icon.Debug,
                    "Toggle Between regular and PEGI Material inspector", PEGI_Inspector_Material.drawDefaultInspector ? ICON_SIZE_FOR_DEFAULT : ICON_SIZE_FOR_CUSTOM);

                if (PEGI_Inspector_Material.drawDefaultInspector &&
                    BACK_TO_CUSTOM.ClickLabel(style: PEGI_Styles.ExitLabel).nl())
                    PEGI_Inspector_Material.drawDefaultInspector = false;
            }
            else
            {

                if (target == inspectedUnityObject)
                {
                    bool isDefault = target == drawDefaultInspector;

                    if (pegi.toggle(ref isDefault, icon.Exit, icon.Debug,
                        "Toggle Between regular and PEGI inspector", isDefault ? ICON_SIZE_FOR_DEFAULT : ICON_SIZE_FOR_CUSTOM))
                        drawDefaultInspector = isDefault ? target : null;

                    if (isDefault && BACK_TO_CUSTOM.ClickLabel(style: PEGI_Styles.ExitLabel).nl())
                        drawDefaultInspector = null;
                }
                else
                {
                    target.ClickHighlight();
                }
            }

            return changed;
        }

        private static void start(SerializedObject so)
        {
            start();
            serObj = so;
        }

        private static void start()
        {
            _elementIndex = 0;
       
            _lineOpen = false;
            globChanged = false;
        }

        public static Object ClearFromPooledSerializedObjects(Object obj) //where T : Object
        {
            if (obj && SerializedObjects.ContainsKey(obj))
                SerializedObjects.Remove(obj);

            return obj;
        }

        private static void end(Object obj)
        {

            if (globChanged)
            {
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                ClearFromPooledSerializedObjects(obj);

                EditorUtility.SetDirty(obj);
            }
            newLine();
        }

        private static bool setDirty { get { globChanged = true; return true; } }

        private static void BeginCheckLine() { checkLine(); EditorGUI.BeginChangeCheck(); }

        private static bool EndCheckLine()
        {
            var val = EditorGUI.EndChangeCheck();
            globChanged |= val;
            return val;
        }

        public static void checkLine()
        {
            if (_lineOpen) return;

           
            EditorGUILayout.BeginHorizontal();
            _lineOpen = true;

        }

        public static void newLine()
        {
            if (!_lineOpen) return;

            _lineOpen = false;
            EditorGUILayout.EndHorizontal();
        }

        public static void Indent(int amount = 1) => EditorGUI.indentLevel+= amount;
        
        public static void UnIndent(int amount = 1) => EditorGUI.indentLevel = Mathf.Max(0, EditorGUI.indentLevel - amount);
        
        private static readonly GUIContent textAndToolTip = new GUIContent();

        private static GUIContent TextAndTip(string text, string tip)
        {
            textAndToolTip.text = text;
            textAndToolTip.tooltip = tip;
            return textAndToolTip;
        }
                
        #region Foldout
      

        private static bool StylizedFoldOut(bool foldedOut, string txt, string hint = "FoldIn/FoldOut")
        {

            BeginCheckLine();

            textAndToolTip.text = txt;
            textAndToolTip.tooltip = hint;

            foldedOut = EditorGUILayout.Foldout(foldedOut, TextAndTip(txt, hint), true);
            EndCheckLine();

            return foldedOut;
        }

        public static bool foldout(string txt, ref bool state)
        {
            state = StylizedFoldOut(state, txt);
            isFoldedOutOrEntered = state;
            return isFoldedOutOrEntered;
        }

        public static bool foldout(string txt, ref int selected, int current)
        {

            isFoldedOutOrEntered = (selected == current);

            if (StylizedFoldOut(isFoldedOutOrEntered, txt))
                selected = current;
            else
                if (isFoldedOutOrEntered) selected = -1;

            isFoldedOutOrEntered = selected == current;

            return isFoldedOutOrEntered;
        }

        public static bool foldout(string txt)
        {
            foldout(txt, ref _selectedFold, _elementIndex);

            _elementIndex++;

            return isFoldedOutOrEntered;
        }

        #endregion

        #region Select

        public static bool selectFlags(ref int no, string[] from, int width) {
            BeginCheckLine();
            no = EditorGUILayout.MaskField(no, from, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool selectFlags(ref int no, string[] from)
        {
            BeginCheckLine();
            no = EditorGUILayout.MaskField(no, from);
            return EndCheckLine();
        }

        public static bool select<T>(ref int no, List<T> lst, int width)
        {

            var listNames = new List<string>();
            var listIndexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                if (lst[j] == null) continue;

                if (no == j)
                    current = listIndexes.Count;
                listNames.Add("{0}: {1}".F(j, lst[j].GetNameForInspector()));
                listIndexes.Add(j);

            }

            if (select(ref current, listNames.ToArray(), width))
            {
                no = listIndexes[current];
                return true;
            }

            return false;

        }

        public static bool select<T>(ref int no, CountlessCfg<T> tree) where T : Migration.ICfg, new()
        {
            List<int> indexes;
            var objs = tree.GetAllObjs(out indexes);
            var filtered = new List<string>();
            var current = -1;

            for (var i = 0; i < objs.Count; i++)
            {
                if (no == indexes[i])
                    current = i;
                filtered.Add("{0}: {1}".F(i, objs[i].GetNameForInspector()));
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
                filtered.Add(objs[i].GetNameForInspector());
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
            BeginCheckLine();
            no = EditorGUILayout.Popup(no, from, GUILayout.MaxWidth(width));
            return EndCheckLine();

        }

        public static bool select(ref int no, string[] from)
        {
            BeginCheckLine();
            no = EditorGUILayout.Popup(no, from);
            return EndCheckLine();
        }

        public static bool select(ref int no, Dictionary<int, string> from)
        {
            var options = new string[from.Count];

            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            BeginCheckLine();
            var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown);
            if (!EndCheckLine()) return false;

            no = from.GetElementAt(newInd).Key;
            return true;

        }

        public static bool select(ref int no, Dictionary<int, string> from, int width)
        {
            var options = new string[from.Count];

            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }


            BeginCheckLine();
            var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(width));
            if (!EndCheckLine()) return false;

            no = from.GetElementAt(newInd).Key;
            return true;

        }

        public static bool select(ref int index, Texture[] tex)
        {
            if (tex.Length == 0) return false;

            var before = index;
            var texNames = new List<string>();
            var texIndexes = new List<int>();

            var tmpInd = 0;
            for (var i = 0; i < tex.Length; i++)
                if (tex[i])
                {
                    texIndexes.Add(i);
                    texNames.Add("{0}: {1}".F(i, tex[i].name));
                    if (index == i) tmpInd = texNames.Count - 1;
                }

            BeginCheckLine();
            tmpInd = EditorGUILayout.Popup(tmpInd, texNames.ToArray());
            if (!EndCheckLine()) return false;

            if (tmpInd >= 0 && tmpInd < texNames.Count)
                index = texIndexes[tmpInd];

            return (before != index);
        }

        private static bool select_Type(ref Type current, IReadOnlyList<Type> others, Rect rect)
        {

            var names = new string[others.Count];

            var ind = -1;

            for (var i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = el.ToPegiStringType();
                if (el != null && el == current)
                    ind = i;
            }

            BeginCheckLine();

            var newNo = EditorGUI.Popup(rect, ind, names);

            if (!EndCheckLine()) return false;

            current = others[newNo];
            return true;
        }

        private static bool select(ref Component current, IReadOnlyList<Component> others, Rect rect)
        {

            var names = new string[others.Count];

            var ind = -1;

            for (var i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = i + ": " + el.GetType().ToPegiStringType();
                if (el && el == current)
                    ind = i;
            }

            BeginCheckLine();

            var newNo = EditorGUI.Popup(rect, ind, names);

            if (!EndCheckLine()) return false;

            current = others[newNo];

            return true;
        }


        #endregion

        public static void Space()
        {
            checkLine();
            EditorGUILayout.Separator();
            newLine();
        }

        #region Edit

        #region Values

        public static bool edit_Scene(ref string path)
        {
            BeginCheckLine();

            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            var newScene = EditorGUILayout.ObjectField(oldScene, typeof(SceneAsset), allowSceneObjects: false) as SceneAsset;
            if (EndCheckLine()) 
            {
                path = AssetDatabase.GetAssetPath(newScene);
                return true;
            }
            return false;
        }

        public static bool edit_Scene(ref string path, int width)
        {
            BeginCheckLine();

            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            var newScene = EditorGUILayout.ObjectField(oldScene, typeof(SceneAsset), allowSceneObjects: false, GUILayout.MaxWidth(width)) as SceneAsset;
            if (EndCheckLine())
            {
                path = AssetDatabase.GetAssetPath(newScene);
                return true;
            }
            return false;
        }

        public static bool editTag(ref string tag)
        {
            BeginCheckLine();
            tag = EditorGUILayout.TagField(tag);
            return EndCheckLine();
        }

        public static bool editLayerMask(ref int val)
        {
            BeginCheckLine();
            val = EditorGUILayout.LayerField(val);
            return EndCheckLine();
        }

        public static bool edit(ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text);
            return EndCheckLine();
        }

        public static bool edit(string label, ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(label, text);
            return EndCheckLine();
        }

        public static bool edit(ref string text, int width)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }
/*
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
            BeginCheckLine();
            i = Mathf.ClosestPowerOfTwo(Mathf.Clamp(EditorGUILayout.IntField(i), min, max));
            return EndCheckLine();
        }
        */
        public static bool editBig(ref string text, int height = 100)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextArea(text, GUILayout.MaxHeight(height));
            return EndCheckLine();
        }
        
        public static bool edit<T>(ref T field, bool allowSceneObjects = true) where T : Object
        {
            BeginCheckLine();
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowSceneObjects);
            return EndCheckLine();
        }

        public static bool edit(ref Object field, Type type, bool allowSceneObjects = true)
        {
            BeginCheckLine();
            field = EditorGUILayout.ObjectField(field, type, allowSceneObjects);
            return EndCheckLine();
        }

        public static bool edit<T>(ref T field, Type type, int width, bool allowSceneObjects = true) where T : Object
        {
            BeginCheckLine();
            field = (T)EditorGUILayout.ObjectField(field, type, allowSceneObjects, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit<T>(ref T field, int width, bool allowSceneObjects = true) where T : Object
        {
            BeginCheckLine();
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowSceneObjects, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit(string label, ref float val)
        {
            BeginCheckLine();
            val = EditorGUILayout.FloatField(label, val);
            return EndCheckLine();
        }

        public static bool edit(ref float val)
        {
            BeginCheckLine();
            val = EditorGUILayout.FloatField(val);
            return EndCheckLine();
        }

        public static bool edit(ref float val, int width)
        {
            BeginCheckLine();
            val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit(ref double val, int width)
        {
            BeginCheckLine();
            val = EditorGUILayout.DoubleField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit(ref double val)
        {
            BeginCheckLine();
            val = EditorGUILayout.DoubleField(val);
            return EndCheckLine();
        }
        
        public static bool edit(ref int val, int minInclusive, int maxInclusive)
        {
            BeginCheckLine();
            val = EditorGUILayout.IntSlider(val, minInclusive, maxInclusive); 
            return EndCheckLine();
        }

       /* public static bool edit(ref int val, int min, int max)
        {
            BeginCheckLine();
            val = (int)EditorGUILayout.Slider((float)val, (float)min, (float)max); 
            return EndCheckLine();
        }*/

        public static bool edit(ref uint val, uint min, uint max)
        {
            BeginCheckLine();
            val = (uint)EditorGUILayout.IntSlider((int)val, (int)min, (int)max); 
            return EndCheckLine();
        }

        public static bool editPOW(ref float val, float min, float max)
        {

            checkLine();
            var before = Mathf.Sqrt(val);
            var after = EditorGUILayout.Slider(before, min, max);
            if (Mathf.Approximately(System.Math.Abs(before - after), 0)) 
                return false;
            val = after * after;
            return setDirty;
        }

        public static bool edit(ref float val, float min, float max)
        {
            BeginCheckLine();
            val = EditorGUILayout.Slider(val, min, max);
            return EndCheckLine();
        }

        public static bool edit(ref Color col)
        {

            BeginCheckLine();
            col = EditorGUILayout.ColorField(col);
            return EndCheckLine();

        }

        public static bool edit(string label, ref Vector3 vec)
        {

            BeginCheckLine();
            vec = EditorGUILayout.Vector3Field(label, vec);
            return EndCheckLine();

        }
        
        public static bool edit(ref Color col, int width)
        {

            BeginCheckLine();
            col = EditorGUILayout.ColorField(col, GUILayout.MaxWidth(width));
            return EndCheckLine();

        }
        /*
        public static bool edit(ref Color col, GUIContent cnt, int width)
        {

            BeginCheckLine();
            col = EditorGUILayout.ColorField(cnt, col, GUILayout.MaxWidth(width));
            return EndCheckLine();

        }
        */
     
        public static bool edit(ref Dictionary<int, string> dic, int atKey)
        {
            var before = dic[atKey];
            if (editDelayed(ref before))
            {
                dic[atKey] = before;
                return setDirty;
            }
            return false;
        }

        public static bool edit(ref int val)
        {
            BeginCheckLine();
            val = EditorGUILayout.IntField(val);
            return EndCheckLine();
        }

        public static bool edit(ref uint val)
        {
            BeginCheckLine();
            val = (uint)EditorGUILayout.IntField((int)val);
            return EndCheckLine();
        }

        public static bool edit(ref long val)
        {
            BeginCheckLine();
            val = EditorGUILayout.LongField(val);
            return EndCheckLine();
        }

        public static bool edit(ref int val, int width)
        {
            BeginCheckLine();
            val = EditorGUILayout.IntField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit(ref long val, int width)
        {
            BeginCheckLine();
            val = EditorGUILayout.LongField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool edit(ref uint val, int width)
        {
            BeginCheckLine();
            val = (uint)EditorGUILayout.IntField((int)val, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }
        /*
        public static bool edit(string name, ref AnimationCurve val)
        {

            BeginCheckLine();
            val = EditorGUILayout.CurveField(name, val);
            return EndCheckLine();
        }
        */
        public static bool edit(string label, ref Vector4 val)
        {
            BeginCheckLine();

            val = EditorGUILayout.Vector4Field(label, val);

            return EndCheckLine();
        }

        public static bool edit(string label, ref Vector2 val)
        {

            BeginCheckLine();
            val = EditorGUILayout.Vector2Field(label, val);
            return EndCheckLine();
        }

        public static bool edit(ref Vector2 val) => edit(ref val.x) || edit(ref val.y);

        public static bool edit(ref MyIntVec2 val) => edit(ref val.x) || edit(ref val.y);

        public static bool edit(ref MyIntVec2 val, int minInclusive, int maxInclusive) => edit(ref val.x, minInclusive: minInclusive, maxInclusive: maxInclusive) || edit(ref val.y, minInclusive: minInclusive, maxInclusive: maxInclusive);

        public static bool edit(ref MyIntVec2 val, int minInclusive, MyIntVec2 maxInclusive) => edit(ref val.x, minInclusive: minInclusive, maxInclusive: maxInclusive.x) || edit(ref val.y, minInclusive: minInclusive, maxInclusive: maxInclusive.y);

        public static bool edit(ref Vector4 val) => "X".edit(ref val.x).nl() || "Y".edit(ref val.y).nl() || "Z".edit(ref val.z).nl() || "W".edit(ref val.w).nl();
        #endregion

        #region Delayed

        // private static string _editedText;
        // private static string _editedHash = "";
        public static bool editDelayed(ref string text)
        {

            BeginCheckLine();
            text = EditorGUILayout.DelayedTextField(text);
            return EndCheckLine();

            /*  if (KeyCode.Return.IsDown())
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
              if (edit(ref tmp).ignoreChanges())
              {
                  _editedText = tmp;
                  _editedHash = text.GetHashCode().ToString();
              }

              return false;//(String.Compare(before, text) != 0);*/
        }

        public static bool editDelayed(ref string text, int width)
        {

            BeginCheckLine();
            text = EditorGUILayout.DelayedTextField(text, GUILayout.MaxWidth(width));
            return EndCheckLine();

            /*
            if (KeyCode.Return.IsDown() && (text.GetHashCode().ToString() == _editedHash))
            {
                checkLine();
                EditorGUILayout.TextField(text);
                text = _editedText;
                return change;
            }

            var tmp = text;
            if (edit(ref tmp, width).ignoreChanges())
            {
                _editedText = tmp;
                _editedHash = text.GetHashCode().ToString();
            }



            return false;//(String.Compare(before, text) != 0);*/
        }


      /*  public static bool editDelayed(ref int val)
        {

            BeginCheckLine();
            val = EditorGUILayout.DelayedIntField(val);
            return EndCheckLine();
        }*/

        // static int editedIntegerIndex;
        // static int editedInteger;
        public static bool editDelayed(ref int val, int width)
        {

            BeginCheckLine();

            if (width > 0)
                val = EditorGUILayout.DelayedIntField(val, GUILayout.MaxWidth(width));
            else
                val = EditorGUILayout.DelayedIntField(val);

            return EndCheckLine();

            /* if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
             {
                 checkLine();
                 EditorGUILayout.IntField(val, GUILayout.Width(width));
                 val = editedInteger;
                 _elementIndex++; editedIntegerIndex = -1;
                 return change;
             }

             var tmp = val;
             if (edit(ref tmp).ignoreChanges())
             {
                 editedInteger = tmp;
                 editedIntegerIndex = _elementIndex;
             }

             _elementIndex++;

             return false;*/
        }

        //private static int _editedFloatIndex;
        //private static float _editedFloat;

        public static bool editDelayed(ref float val)
        {

            BeginCheckLine();
            val = EditorGUILayout.DelayedFloatField(val);
            return EndCheckLine();
        }

        public static bool editDelayed(ref float val, int width)
        {

            BeginCheckLine();
            val = EditorGUILayout.DelayedFloatField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();

            /* if (KeyCode.Return.IsDown() && (_elementIndex == _editedFloatIndex))
             {
                 checkLine();
                 EditorGUILayout.FloatField(val, GUILayout.Width(width));
                 val = _editedFloat;
                 _elementIndex++;
                 _editedFloatIndex = -1;
                 return change;
             }

             var tmp = val;
             if (edit(ref tmp, width).ignoreChanges())
             {
                 _editedFloat = tmp;
                 _editedFloatIndex = _elementIndex;
             }

             _elementIndex++;

             return false;*/
        }

        public static bool editDelayed(ref double val)
        {

            BeginCheckLine();
            val = EditorGUILayout.DelayedDoubleField(val);
            return EndCheckLine();
        }

        public static bool editDelayed(ref double val, int width)
        {

            BeginCheckLine();
            val = EditorGUILayout.DelayedDoubleField(val, GUILayout.MaxWidth(width));
            return EndCheckLine();


        }


        #endregion

        #region Property

        public static bool edit_Property<T>(int width, System.Linq.Expressions.Expression<System.Func<T>> memberExpression, Object obj, bool includeChildren)
        {
            var serializedObject = (!obj ? serObj : GetSerObj(obj));

            if (serializedObject == null) return false;

            var member = ((System.Linq.Expressions.MemberExpression)memberExpression.Body).Member;

            string name;

            switch (member.MemberType) 
            {
                case System.Reflection.MemberTypes.Field: name = member.Name; break;
                case System.Reflection.MemberTypes.Property: name = "m_{0}{1}".F(char.ToUpper(member.Name[0]), member.Name.Substring(1)); break;
                default: QcLog.CaseNotImplemented(member.MemberType, context: "Get Serialized Property").write(); return false;
            }
            
            var tps = serializedObject.FindProperty(name);

            if (tps == null)
            {
                "{0} not found".F(name).write();
                return false;
            }

            EditorGUI.BeginChangeCheck();

            if (width < 1)
                EditorGUILayout.PropertyField(tps, GUIContent.none, includeChildren);
            else
                EditorGUILayout.PropertyField(tps, GUIContent.none, includeChildren, GUILayout.MaxWidth(width));

            if (!EditorGUI.EndChangeCheck()) return false;

            serializedObject.ApplyModifiedProperties();
            
            return setDirty;
        }

        private static readonly Dictionary<Object, SerializedObject> SerializedObjects = new Dictionary<Object, SerializedObject>();

        private static SerializedObject GetSerObj(Object obj)
        {
            SerializedObject so;

            if (SerializedObjects.TryGetValue(obj, out so))
                return so;

            so = new SerializedObject(obj);

            if (SerializedObjects.Count > 8)
                SerializedObjects.Clear();

            SerializedObjects.Add(obj, so);

            return so;

        }
        #endregion

        #endregion

        #region Toggle
        public static bool toggleInt(ref int val)
        {
            checkLine();
            var before = val > 0;
            if (toggle(ref before))
            {
                val = before ? 1 : 0;
                return setDirty;
            }
            return false;
        }

        public static bool toggle(ref bool val)
        {
            BeginCheckLine();
            val = EditorGUILayout.Toggle(val, GUILayout.MaxWidth(40));
            return EndCheckLine();
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

        public static bool toggle(ref bool val, GUIContent cnt)
        {
            BeginCheckLine();
            val = EditorGUILayout.Toggle(cnt, val);
            return EndCheckLine();
        }

        #endregion

        #region Click
        public static bool Click(string label)
        {
            checkLine();
            return GUILayout.Button(label) && setDirty;
        }
        /*
        public static bool Click(string label, GUIStyle style)
        {
            checkLine();
            return GUILayout.Button(label, style) && setDirty;
        }
        */
        public static bool Click(GUIContent content)
        {
            checkLine();
            return GUILayout.Button(content) && setDirty;
        }

        public static bool Click(GUIContent content, GUIStyle style)
        {
            checkLine();
            return GUILayout.Button(content, style) && setDirty;
        }

        public static bool Click(GUIContent content, int width, GUIStyle style)
        {
            checkLine();
            return GUILayout.Button(content, style, GUILayout.MaxWidth(width)) && setDirty;
        }

        public static bool Click(Texture image, int width, GUIStyle style = null)
        {
            if (style == null)
                style = PEGI_Styles.ImageButton.Current;

            checkLine();
            return GUILayout.Button(image, style, GUILayout.MaxHeight(width), GUILayout.MaxWidth(width + 10)) && setDirty;
        }

        public static bool ClickImage(GUIContent cnt, int width, GUIStyle style = null) => ClickImage(cnt, width, width, style);

        public static bool ClickImage(GUIContent cnt, int width, int height, GUIStyle style = null)
        {
            if (style == null)
                style = PEGI_Styles.ImageButton.Current;

            checkLine();

            return GUILayout.Button(cnt, style, GUILayout.MaxWidth(width + 10), GUILayout.MaxHeight(height)) && setDirty;
        }

        #endregion

        #region write

        private static readonly GUIContent imageAndTip = new GUIContent();
/*
        private static GUIContent ImageAndTip(Texture tex) => ImageAndTip(tex, tex.GetNameForInspector_Uobj());
       */
        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

        public static void write<T>(T field) where T : Object
        {
            checkLine();
            EditorGUILayout.ObjectField(field, typeof(T), false);
        }

      /*  
        public static void write(GUIContent cnt, int width, int height)
        {

            checkLine();
            GUI.enabled = false;
            pegi.SetBgColor(Color.clear);
            GUILayout.Button(cnt, PEGI_Styles.ImageButton.Current, GUILayout.MaxWidth(width + 10), GUILayout.MaxHeight(height));
            pegi.PreviousBgColor();
            GUI.enabled = true;

        }*/

        public static void write(string text, int width) {
            checkLine();
            EditorGUILayout.LabelField(text, EditorStyles.miniLabel, GUILayout.MaxWidth(width));
        }

        public static void write(GUIContent cnt)
        {
            checkLine();
            EditorGUILayout.LabelField(cnt, PEGI_Styles.ClippingText.Current);
        }

        public static void draw(Texture tex, string tip, int width, int height)
        {
            checkLine();
            var cnt = ImageAndTip(tex, tip);
            GUI.enabled = false;
            pegi.SetBgColor(Color.clear);
            GUILayout.Button(cnt, PEGI_Styles.ImageButton.Current, GUILayout.MaxWidth(width + 10), GUILayout.MaxHeight(height));
            pegi.SetPreviousBgColor();
            GUI.enabled = true;
        }

        public static void draw(Texture tex, int width, bool alphaBlend)
        {
            checkLine();

            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(width), GUILayout.MaxHeight(width));

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, alphaBlend: alphaBlend);
        }
        /*
        public static void write(Texture tex, int width, int height, bool alphaBlend = true)
        {
            checkLine();

            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));

            rect.width = width;
            rect.height = height;

            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, alphaBlend: alphaBlend);
        }
        */
        public static void write(GUIContent cnt, int width)
        {
            checkLine();
            EditorGUILayout.LabelField(cnt, PEGI_Styles.ClippingText.Current, GUILayout.MaxWidth(width));
        }
        /*
        public static void write(string text)
        {
            checkLine();
            EditorGUILayout.LabelField(text, PEGI_Styles.ClippingText.Current);
        }
        */
        public static void write_ForCopy(string text)
        {
            checkLine();
            EditorGUILayout.SelectableLabel(text, PEGI_Styles.ClippingText.Current);
        }

        /*
        public static void write(string text, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(text, style);
        }

        public static void write(string text, int width, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(text, style, GUILayout.MaxWidth(width));
        }
        */
        public static void write(GUIContent cnt, int width, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(cnt, style, GUILayout.MaxWidth(width));
        }

        public static void write(GUIContent cnt, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(cnt, style);
        }

        public static void writeHint(string text, MessageType type)
        {
            checkLine();
            EditorGUILayout.HelpBox(text, type);
        }
        #endregion

        private static bool searchInChildren;

        public static IEnumerable<T> DropAreaGUI<T>() where T : Object
        {
            newLine();

            var evt = Event.current;
            var drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));

            bool isComponent = typeof(Component).IsAssignableFrom(typeof(T));

            if (isComponent) {
                GUILayout.Box("Drag & Drop area for Game Object with {0} is above".F(pegi.CurrentListLabel<T>()));
                "Search in children".toggle(120, ref searchInChildren).nl();
            }
            else
                GUILayout.Box("Drag & Drop area for {0} is above".F(pegi.CurrentListLabel<T>()));

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        yield break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform) {

                        DragAndDrop.AcceptDrag();

                        foreach (var o in DragAndDrop.objectReferences) {
                            var cnvrt = o as T;
                            if (cnvrt)
                                yield return cnvrt;
                            else {
                                var go = o as GameObject;

                                if (!go) continue;
                                foreach (var c in (searchInChildren
                                    ? go.GetComponentsInChildren(typeof(T))
                                    : go.GetComponents(typeof(T)))) {

                                    yield return c as T;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        #region Reordable List

        private static readonly Dictionary<System.Collections.IList, ReorderableList> ReorderableList = new Dictionary<System.Collections.IList, ReorderableList>();

        private static ReorderableList GetReordable<T>(this List<T> list, CollectionMetaData metaDatas)
        {
            ReorderableList rl;
            ReorderableList.TryGetValue(list, out rl);

            if (rl != null) return rl;

            rl = new ReorderableList(list, typeof(T), metaDatas == null || metaDatas[CollectionInspectParams.allowReordering], true, false, false);//metaDatas == null || metaDatas.allowDelete);
            ReorderableList.Add(list, rl);

            rl.drawHeaderCallback += DrawHeader;
            rl.drawElementCallback += DrawElement;
            //rl.onRemoveCallback += RemoveItem;

            return rl;
        }

        private static System.Collections.IList _currentReorderedList;
        private static Type _currentReorderedType;
        private static List<Type> _currentReorderedListTypes;
        private static CollectionMetaData _listMetaData;
        private static Migration.TaggedTypesCfg _currentTaggedTypes;


        private static bool GetIsSelected(int ind) => (_listMetaData != null) 
            ? _listMetaData.GetIsSelected(ind) 
            : pegi.collectionInspector.selectedEls[ind]; //IsSelectedCollectionElement(ind);

        private static void SetIsSelected(int ind, bool val)
        {
            if (_listMetaData != null)
                _listMetaData.SetIsSelected(ind, val);
            else
                pegi.collectionInspector.selectedEls[ind] = val; //SetSelected(ind, val);
        }

        public static bool reorder_List<T>(List<T> l, CollectionMetaData metas)
        {
            _listMetaData = metas;

            EditorGUI.BeginChangeCheck();

            if (_currentReorderedList != l)
            {

                var type = typeof(T);

                _currentReorderedListTypes = Migration.ICfgExtensions.TryGetDerivedClasses(type);

                if (_currentReorderedListTypes == null)
                {
                    _currentTaggedTypes = Migration.TaggedTypesCfg.TryGetOrCreate(type); //typeof(T).TryGetTaggedClasses();
                    if (_currentTaggedTypes != null)
                        _currentReorderedListTypes = _currentTaggedTypes.Types;
                }
                else _currentTaggedTypes = null;

                _currentReorderedType = type;
                _currentReorderedList = l;
                if (metas == null)
                   pegi.collectionInspector.selectedEls.Clear();

            }

            l.GetReordable(metas).DoLayoutList();
            return EditorGUI.EndChangeCheck();
        }

        private static void DrawHeader(Rect rect) => GUI.Label(rect, "Ordering {0} {1}s".F(_currentReorderedList.Count.ToString(), _currentReorderedType.ToPegiStringType()));

        private static void DrawElement(Rect rect, int index, bool active, bool focused)
        {

            var el = _currentReorderedList[index];

            var selected = GetIsSelected(index);

            var after = EditorGUI.Toggle(new Rect(rect.x, rect.y, 30, rect.height), selected);

            if (after != selected)
                SetIsSelected(index, after);

            rect.x += 30;
            rect.width -= 30;

            if (el != null)
            {

                var ty = el.GetType();

                bool exactType = ty == _currentReorderedType;

                textAndToolTip.text = "{0} {1}".F(exactType ? "" : (ty.ToPegiStringType() + ":" ), el.GetNameForInspector());
                textAndToolTip.tooltip = el.ToString();

                var uo = el as Object;
                if (uo)
                {
                    var cmp = uo as Component;
                    var go = cmp ? cmp.gameObject : uo as GameObject;

                    if (!go)
                        EditorGUI.ObjectField(rect, textAndToolTip, uo, _currentReorderedType, true);
                    else
                    {
                        var mbs = go.GetComponents<Component>();

                        if (mbs.Length > 1)
                        {
                            rect.width = 100;
                            EditorGUI.LabelField(rect, textAndToolTip);
                            rect.x += 100;

                            if (select(ref cmp, mbs, rect))
                                _currentReorderedList[index] = cmp;
                        }
                        else
                            EditorGUI.ObjectField(rect, textAndToolTip, uo, _currentReorderedType, true);
                    }
                }
                else
                {
                    if (_currentReorderedListTypes != null)
                    {

                        textAndToolTip.text = el.GetNameForInspector();

                        rect.width = 100;
                        EditorGUI.LabelField(rect, textAndToolTip);
                        rect.x += 100;
                        rect.width = 100;

                        if (select_Type(ref ty, _currentReorderedListTypes, rect))
                            Migration.TaggedTypes.TryChangeObjectType(_currentReorderedList, index, ty, _listMetaData);
                    }
                    else
                        EditorGUI.LabelField(rect, textAndToolTip);
                }
            }
            else
            {
               // var ed = 
                    _listMetaData.TryGetElement(index);
                
               /* if (ed != null && ed.unrecognized)
                {

                    if (_currentTaggedTypes != null)
                    {

                        rect.width = 100;
                        EditorGUI.LabelField(rect, TextAndTip("UNREC {0}".F(ed.unrecognizedUnderTag), "Select New Class"));
                        rect.x += 100;
                        rect.width = 100;

                        Type ty = null;

                        if (select_Type(ref ty, _currentReorderedListTypes, rect))
                        {
                            el = Activator.CreateInstance(ty);
                            _currentReorderedList[index] = el;

                            var std = el as ICfg;

                            if (std != null)
                                std.Decode(ed.SetRecognized().stdDta);

                        }
                    }

                }
                else*/
                    EditorGUI.LabelField(rect, "Empty {0}".F(_currentReorderedType.ToPegiStringType()));
            }
        }

        /*private static void RemoveItem(ReorderableList list)
        {
            var i = list.index;
            var el = _currentReorderedList[i];
            if (el != null && _currentReorderedType.IsUnityObject())
                _currentReorderedList[i] = null;
            else
                _currentReorderedList.RemoveAt(i);
        }*/

        #endregion
        
#endif

    }


}


#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration
