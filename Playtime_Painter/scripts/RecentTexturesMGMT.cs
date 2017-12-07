using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
 

namespace TextureEditor{

	public static class RecentTextureExtensions {
	
		public static void RemoveEmpty (this Dictionary<string, List<Texture>> dic){
			foreach (KeyValuePair<string, List<Texture>> l in dic)
				l.Value.RemoveEmpty ();
		}


		public static void AddIfNew (this Dictionary<string, List<Texture>> dic, string Property, Texture texture){
			//Debug.Log ("Trying to add if new for "+Property);
			List<Texture> mgmt;
			if (!dic.TryGetValue (Property, out mgmt)) {
				//Debug.Log ("Creating a new list");
				mgmt = new List<Texture> ();
				dic.Add (Property, mgmt);
			}
		
			if (!mgmt.ContainsDuplicant (texture))
				mgmt.Add (texture);
			//else
				//Debug.Log ("Already in the list");

		}

	}
}