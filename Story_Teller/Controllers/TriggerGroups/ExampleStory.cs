using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * INSTRUCTION:
 * In order to enable merging of many stories you may want to RENAME this classes and set HashCode to your favorite number. 
 * Rename everything that has word 'Example' to whatever you like. 
 * HINT: 
 * a) You may want to create few or more story classes for your convenience, so make a copy of this class, comment it out, so you can use quick Rename operation later to get another story.
 * b) Also you can move coresponding classes to folders that contain story data
 */

namespace StoryTriggerData {

    public class MyStory_EXAMPLE : TriggerGroups {


       
        // If you want to have variables accessable from code, add enums
        // When adding new enums keep in mind it will override the ones under the same index. If your story is developed, assign index you haven't used yet,
        // like "Princess_Saved = 50"  means that indexes 4 to 49  are likely used for other purpouses. If not, no problem. 
        // If you start writing this from 50 or 500 and skip somu numbers, it's ok.

        public enum integers { Player_Level = 0, Player_Cash = 1, Player_Experience = 3 }
        public enum booleans { lockFPScamera = 4, StereoOn = 5, Menu_is_Open = 6, Princess_Saved = 50 }

        public static int group = 1234;// Change this value to something

        public static MyStory_EXAMPLE _inst;

        public MyStory_EXAMPLE() {
            _inst = this;
        }

        public override int GetHashCode() {
            return group; 
        }

        public override string ToString() {
            return typeof(MyStory_EXAMPLE).Name;
        }

        public override Type getIntegerEnums() {
            return typeof(integers);
        }

        public override Type getBooleanEnums() {
            return typeof(booleans);
        }

    }

    public static class MyStory_EXAMPLE_Extensions {

        public static int Get(this STD_Values st, MyStory_EXAMPLE.integers ind) {
            return st.ints[MyStory_EXAMPLE.group][(int)ind];
        }

        public static void Set(this STD_Values st, MyStory_EXAMPLE.integers ind, int value) {
            st.ints[MyStory_EXAMPLE.group][(int)ind] = value;
        }

        public static bool Get(this STD_Values st, MyStory_EXAMPLE.booleans ind) {
            return st.bools[MyStory_EXAMPLE.group][(int)ind];
        }

        public static void Set(this STD_Values st, MyStory_EXAMPLE.booleans ind, bool value) {
            st.bools[MyStory_EXAMPLE.group][(int)ind] = value;
        }

    }
}