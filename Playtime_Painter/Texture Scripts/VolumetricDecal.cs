using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter{

public enum VolumetricDecalType {Add, Dent}



[Serializable]
public class VolumetricDecal : IEditorDropdown, IPEGI, IGotName, IGotDisplayName  {
    public String decalName;
    public VolumetricDecalType type;
    public Texture2D heightMap;
    public Texture2D overlay;

	public bool ShowInDropdown() => heightMap && overlay;
    
#if PEGI
        public string NameForPEGI { get { return decalName; } set { decalName = value; } }

        public bool PEGI() {
            var changed = this.inspect_Name().nl();

            "Type".editEnum(40, ref type).nl();
            "Height Map".edit(ref heightMap).nl();
            "Overlay".edit(ref overlay).nl();

                return changed;
        }

        public string NameForPEGIdisplay() => "{0} ({1})".F(decalName, type);
        
#endif
    }


}