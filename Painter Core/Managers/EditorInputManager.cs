using UnityEngine;

namespace QuizCanners.Inspect
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public static class EditorInputManager
    {
        public static Ray mouseRaySceneView = new Ray();
        public static Ray centerRaySceneView = new Ray();

        public static Ray GetScreenMousePositionRay(Camera cam = null)
        {
            if (!cam)
                cam = Camera.main;

            return Application.isPlaying ?
                 cam ? cam.ScreenPointToRay(Input.mousePosition) : mouseRaySceneView 
                 : mouseRaySceneView;
        }

        public enum MB_state_Editor { Nothing, Up, Down, Dragging }
        public static MB_state_Editor[] mouseButtonState = new MB_state_Editor[3];

        public static bool MouseToPlane(this Plane plane, out Vector3 hitPos, Camera cam = null)
            => plane.MouseToPlane(out hitPos, GetScreenMousePositionRay(cam));

        public static bool MouseToPlane(this Plane plane, out Vector3 hitPos, Ray ray)
        {
            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                hitPos = ray.GetPoint(rayDistance);
                return true;
            }

            hitPos = Vector3.zero;

            return false;
        }


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
            return (mouseButtonState[no] == MB_state_Editor.Up);
        }

        public static bool GetMouseButtonDown(int no)
        {
            if (Application.isPlaying)
                return Input.GetMouseButtonDown(no);
            return (mouseButtonState[no] == MB_state_Editor.Down);
        }

        public static bool GetMouseButton(int no)
        {
            if (Application.isPlaying)
                return Input.GetMouseButton(no);
            return ((mouseButtonState[no] == MB_state_Editor.Dragging) || (mouseButtonState[no] == MB_state_Editor.Down));
        }

        public static void FeedMouseEvent(Event e)
        {
            var mb = e.button;

            if (e.type == EventType.MouseLeaveWindow || e.type == EventType.MouseEnterWindow) {
                for (int i = 0; i < 3; i++)
                    mouseButtonState[i] = MB_state_Editor.Nothing;
            } 
             else 
            if (mb < 3) switch (e.type) {
                    case EventType.MouseDown: mouseButtonState[mb] = MB_state_Editor.Down; break;
                    case EventType.MouseDrag: mouseButtonState[mb] = MB_state_Editor.Dragging; break;
                    case EventType.MouseUp: mouseButtonState[mb] = MB_state_Editor.Up; break;
                    case EventType.MouseMove: mouseButtonState[mb] = MB_state_Editor.Nothing; break;
            }
        }

    }
}