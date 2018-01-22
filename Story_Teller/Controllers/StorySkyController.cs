using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StoryTriggerData;

public class StorySkyController : skyController {

    public override void Update()   {

        // Got some clipping when using _WorldSpaceLightPos0 
        if (directional != null)
        {

            Vector3 v3 = directional.transform.rotation * Vector3.back;
            Shader.SetGlobalVector("_SunDirection", new Vector4(v3.x, v3.y, v3.z));
            Shader.SetGlobalColor("_Directional", directional.color);
        }
        Camera c = Camera.main;
        if (c != null)
        {
            Vector3 pos = (c.transform.position + SpaceValues.playerPosition.Meters + SpaceValues.playerPosition.KM*SpaceValues.meters_In_Kilometer) * skyDynamics;
            Shader.SetGlobalVector("_Off", new Vector4(pos.x, pos.z, 0f, 0f));
        }
    }
}
