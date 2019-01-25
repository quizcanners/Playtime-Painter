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
        public enum MB_state_Editor { nothing, up, down, dragging }
        public static MB_state_Editor[] mouseBttnState = new MB_state_Editor[3];

        public static bool Control => Application.isPlaying ?
                     (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    : Event.current.control;
        
        public static bool Alt => Application.isPlaying ?
                    (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    : (Event.current != null && Event.current.alt);
        

        public static bool Shift => Application.isPlaying ? 
                    (Input.GetKey(KeyCode.LeftShift)
                    || Input.GetKey(KeyCode.RightShift)) : ( Event.current != null && Event.current.shift);
        

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

        public static void FeedMouseEvent(Event e)
        {
            int mb = e.button;

            if (e.type == EventType.MouseLeaveWindow || e.type == EventType.MouseEnterWindow) {
                for (int i = 0; i < 3; i++)
                    mouseBttnState[i] = MB_state_Editor.nothing;
            } 
             else 
            if (mb < 3) switch (e.type) {
                    case EventType.MouseDown: mouseBttnState[mb] = MB_state_Editor.down; break;
                    case EventType.MouseDrag: mouseBttnState[mb] = MB_state_Editor.dragging; break;
                    case EventType.MouseUp: mouseBttnState[mb] = MB_state_Editor.up; break;
                    case EventType.MouseMove: mouseBttnState[mb] = MB_state_Editor.nothing; break;
            }
        }

    }
}