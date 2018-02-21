using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR

public abstract class SceneViewEditable<T> : Editor where T : MonoBehaviour {

    public static T painter;

    public abstract bool AllowEditing(T targ);

    public abstract bool OnEditorRayHit(RaycastHit tf, Ray ray);

    public static bool navigating = false;

    public virtual void OnEnable() {
        navigating = true;
    }

    public static bool L_mouseDwn;
    public static bool L_mouseUp;

    public bool isCurrentTool() {
        return PlaytimeToolComponent.enabledTool == typeof(T);
    }

    public void CloseAllButThis(T trg) {
        trg.enabled = true;
        GameObject go = trg.gameObject;
        Component[] cs = go.GetComponents(typeof(Component));

        foreach (Component c in cs)
            if (c.GetType() != typeof(T))
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, false);


        UnityHelperFunctions.FocusOn(null);
        PlaytimeToolComponent.refocusOnThis = go;
    }

    public Vector2 mousePosition;
    public Ray rayGUI = new Ray();

    public virtual void getEvents(Event e, Ray ray) { }

    public virtual void GridUpdate(SceneView sceneview) {

        if (!isCurrentTool())
            return;


        Event e = Event.current;

        if (e.isMouse) {

            L_mouseDwn = (e.type == EventType.MouseDown) && (e.button == 0);
            L_mouseUp = (e.type == EventType.MouseUp) && (e.button == 0);

            mousePosition = Event.current.mousePosition;
            rayGUI = HandleUtility.GUIPointToWorldRay(mousePosition);
        }

        getEvents(e, rayGUI);

        if (e.isMouse) {

            RaycastHit hit;
            bool ishit = Physics.Raycast(rayGUI, out hit);

            T pp = ishit ? hit.transform.GetComponent<T>() : null;

            bool refocus = OnEditorRayHit(hit, rayGUI);

            if ((L_mouseDwn) && (e.button == 0) && (refocus) && ishit) {

                if ((pp != null) && (pp == painter) && (AllowEditing(painter)))
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                UnityHelperFunctions.FocusOn(hit.transform.gameObject);
            }

            if ((!navigating) && AllowEditing(painter))
                e.Use();
        }
    }

    public virtual void OnSceneGUI() {


        // if (Application.isPlaying)
        //   return;

        if (isCurrentTool() && (painter != null) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter) == false))
            PlaytimeToolComponent.enabledTool = null;

        if (AllowEditing(painter)) {

            GridUpdate(SceneView.currentDrawingSceneView);

            if ((!navigating) && (isCurrentTool()))
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        }

        navigating = false;
    }


}
#endif