using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic {


    public class TestOnceCondition : ConditionLogic
    {

        public ResultType resultType;
        public int updateValue;

        public override StdEncoder Encode()
        {
            var cody =  new StdEncoder()

            .Add_ifNotZero("v", compareValue)
            .Add_ifNotZero("ty", (int)type)
            .Add_ifNotZero("rty", (int)resultType)
            .Add("g", groupIndex)
            .Add("t", triggerIndex);

            return cody;
        }

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "v": compareValue = data.ToInt(); break;
                case "ty": type = (ConditionType)data.ToInt(); break;
                case "rty": resultType = (ResultType)data.ToInt(); break;
                case "g": groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
#if PEGI
        public override bool PEGI() {

            bool changed = false;

            if (isBoolean() != (resultType == ResultType.SetBool))
            {
                if (("Wrong ResType: " + resultType.ToString() + ". FIX").Click())
                    resultType = isBoolean() ? ResultType.SetBool : ResultType.Set;
            } else {
                changed |= PEGI(0, null, "Cond");

                trig._usage.testOnceConditionPEGI(this, null);

                changed |= searchAndAdd_PEGI(0);
            }

            return base.PEGI() || changed;
        }

#endif

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