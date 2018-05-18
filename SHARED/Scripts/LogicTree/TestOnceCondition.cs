using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic {

    public class TestOnceCondition : ConditionLogic
    {

        public int _ResultType;
        public int updateValue;

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();

            cody.Add_ifNotZero("v", compareValue);
            cody.Add_ifNotZero("ty", _type);
            cody.Add_ifNotZero("rty", _ResultType);
            cody.Add("g", groupIndex);
            cody.Add("t", triggerIndex);

            return cody;
        }

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "v": compareValue = data.ToInt(); break;
                case "ty": _type = data.ToInt(); break;
                case "g": groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        public override bool TestFor(Values st)
        {
            bool value = base.TestFor(st);

            if (value) {
                // TODO: Apply Results
            }

            return value;
        }

    }
}