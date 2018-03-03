using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Playtime_Painter{

public enum VolumetricDecalType {Add, Dent}



[Serializable]
public class VolumetricDecal : IeditorDropdown {
    public String decalName;
    public VolumetricDecalType type;
    public Texture2D heightMap;
    public Texture2D overlay;

	public override string ToString ()
	{
		return decalName+" ("+type+")";
	}




	public bool showInDropdown() {
        return ((heightMap != null) && (overlay != null));
    }
}


}