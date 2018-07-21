using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter{

public enum VolumetricDecalType {Add, Dent}



[Serializable]
public class VolumetricDecal : IEditorDropdown {
    public String decalName;
    public VolumetricDecalType type;
    public Texture2D heightMap;
    public Texture2D overlay;

	public override string ToString ()
	{
		return decalName+" ("+type+")";
	}




	public bool ShowInDropdown() {
        return ((heightMap != null) && (overlay != null));
    }
}


}