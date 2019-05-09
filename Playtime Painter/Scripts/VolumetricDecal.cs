using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter {

    public enum VolumetricDecalType {Add, Dent}
    
    [Serializable]
    public class VolumetricDecal : IEditorDropdown, IPEGI, IGotName, IGotDisplayName  {
        public string decalName;
        public VolumetricDecalType type;
        public Texture2D heightMap;
        public Texture2D overlay;

        #region Inspector
        #if PEGI
        public bool ShowInDropdown() => heightMap && overlay;

        public string NameForPEGI { get { return decalName; } set { decalName = value; } }

        public bool Inspect()
        {
            var changed = this.inspect_Name().nl();

            "GetBrushType".editEnum(40, ref type).nl(ref changed);
            "Height Map".edit(ref heightMap).nl(ref changed);
            "Overlay".edit(ref overlay).nl(ref changed);

            return changed;
        }

        public string NameForDisplayPEGI => "{0} ({1})".F(decalName, type);

        #endif
        #endregion
    }


}