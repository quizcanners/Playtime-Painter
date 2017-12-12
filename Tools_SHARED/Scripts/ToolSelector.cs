using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using PlayerAndEditorGUI;

public interface IeditorDropdown {
    bool showInDropdown();
}

//public enum customTools {Disabled, Painter, MeshEdit }

public static class ToolComponentExtensions {

	public static void AddOther (this PlaytimeToolComponent tc, PlaytimeToolComponent other, int ind){
		if ((tc != null) && (tc.attachedTools!= null))
			tc.attachedTools[ind] = other;
	}

	public static void RemoveOther (this PlaytimeToolComponent tc, int ind){
		if ((tc != null) && (tc.attachedTools.Length>ind))
			tc.attachedTools [ind] = null;
	}

}

[ExecuteInEditMode]
public class PlaytimeToolComponent : MonoBehaviour {

	public static pegi.windowPositionData windowPosition = new pegi.windowPositionData();

	public virtual string playtimeWindowName { get {return gameObject.name;} }

	public virtual void OnGUI() {

        //Debug.Log("Selected: "+selectedInPlaytime);

		if (selectedInPlaytime == null)
			selectedInPlaytime = this;

		if (selectedInPlaytime == this)  
			windowPosition.Render(PEGI, playtimeWindowName);
	}

	public virtual bool PEGI(){
		pegi.write ("Override PEGI with your functions");
        return false;
	}
		
	public static PlaytimeToolComponent selectedInPlaytime = null;

	public static Type[] tools = null;

	public static Type enabledTool;

	public static int getToolIndex(Type type){
		if (type == null)
			return -1;
		for (int i = 0; i < tools.Length; i++)
			if (tools [i] == type)
				return i;

		return -1;
	}

	public PlaytimeToolComponent[] attachedTools;

	static void checkToolList(){
		if (tools == null) {
			tools = CsharpFuncs.GetAllChildTypesOf<PlaytimeToolComponent> ().ToArray();
			//Debug.Log ("Got "+tools.Length + " tools ");
		}
	}

	public virtual void OnEnable(){
		checkToolList ();

		if (attachedTools == null) {
			attachedTools = new PlaytimeToolComponent[tools.Length];
			int myInd = getToolIndex (GetType ());
			for (int i = 0; i < tools.Length; i++) {
				attachedTools [i] = (PlaytimeToolComponent)gameObject.GetComponent (tools [i]);
				attachedTools [i].AddOther (this, myInd);
			}
		}
	}

	public virtual void OnDestroy(){
		int myInd = getToolIndex (GetType ());
		for (int i = 0; i < attachedTools.Length; i++)
			attachedTools[i].RemoveOther(myInd);
	}

	public virtual Texture ToolIcon(){
		return null;
	}

	public virtual string ToolName(){
		return GetType ().ToString ();
	}

	public void ToolSelector(){
        
		foreach (PlaytimeToolComponent tc in attachedTools) 
            if ((tc!= null) &&  (enabledTool != this.GetType())) {
               // Debug.Log("Tool type " + enabledTool.ToString());
				Texture icon = tc.ToolIcon ();
				if ((icon != null) ? pegi.Click (icon, 25) : pegi.Click (tc.ToolName ()))
					enabledTool = this.GetType();
			}
	}

	public bool ToolManagementPEGI () {
		bool changed = false;
		if (!isCurrentTool())
		{
			if (pegi.Click(icon.Off.getIcon(), "Click to Enable Tool", 35)) {
				enabledTool = this.GetType();
				changed = true;
			}
			pegi.newLine();
		}
		else
		{
            selectedInPlaytime = this;
			if (pegi.Click (icon.On.getIcon (), "Click to Disable Tool", 35))
				enabledTool = null;
			changed = true;
		}


		if ((changed) && (!isCurrentTool()))
			windowPosition.collapse();


		return changed;
	}

	public bool isCurrentTool(){
		return ((enabledTool != null) && (enabledTool == this.GetType ()));
	}

	public static void setTool(int index){
		checkToolList ();
		if ((index >= 0) && (tools.Length > index))
			enabledTool = tools [index];
		else
			enabledTool = null;
	}




	public const string ToolsFolder = "Tools";



	/*
     Use this class to make sure different tools don't interfear with each other.
     */
	
	public static void SetPrefs() {
		PlayerPrefs.SetInt("Tool", getToolIndex(enabledTool));
	}

	public static void GetPrefs() {
		setTool (PlayerPrefs.GetInt ("Tool"));
	}

    // Tags to prevent tools from effecting certain objects:

    public static string[] MeshEditorIgnore = new string[] { "VertexEd", "toolComponent" };

	public static bool MesherCanEditWithTag(string tag) {
		foreach (string x in MeshEditorIgnore)
			if (tag.Contains(x))
				return false;
		return true;
	}

    public static string[] TextureEditorIgnore = new string[] { "VertexEd", "toolComponent", "o" };

    public static bool PainterCanEditWithTag (string tag) {
		foreach (string x in TextureEditorIgnore) 
			if (tag.Contains(x)) 
				return false;
		return true;
	}
		
	public static GameObject refocusOnThis;
	static int scipframes = 3;
	public static void CheckRefocus() {
#if UNITY_EDITOR
		if (refocusOnThis != null) {
			scipframes--;
			if (scipframes == 0) {
				UnityHelperFunctions.FocusOn(refocusOnThis);
				refocusOnThis = null;
				scipframes = 3;
			}
		}
#endif
	}

}
	
