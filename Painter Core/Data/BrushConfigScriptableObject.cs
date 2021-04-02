using UnityEngine;

namespace PlaytimePainter
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/"+ FILE_NAME)]
    public class BrushConfigScriptableObject : ScriptableObject
    {
        public const string FILE_NAME = "Brush Config";

        public Brush brush;


    }
}
