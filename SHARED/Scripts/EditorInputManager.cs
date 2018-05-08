using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerAndEditorGUI
{

    public static class EditorInputManager
    {

        public static Ray raySceneView = new Ray();
        public static Ray GetScreenRay()
        {
            if (Application.isPlaying)
                return (Camera.main != null) ? Camera.main.ScreenPointToRay(Input.mousePosition) : raySceneView;
            else
                return raySceneView;
        }
        public enum MB_state_Editor { up, down, dragging }
        public static MB_state_Editor[] mouseBttnState = new MB_state_Editor[3];

        public static bool getControlKey()
        {
            if (Application.isPlaying)
            {
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            }
            else
                return Event.current.control;
        }

        public static bool getAltKey()
        {
            if (Application.isPlaying)
            {
                return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            }
            else
                return Event.current != null && Event.current.alt;
        }

        public static bool getShiftKey()
        {
            if (Application.isPlaying)
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            else
                return Event.current != null && Event.current.shift;
        }

        public static int GetNumberKeyDown()
        {
            for (int i = 0; i < 10; i++)
                if (Input.GetKey(i.ToString()))
                    return i;

            return -1;
        }

        public static bool GetMouseButtonUp(int no)
        {
            if (Application.isPlaying)
                return Input.GetMouseButtonUp(no);
            else
                return (mouseBttnState[no] == MB_state_Editor.up);
        }

        public static bool GetMouseButtonDown(int no)
        {
            if (Application.isPlaying)
                return Input.GetMouseButtonDown(no);
            else
                return (mouseBttnState[no] == MB_state_Editor.down);
        }

        public static bool GetMouseButton(int no)
        {
            if (Application.isPlaying)
                return Input.GetMouseButton(no);
            else
                return ((mouseBttnState[no] == MB_state_Editor.dragging) || (mouseBttnState[no] == MB_state_Editor.down));
        }

        public static void feedMouseEvent(Event e)
        {
            int mb = e.button;

            for (int i = 0; i < 3; i++)
                if (mouseBttnState[i] == MB_state_Editor.down) mouseBttnState[i] = MB_state_Editor.dragging; // to prevent multiple click readings

            if (mb < 3) switch (e.type)
                {
                    case EventType.MouseDown: EditorInputManager.mouseBttnState[mb] = EditorInputManager.MB_state_Editor.down; break;
                    case EventType.MouseDrag: EditorInputManager.mouseBttnState[mb] = EditorInputManager.MB_state_Editor.dragging; break;
                    case EventType.MouseUp: EditorInputManager.mouseBttnState[mb] = EditorInputManager.MB_state_Editor.up; break;
                }
        }

    }
}