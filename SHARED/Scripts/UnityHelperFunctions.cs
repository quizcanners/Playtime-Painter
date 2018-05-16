

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public static class UnityHelperFunctions {

        public static void RepaintViews()
        {
#if UNITY_EDITOR
//            if (SceneView.lastActiveSceneView != null)
  //              SceneView.lastActiveSceneView.Repaint();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }
        
        public static void SetToDirty(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void UpdatePrefab(this GameObject gameObject)
        {
#if UNITY_EDITOR
            var pf = PrefabUtility.GetPrefabObject(gameObject);
            if (pf != null)
                PrefabUtility.ReplacePrefab(gameObject, PrefabUtility.GetPrefabParent(gameObject), ReplacePrefabOptions.ConnectToPrefab);
#endif
        }

        public static float Angle(this Vector2 vec)
        {
            if (vec.x < 0)
            {
                return 360 - (Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg;
            }
        }

        public static Vector3 OnSpherePosition(this Vector3 vec)
        {

            var v3 = new Vector3(
                UnityEngine.Random.Range(-10f, 10f),
                  UnityEngine.Random.Range(-10f, 10f),
                  UnityEngine.Random.Range(-10f, 10f)
                );

            v3.Normalize();
            v3.Scale(vec);

            return v3;
        }

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos)
        {
            Vector3 ray = from - vpos;
            return Physics.Raycast(new Ray(vpos, ray), ray.magnitude);
        }

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos, float safeGap)
        {
            Vector3 ray = vpos - from;

            float magnitude = ray.magnitude - safeGap;

            if (magnitude < 0) return false;

            return Physics.Raycast(new Ray(from

                , ray), magnitude);
        }

        public static string GetUniqueName<T>(this string s, List<T> list)
        {

            bool match = true;
            int index = 1;
            string mod = s;


            while (match)
            {
                match = false;

                foreach (var l in list)
                    if (l.ToString().SameAs(mod))
                    {
                        match = true;
                        break;
                    }

                if (match)
                {
                    mod = s + index.ToString();
                    index++;
                }
            }

            return mod;
        }

        public static bool IsNaN(this Vector3 q)
        {
            return float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z);
        }

        public static bool isNaN(this float f)
        {
            return float.IsNaN(f);
        }

        public static GameObject SetFlagsOnItAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags = flags;
                child.gameObject.AddFlagsOnItAndChildren(flags);
            }

            return go;
        }

        public static GameObject AddFlagsOnItAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags |= flags;
                child.gameObject.AddFlagsOnItAndChildren(flags);
            }

            return go;
        }

        public static Transform Clear(this Transform transform)
        {


            if (Application.isPlaying)
            {
                foreach (Transform child in transform)
                {
                    Debug.Log("Destroying " + child.name);
                    GameObject.Destroy(child.gameObject);
                }
            }
            return transform;
        }

        public static string ToStringShort(this Vector3 v)
        {
            StringBuilder sb = new StringBuilder();

            if (v.x != 0) sb.Append("x:" + ((int)v.x));
            if (v.y != 0) sb.Append(" y:" + ((int)v.y));
            if (v.z != 0) sb.Append(" z:" + ((int)v.z));

            return sb.ToString();
        }

        public static void LineTo(this Vector3 v3a, Vector3 v3b, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawLine(v3a, v3b);
        }

        public static bool SameAs(this string s, string other)
        {
            return (String.Compare(s, other) == 0);
        }

        public static bool SearchCompare(this string search, string name)
        {
            if ((search.Length == 0) || Regex.IsMatch(name, search, RegexOptions.IgnoreCase)) return true;

            if (search.Contains(" "))
            {
                string[] sgmnts = search.Split(' ');
                for (int i = 0; i < sgmnts.Length; i++)
                    if (!Regex.IsMatch(name, sgmnts[i], RegexOptions.IgnoreCase)) return false;

                return true;
            }
            return false;
        }

        public static T ForceComponent<T>(this GameObject go, ref T co) where T : Component
        {
            if (co == null)
            {
                co = go.GetComponent<T>();
                if (co == null)
                    co = go.AddComponent<T>();
            }

            return co;
        }

        public static string AddPreSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[0] == '/')) ? s : "/" + s;
        }

        public static string AddPostSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[s.Length - 1] == '/')) ? s : s + "/";
        }

        public static void DestroyWhatever(this UnityEngine.Object go)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);
        }

        public static Vector2 To01Space(this Vector2 v2)
        {
            return (v2 - new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y)));
        }

        public static Vector2 Floor(this Vector2 v2)
        {
            return new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y));
        }

        public static Vector4 ToVector4(this Color col)
        {
            return new Vector4(col.r, col.g, col.b, col.a);
        }
#if UNITY_EDITOR
        public static void DuplicateResource(string assetFolder, string insideAssetFolder, string oldName, string newName)
        {
            string path = "Assets" + assetFolder.AddPreSlashIfNotEmpty() + "/Resources" + insideAssetFolder.AddPreSlashIfNotEmpty() + "/";
            AssetDatabase.CopyAsset(path + oldName + ResourceSaver.fileType, path + newName + ResourceSaver.fileType);
        }
#endif

        public static void DeleteResource(string assetFolder, string insideAssetFolderAndName)
        {



#if UNITY_EDITOR
            try
            {
                string path = "Assets" + assetFolder.AddPreSlashIfNotEmpty() + "/Resources/" + insideAssetFolderAndName + ResourceSaver.fileType;
                //Debug.Log("Deleting " +path);
                //Application.dataPath + "/"+assetFolder + "/Resources/" + insideAssetFolderAndName);
                AssetDatabase.DeleteAsset(path);
            }
            catch (Exception e)
            {
                Debug.Log("Oh No " + e.ToString());
            }
#endif
        }

        public static int TotalCount(this List<int>[] lists)
        {
            int total = 0;

            foreach (var e in lists)
                total += e.Count;

            return total;
        }

        public static void AddResourceIfNew(this List<string> l, string assetFolder, string insideAssetsFolder)
        {

#if UNITY_EDITOR

            try
            {
                string path = Application.dataPath + "/" + assetFolder
                                                                     + "/Resources" + insideAssetsFolder.AddPreSlashIfNotEmpty();

                if (!Directory.Exists(path)) return;

                DirectoryInfo dirInfo = new DirectoryInfo(path);

                if (dirInfo == null) return;

                FileInfo[] fileInfo = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                l = new List<string>();

                foreach (FileInfo file in fileInfo)
                {
                    string name = file.Name.Substring(0, file.Name.Length - ResourceSaver.fileType.Length);
                    if ((file.Extension == ResourceSaver.fileType) && (!l.Contains(name)))
                    {
                        l.Add(name);
                    }
                }

            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.ToString());
            }

#endif
        }

        public static List<string> getTextures(this Material m)
        {
            List<string> tnames = new List<string>();
#if UNITY_EDITOR
            if (m == null) return tnames;

            Material[] mat = new Material[1];
            mat[0] = m;
            MaterialProperty[] props = null;

            try
            {
                props = MaterialEditor.GetMaterialProperties(mat);
            }
            catch
            {
                return tnames = new List<string>();
            }


            if (props != null)
                foreach (MaterialProperty p in props)
                    if (p.type == MaterialProperty.PropType.Texture)
                        tnames.Add(p.name);
#endif
            return tnames;
        }

        public static bool DisplayNameContains(this Material m, string propertyName, string tag)
        {
            /*
            #if UNITY_EDITOR
                    try
                    {
                        var p = MaterialEditor.GetMaterialProperty(new Material[] { m }, propertyName);
                        if (p!= null) 
                            return p.displayName.Contains(tag);

                    } catch (Exception ex) {
                        Debug.Log("Materail "+m.name +" has no "+ propertyName+ " "+ex.ToString());
                    }
            #endif
            */
            return propertyName.Contains(tag);
        }

        public static bool isPrefab(this GameObject go)
        {
#if UNITY_EDITOR

            return PrefabUtility.GetPrefabObject(go) != null && PrefabUtility.GetPrefabParent(go) == null; // Is a prefab
#else
        return false;
#endif

        }

        public static void SetActiveTo(this GameObject go, bool setTo)
        {
            if (go.activeSelf != setTo)
                go.SetActive(setTo);
        }

        public static void EnabledUpdate(this Renderer c, bool setTo)
        {
            //There were some update when enabled state is changed
            if (c != null && c.enabled != setTo)
                c.enabled = setTo;
        }

        public static bool ApplicationIsAboutToEnterPlayMode(this MonoBehaviour mb)
        {
#if UNITY_EDITOR
            return (((EditorApplication.isPlayingOrWillChangePlaymode) && (Application.isPlaying == false)));
#else
        return false;
#endif
        }

        public static bool isFocused(this GameObject go) {

#if UNITY_EDITOR
            UnityEngine.Object[] tmp = Selection.objects;
            if ((tmp == null) || (tmp.Length == 0))
                return false;
            return (tmp[0].GetType() == typeof(GameObject)) && ((GameObject)tmp[0] == go);
#else
        return false;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Return -1 if no numeric key was pressed</returns>
        public static int NumericKeyDown(this Event e)
        {

            if ((Application.isPlaying) && (!Input.anyKeyDown)) return -1;

            if ((!Application.isPlaying) && (e.type != EventType.KeyDown)) return -1;

            if (KeyCode.Alpha0.isDown()) return 0;
            if (KeyCode.Alpha1.isDown()) return 1;
            if (KeyCode.Alpha2.isDown()) return 2;
            if (KeyCode.Alpha3.isDown()) return 3;
            if (KeyCode.Alpha4.isDown()) return 4;
            if (KeyCode.Alpha5.isDown()) return 5;
            if (KeyCode.Alpha6.isDown()) return 6;
            if (KeyCode.Alpha7.isDown()) return 7;
            if (KeyCode.Alpha8.isDown()) return 8;
            if (KeyCode.Alpha9.isDown()) return 9;

            return -1;
        }

        public static bool isDown(this KeyCode k)
        {
            bool down = false;
#if UNITY_EDITOR
            down |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                down |= Input.GetKeyDown(k);

            return down;
        }

        public static bool isUp(this KeyCode k)
        {

            bool up = false;
#if UNITY_EDITOR
            up |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyUp && Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                up |= Input.GetKeyUp(k);

            return up;
        }

        public static void Focus(this GameObject go)
        {
#if UNITY_EDITOR
            GameObject[] tmp = new GameObject[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }

        public static void FocusOn(GameObject go)
        {
#if UNITY_EDITOR
            GameObject[] tmp = new GameObject[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }

        public static void CopyFrom(this Texture2D tex, RenderTexture rt)
        {

            RenderTexture curRT = RenderTexture.active;

            RenderTexture.active = rt;

            if (RenderTexture.active == null)
            {
                Debug.Log("Active is null");
                return;
            }

            if (tex == null)
            {
                Debug.Log("Texture is null");
                return;
            }

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = curRT;

        }


        public static string SetUniqueObjectName(this UnityEngine.Object obj, string folderName, string extension)
        {

            folderName = "Assets" + folderName.AddPreSlashIfNotEmpty();
            string name = obj.name;
            string fullpath =
#if UNITY_EDITOR

                AssetDatabase.GenerateUniqueAssetPath(folderName + "/" + name + extension);
#else
            folderName + "/" + name + extension;
#endif
            name = fullpath.Substring(folderName.Length);
            name = name.Substring(0, name.Length - extension.Length);
            obj.name = name;

            return fullpath;
        }


        public static bool SavedAsAsset(this UnityEngine.Object go)
        {
#if UNITY_EDITOR
            return (!String.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)));
#else
        return true;
#endif

        }

#if UNITY_EDITOR
        public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName)
        {
            return CreatePngSameDirectory(diffuse, newName, diffuse.width, diffuse.height);
        }

        public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName, int width, int height)
        {

            Texture2D Result = new Texture2D(width, height, TextureFormat.RGBA32, true, false);

            diffuse.Reimport_IfNotReadale();

            var pxls = diffuse.GetPixels(width, height);
            pxls[0].a = 0.5f;

            Result.SetPixels(pxls);

            byte[] bytes = Result.EncodeToPNG();

            string dest = AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");

            var extension = dest.Substring(dest.LastIndexOf(".") + 1);

            dest = dest.Substring(0, dest.Length - extension.Length) + "png";

            dest = dest.ReplaceLastOccurrence(diffuse.name, newName);

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();

            var tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            var imp = tex.getTextureImporter();
            bool needReimport = imp.wasNotReadable();
            needReimport |= imp.wasClamped();
            needReimport |= imp.wasWrongIsColor(diffuse.isColorTexturee());
            if (needReimport)
                imp.SaveAndReimport();

            return tex;

        }

        public static void saveTexture(this Texture2D tex)
        {

            byte[] bytes = tex.EncodeToPNG();
            //Debug.Log("Format " + tex.format); 

            string dest = AssetDatabase.GetAssetPath(tex).Replace("Assets", "");

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();
        }

        public static string GetAssetPath(this Texture2D tex)
        {
            return AssetDatabase.GetAssetPath(tex);
        }

        public static string GetPathWithout_Assets_Word(this Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (String.IsNullOrEmpty(path)) return null;
            return path.Replace("Assets", "");
        }

        public static Texture2D rewriteOriginalTexture_NewName(this Texture2D tex, string name)
        {
            if (name == tex.name)
                return tex.rewriteOriginalTexture();

            //Debug.Log("Rewriting original texture");

            byte[] bytes = tex.EncodeToPNG();

            string dest = tex.GetPathWithout_Assets_Word();
            dest = dest.ReplaceLastOccurrence(tex.name, name);
            if (String.IsNullOrEmpty(dest)) return tex;

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            //Debug.Log ("Writing to "+dest);
            //AssetDatabase.Refresh();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            result.ReimportToMatchImportConfigOf(tex);

            AssetDatabase.DeleteAsset(tex.GetAssetPath());

            AssetDatabase.Refresh();
            return result;
        }

        public static Texture2D rewriteOriginalTexture(this Texture2D tex)
        {
            //Debug.Log("Rewriting original texture");

            byte[] bytes = tex.EncodeToPNG();

            string dest = tex.GetPathWithout_Assets_Word();
            if (String.IsNullOrEmpty(dest)) return tex;

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);
            //AssetDatabase.Refresh();

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            result.ReimportToMatchImportConfigOf(tex);

            return result;
        }

        public static Texture2D saveTextureAsAsset(this Texture2D tex, string folderName, ref string textureName, bool saveAsNew)
        {

            byte[] bytes = tex.EncodeToPNG();

            string lastPart = folderName.AddPreSlashIfNotEmpty() + "/";
            string folderPath = Application.dataPath + lastPart;
            Directory.CreateDirectory(folderPath);

            string fileName = textureName + ".png";

            string relativePath = "Assets" + lastPart + fileName;

            if (saveAsNew)
                relativePath = AssetDatabase.GenerateUniqueAssetPath(relativePath);

            string fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + relativePath;

            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);
            //AssetDatabase.Refresh(); 

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

            textureName = result.name;

            result.ReimportToMatchImportConfigOf(tex);

            return result;
        }

        public static GameObject getFocused()
        {

            UnityEngine.Object[] tmp = Selection.objects;
            return (((tmp != null) && (tmp.Length > 0)) ? (GameObject)tmp[0] : null);

        }

        public static void focusOnGame()
        {

            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType("UnityEditor.GameView");
            EditorWindow gameview = EditorWindow.GetWindow(type);
            gameview.Focus();


        }

        public static void RenamingLayer(int index, string name)
        {
            if (Application.isPlaying) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                return;
            }


            SerializedProperty layerSP = layers.GetArrayElementAtIndex(index);
            if ((layerSP.stringValue != name) && ((layerSP.stringValue == null) || (layerSP.stringValue.Length == 0)))
            {
                Debug.Log("Changing layer name.  " + layerSP.stringValue + " to " + name);
                layerSP.stringValue = name;
            }


            tagManager.ApplyModifiedProperties();
        }

#endif

        public static void setSplashPrototypeTexture(this Terrain terrain, Texture2D tex, int index)
        {

            if (terrain == null) return;

            SplatPrototype[] newProtos = terrain.GetCopyOfSplashPrototypes();

            if (newProtos.Length <= index)
            {
                ArrayManager<SplatPrototype> arrman = new ArrayManager<SplatPrototype>();
                arrman.AddAndInit(ref newProtos, index + 1 - newProtos.Length);
            }

            newProtos[index].texture = tex;

            terrain.terrainData.splatPrototypes = newProtos;

        }

        public static Texture getSplashPrototypeTexture(this Terrain terrain, int ind)
        {

            SplatPrototype[] prots = terrain.terrainData.splatPrototypes;

            if (prots.Length <= ind) return null;


            return prots[ind].texture;

        }

        public static Color[] GetPixels(this Texture2D tex, int width, int height)
        {

            if ((tex.width == width) && (tex.height == height))
                return tex.GetPixels();

            Color[] dst = new Color[width * height];

            Color[] src = tex.GetPixels();

            float dX = (float)tex.width / (float)width;
            float dY = (float)tex.height / (float)height;

            for (int y = 0; y < height; y++)
            {
                int dstIndex = y * width;
                int srcIndex = ((int)(y * dY)) * tex.width;
                for (int x = 0; x < width; x++)
                    dst[dstIndex + x] = src[srcIndex + (int)(x * dX)];

            }


            return dst;
        }

        public static SplatPrototype[] GetCopyOfSplashPrototypes(this Terrain terrain)
        {

            if (terrain == null) return null;

            SplatPrototype[] oldProtos = terrain.terrainData.splatPrototypes;
            SplatPrototype[] newProtos = new SplatPrototype[oldProtos.Length];
            for (int i = 0; i < oldProtos.Length; i++)
            {
                SplatPrototype oldProto = oldProtos[i];
                SplatPrototype newProto = new SplatPrototype();
                newProtos[i] = newProto;

                newProto.texture = oldProto.texture;
                newProto.tileSize = oldProto.tileSize;
                newProto.tileOffset = oldProto.tileOffset;
                newProto.normalMap = oldProto.normalMap;
            }

            return newProtos;
        }

        public static void SetKeyword(string name, bool value)
        {

            if (value) Shader.EnableKeyword(name);
            else
                Shader.DisableKeyword(name);

        }

        public static MeshCollider ForceMeshCollider(GameObject go)
        {

            Collider[] collis = go.GetComponents<Collider>();

            foreach (Collider c in collis)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

            MeshCollider mc = go.GetComponent<MeshCollider>();

            if (mc == null)
                mc = go.AddComponent<MeshCollider>();

            return mc;

        }

        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        public static Transform tryGetCameraTransform(this GameObject go)
        {
            Camera c = null;
            if (Application.isPlaying)
            {

                c = Camera.main;
            }
#if UNITY_EDITOR
            else
            {
                if (SceneView.lastActiveSceneView != null)
                    c = SceneView.lastActiveSceneView.camera;

            }
#endif

            if (c != null)
                return c.transform;

            c = GameObject.FindObjectOfType<Camera>();
            if (c != null) return c.transform;


            return go.transform;
        }



        // Spin Around object:

        public static Vector2 camOrbit = new Vector2();
        public static Vector3 SpinningAround;
        public static float OrbitDistance = 0;
        public static bool OrbitingFocused;
        public static float SpinStartTime = 0;
        // Use this for initialization
        public static void SpinAround(Vector3 pos, Transform cameraman)
        {
            if (Input.GetMouseButtonDown(2))
            {
                Quaternion before = cameraman.rotation;//cam.transform.rotation;
                cameraman.transform.LookAt(pos);
                Vector3 rot = cameraman.rotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                OrbitDistance = (pos - cameraman.position).magnitude;
                SpinningAround = pos;
                cameraman.rotation = before;
                OrbitingFocused = false;
                SpinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                OrbitDistance = 0;

            if ((OrbitDistance != 0) && (Input.GetMouseButton(2)))
            {

                camOrbit.x += Input.GetAxis("Mouse X") * 5;
                camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

                if (camOrbit.y <= -360)
                    camOrbit.y += 360;
                if (camOrbit.y >= 360)
                    camOrbit.y -= 360;
                //y = Mathf.Clamp (y, min, max);




                Quaternion rot = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
                Vector3 campos = rot *
                    (new Vector3(0.0f, 0.0f, -OrbitDistance)) +
                    SpinningAround;

                cameraman.position = campos;
                if ((Time.time - SpinStartTime) > 0.2f)
                {
                    if (!OrbitingFocused)
                    {
                        cameraman.transform.rotation = MyMath.Lerp(cameraman.rotation, rot, 300 * Time.deltaTime);
                        if (Quaternion.Angle(cameraman.rotation, rot) < 1)
                            OrbitingFocused = true;
                    }
                    else cameraman.rotation = rot;
                }

            }
        }

        public static bool isColorTexturee(this Texture2D tex)
        {
#if UNITY_EDITOR
            if (tex == null) return true;

            TextureImporter importer = tex.getTextureImporter();

            if (importer != null)
                return importer.sRGBTexture;
#endif
            return true;
        }

#if UNITY_EDITOR

        public static TextureImporter getTextureImporter(this Texture2D tex)
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        }


        public static void ReimportToMatchImportConfigOf(this Texture2D dest, Texture2D original)
        {
            TextureImporter dst = dest.getTextureImporter();
            TextureImporter org = original.getTextureImporter();

            if ((dst == null) || (org == null)) return;

            int maxSize = Mathf.Max(original.width, org.maxTextureSize);

            bool needReimport = (dst.wrapMode != org.wrapMode) ||
                                (dst.sRGBTexture != org.sRGBTexture) ||
                                (dst.textureType != org.textureType) ||
                                (dst.alphaSource != org.alphaSource) ||
                                (dst.maxTextureSize < maxSize) ||
                                (dst.isReadable != org.isReadable) ||
                                (dst.textureCompression != org.textureCompression) ||
                                (dst.alphaIsTransparency != org.alphaIsTransparency);

            if (needReimport)
            {
                dst.wrapMode = org.wrapMode;
                dst.sRGBTexture = org.sRGBTexture;
                dst.textureType = org.textureType;
                dst.alphaSource = org.alphaSource;
                dst.alphaIsTransparency = org.alphaIsTransparency;
                dst.maxTextureSize = maxSize;
                dst.isReadable = org.isReadable;
                dst.textureCompression = org.textureCompression;
                dst.SaveAndReimport();
            }

        }

        public static bool hadNoMipmaps(this TextureImporter importer)
        {

            bool needsReimport = false;

            if (importer.mipmapEnabled == false)
            {
                importer.mipmapEnabled = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfMarkedAsNOrmal(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasMarkedAsNormal()))
                importer.SaveAndReimport();
        }
        public static bool wasMarkedAsNormal(this TextureImporter importer)
        {

            /*  bool needsReimport = false;

              if (importer.textureType == TextureImporterType.NormalMap) {
                  importer.textureType = TextureImporterType.Default;
                  needsReimport = true;
              }*/

            return wasMarkedAsNormal(importer, false);

        }
        public static bool wasMarkedAsNormal(this TextureImporter importer, bool convertToNormal)
        {

            bool needsReimport = false;

            if ((importer.textureType == TextureImporterType.NormalMap) != convertToNormal)
            {
                importer.textureType = convertToNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                needsReimport = true;
            }

            return needsReimport;

        }



        public static void Reimport_IfClamped(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasClamped()))
                importer.SaveAndReimport();
        }
        public static bool wasClamped(this TextureImporter importer)
        {

            bool needsReimport = false;


            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfNotReadale(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasNotReadable()))
                importer.SaveAndReimport();
        }
        public static bool wasNotReadable(this TextureImporter importer)
        {

            bool needsReimport = false;



            if (importer.isReadable == false)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Default;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            return needsReimport;


        }

        public static void Reimport_SetIsColorTexture(this Texture2D tex, bool value)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasWrongIsColor(value)))
                importer.SaveAndReimport();
        }
        public static bool wasWrongIsColor(this TextureImporter importer, bool isColor)
        {

            bool needsReimport = false;

            if (importer.sRGBTexture != isColor)
            {
                importer.sRGBTexture = isColor;
                needsReimport = true;
            }

            return needsReimport;
        }

        public static void Reimport_IfNotSingleChanel(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasNotSingleChanel()))
                importer.SaveAndReimport();
        }
        public static bool wasNotSingleChanel(this TextureImporter importer)
        {

            bool needsReimport = false;


            if (importer.textureType != TextureImporterType.SingleChannel)
            {
                importer.textureType = TextureImporterType.SingleChannel;
                needsReimport = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                needsReimport = true;
            }

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfAlphaIsNotTransparency(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasAlphaNotTransparency()))
                importer.SaveAndReimport();

        }
        public static bool wasAlphaNotTransparency(this TextureImporter importer)
        {

            bool needsReimport = false;

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfWrongMaxSize(this Texture2D tex, int width)
        {
            if (tex == null) return;

            TextureImporter importer = tex.getTextureImporter();

            if ((importer != null) && (importer.wasWrongMaxSize(width)))
                importer.SaveAndReimport();

        }
        public static bool wasWrongMaxSize(this TextureImporter importer, int width)
        {

            bool needsReimport = false;

            if (importer.maxTextureSize < width)
            {
                importer.maxTextureSize = width;
                needsReimport = true;
            }

            return needsReimport;

        }


#endif

    }

}





