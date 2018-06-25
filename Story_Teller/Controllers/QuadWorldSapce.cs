using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using SharedTools_Stuff;

namespace StoryTriggerData {


[TagName(QuadWorldSapce.tagName)]
    public class QuadWorldSapce : STD_Poolable {

        public static STD_Pool StoryPoolController;

        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }

	    public const string tagName = "quad";

	    public override string GetObjectTag () {
			return tagName;
	    }

    public override StdEncoder Encode  () =>EncodeUnrecognized()
            .Add(InteractionTarget.storyTag, stdValues)
            .Add_IfNotZero("pos", transform.position);

	    public override bool Decode (string subtag, string data) {

            switch (subtag) {
                case InteractionTarget.storyTag: stdValues.Decode(data); break;
			    case "pos" : transform.position = data.ToVector3(); break;
                default: return false;
            }
            return true;
        }

    public override void Reboot() {
            transform.position = Vector3.zero;
    }

			
}


}