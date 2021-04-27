using QuizCanners.Inspect;
using UnityEngine;

namespace PainterTool
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/"+ FILE_NAME)]
    public class PlaytimePainter_BrushConfigScriptableObject : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Brush Config";

      

        public Brush brush;

        public void Inspect()
        {
            pegi.Nl();
            pegi.CopyPaste.InspectOptionsFor(ref brush);
            pegi.Nl();
            brush.Nested_Inspect();
        }
    }

    [PEGI_Inspector_Override(typeof(PlaytimePainter_BrushConfigScriptableObject))] 
    internal class PlaytimePainter_BrushConfigScriptableObjectDrawer : PEGI_Inspector_Override { }
}
