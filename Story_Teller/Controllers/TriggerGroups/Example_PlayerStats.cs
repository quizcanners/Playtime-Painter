using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
using STD_Logic;

namespace StoryTriggerData
{

    // CHANGE NAME (Refaractor):
    public class Example_PlayerStats : TriggerGroup
    {
        // CHANGE GROUP INDEX:
        public const int group = 5678;  

        // You are good to go)


        // When adding new enums keep in mind it will override the ones under the same index. If your story is developed, assign index you haven't used yet,
        public enum integers { CurrentCase = 0, Rank = 1, Clues = 2, Case_Partners_Count = 4 } // !!! those are indexes, not default values
        public enum booleans { GotTheBadge = 3, FinishedDetectiveTraining = 5 }

        public static Example_PlayerStats _inst;

        public Example_PlayerStats() {
            _inst = this;
        }

        public override int GetHashCode() {
            return group; // Change this value to something
        }

        public override string ToString() {
            return typeof(Example_PlayerStats).Name;
        }

        public override Type getIntegerEnums() {
            return typeof(integers);
        }

        public override Type getBooleanEnums() {
            return typeof(booleans);
        }

    }



    public static class EXAMPLE_PlayerStats_Extensions
    {

        public static int Get(this InteractionTarget st, Example_PlayerStats.integers ind) {
            return st.ints[Example_PlayerStats.group][(int)ind];
        }

        public static void Set(this InteractionTarget st, Example_PlayerStats.integers ind, int value) {
            st.ints[Example_PlayerStats.group][(int)ind] = value;
        }

        public static bool Get(this InteractionTarget st, Example_PlayerStats.booleans ind) {
            return st.bools[Example_PlayerStats.group][(int)ind];
        }

        public static void Set(this InteractionTarget st, Example_PlayerStats.booleans ind, bool value) {
            st.bools[Example_PlayerStats.group][(int)ind] = value;
        }


       

    }
}