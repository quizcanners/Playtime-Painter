using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public interface IEditorDropdown
    {
        bool ShowInDropdown();
    }

    //public enum customTools {Disabled, Painter, MeshEdit }

    public static class ToolComponentExtensions
    {

        public static void AddOther(this PlaytimeToolComponent tc, PlaytimeToolComponent other, int ind)
        {
            if ((tc != null) && (tc.attachedTools != null))
                tc.attachedTools[ind] = other;
        }

        public static void RemoveOther(this PlaytimeToolComponent tc, int ind)
        {
            if ((tc != null) && (tc.attachedTools.Length > ind))
                tc.attachedTools[ind] = null;
        }

    }

    [ExecuteInEditMode]
    public class PlaytimeToolComponent : MonoBehaviour
    {
        #if PEGI
        public static pegi.windowPositionData windowPosition = new pegi.windowPositionData();
        #endif
        public virtual string PlaytimeWindowName => gameObject.name; 

        public virtual void OnGUI()
        {
            if (selectedInPlaytime == null)
                selectedInPlaytime = this;
            #if PEGI
            if (selectedInPlaytime == this)
                windowPosition.Render(PEGI, PlaytimeWindowName);
#endif
        }
        #if PEGI
        public virtual bool PEGI()
        {
            "Override PEGI with your functions".writeHint();
            return false;
        }
#endif
        public static PlaytimeToolComponent selectedInPlaytime = null;

        public static Type[] tools = null;

        public static Type enabledTool;

        public static int GetToolIndex(Type type)
        {
            if (type == null)
                return -1;
            for (int i = 0; i < tools.Length; i++)
                if (tools[i] == type)
                    return i;

            return -1;
        }

        public PlaytimeToolComponent[] attachedTools;

        static void CheckToolList()
        {
            if (tools == null)
                tools = CsharpFuncs.GetAllChildTypesOf<PlaytimeToolComponent>().ToArray();
            
        }

        public virtual void OnEnable()
        {
            CheckToolList();

            if (attachedTools == null)
            {
                attachedTools = new PlaytimeToolComponent[tools.Length];
                int myInd = GetToolIndex(GetType());
                for (int i = 0; i < tools.Length; i++)
                {
                    attachedTools[i] = (PlaytimeToolComponent)gameObject.GetComponent(tools[i]);
                    attachedTools[i].AddOther(this, myInd);
                }
            }
        }

        public virtual void OnDestroy()
        {
            int myInd = GetToolIndex(GetType());
            for (int i = 0; i < attachedTools.Length; i++)
                attachedTools[i].RemoveOther(myInd);
        }

        public virtual Texture ToolIcon() => null;
        

        public virtual string ToolName() => GetType().ToString();
        

        #if PEGI
        public void ToolSelector()
        {

            foreach (PlaytimeToolComponent tc in attachedTools)
                if (tc && (enabledTool != this.GetType()))  {
                    Texture icon = tc.ToolIcon();
                    if ((icon) ? icon.Click(25) : tc.ToolName().Click())
                        enabledTool = this.GetType();
                }
        }

        public bool ToolManagementPEGI()
        {
            bool changed = false;
            if (!IsCurrentTool())
            {
                if (icon.Off.Click("Click to Enable Tool", 35))
                {
                    enabledTool = this.GetType();
                    changed = true;
                }
                pegi.nl();
            }
            else {
                selectedInPlaytime = this;
                if (icon.On.Click("Click to Disable Tool", 35))
                    enabledTool = null;
                changed = true;
            }


            if ((changed) && (!IsCurrentTool()))
                windowPosition.collapse();
            
            return changed;
        }

#endif

        public bool IsCurrentTool() => ((enabledTool != null) && (enabledTool == this.GetType()));
        

        public static void SetTool(int index)
        {
            CheckToolList();
            if ((index >= 0) && (tools.Length > index))
                enabledTool = tools[index];
            else
                enabledTool = null;
        }

        public const string ToolsFolder = "Tools";

        public static void SetPrefs() =>  PlayerPrefs.SetInt("Tool", GetToolIndex(enabledTool));
        

        public static void GetPrefs() =>  SetTool(PlayerPrefs.GetInt("Tool"));
        

        // Tags to prevent tools from effecting certain objects:

        public static List<string> MeshEditorIgnore = new List<string> { "VertexEd", "toolComponent" };

        public static bool MesherCanEditWithTag(string tag)
        {
            foreach (string x in MeshEditorIgnore)
                if (tag.Contains(x))
                    return false;
            return true;
        }

        public static List<string> TextureEditorIgnore = new List<string> { "VertexEd", "toolComponent", "o" };

        public static bool PainterCanEditWithTag(string tag)
        {
            foreach (string x in TextureEditorIgnore)
                if (tag.Contains(x))
                    return false;
            return true;
        }

        public static GameObject refocusOnThis;
#if UNITY_EDITOR
        static int scipframes = 3;
#endif
        public static void CheckRefocus()
        {
#if UNITY_EDITOR
            if (refocusOnThis != null)
            {
                scipframes--;
                if (scipframes == 0)
                {
                    UnityHelperFunctions.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    scipframes = 3;
                }
            }
#endif
        }

    }

}