using System;
using PlayerAndEditorGUI;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using  QuizCannersUtilities;

namespace Playtime_Painter
{
#if UNITY_2019_1_OR_NEWER
    // Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
    [EditorTool(PainterDataAndConfig.ToolName)]
    class PainterAsIntegratedCustomTool : EditorTool {
        
        GUIContent m_IconContent;

        void OnEnable() {
            m_IconContent = new GUIContent() {
                image = icon.Painter.GetIcon(),
                text = PainterDataAndConfig.ToolName,
                tooltip = "Paint textures and repackage meshes with custom data"
            };
        }

        public override GUIContent toolbarIcon => m_IconContent; 
        
        public override void OnToolGUI(EditorWindow window) {
            
           /* EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;

            using (new Handles.DrawingScope(Color.green))
            {
                position = Handles.Slider(position, Vector3.right);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = position - Tools.handlePosition;

                Undo.RecordObjects(Selection.transforms, "Move Platform");

                foreach (var transform in Selection.transforms)
                    transform.position += delta;
            }*/
        }
    }
#endif
}