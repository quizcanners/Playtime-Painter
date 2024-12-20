using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/"+ FILE_NAME)]
    public class SO_BrushConfigScriptableObject : ScriptableObject, IPEGI
    {
        public const string FILE_NAME = "Brush Config";

        public Brush brush;

        public override string ToString() => "Brush Config ({0})".F(name);

        void IPEGI.Inspect()
        {
            pegi.CopyPaste.InspectOptionsFor(ref brush);
            pegi.Nl();
            brush.Nested_Inspect();
        }
    }

    [PEGI_Inspector_Override(typeof(SO_BrushConfigScriptableObject))] 
    internal class PlaytimePainter_BrushConfigScriptableObjectDrawer : PEGI_Inspector_Override { }
}
