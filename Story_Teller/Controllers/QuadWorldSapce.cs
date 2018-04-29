using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace StoryTriggerData {


[TagName(QuadWorldSapce.tagName)]
    public class QuadWorldSapce : STD_Poolable {

        public static STD_Pool StoryPoolController;

        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }

	    public const string tagName = "quad";

	    public override string getDefaultTagName () {
			return tagName;
	    }

    public override stdEncoder Encode  (){
        var cody = new stdEncoder();

        cody.Add(stdValues);
            cody.AddIfNotZero("pos", transform.position);

            return cody;
	}

	    public override bool Decode (string subtag, string data) {

            switch (subtag) {
                case STD_Values.storyTag: stdValues.Decode(data); break;
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