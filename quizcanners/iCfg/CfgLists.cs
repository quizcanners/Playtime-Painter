using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace QuizCanners.CfgDecode
{

    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedListAttribute : Attribute
    {
        public readonly List<Type> derivedTypes;
        public DerivedListAttribute(params Type[] types)
        {
            derivedTypes = new List<Type>(types);
        }
    }

    public abstract class ConfigurationsSO_Generic<T> : ConfigurationsSO_Base where T : Configuration, new()
    {
        public List<T> configurations = new List<T>();

        #region Inspector

        public void InspectShortcut()
        {
            if (configurations.Count == 0)
            {
                if ("New {0}".F(typeof(T).ToPegiStringType()).Click())
                    configurations.Add(new T());
            }
            else
            {
                var any = configurations[0];
                var active = any.ActiveConfiguration as T;

                if (pegi.select(ref active, configurations))
                    any.ActiveConfiguration = active;

                if (active != null && icon.Save.Click())
                    active.SaveCurrentState();
            }
        }

        public override void Inspect() => "Configurations".edit_List(configurations);

        #endregion
    }

    public abstract class ConfigurationsSO_Base : ScriptableObject, IPEGI
    {
        public virtual void Inspect() { }

        public static bool Inspect<T>(ref T configs) where T : ConfigurationsSO_Base
        {
            var changed = false;

            if (configs)
            {
                if (icon.UnLinked.Click("Disconnect config"))
                    configs = null;
                else
                    configs.Nested_Inspect().nl();
            }
            else
            {
                "Configs".edit(90, ref configs);

                if (icon.Create.Click("Create new Config"))
                    configs = QcUnity.CreateScriptableObjectAsset<T>("ScriptableObjects/Configs", "Config");

                pegi.nl();
            }

            return changed;
        }
    }

    [Serializable]
    public abstract class Configuration : ICfg, IPEGI_ListInspect, IGotName
    {
        public string name;
        public string data;

        public abstract Configuration ActiveConfiguration { get; set; }

        public void SetAsCurrent()
        {
            ActiveConfiguration = this;
        }

        public void SaveCurrentState() => data = EncodeData().ToString();

        public abstract CfgEncoder EncodeData();

        #region Inspect

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }

        public virtual void InspectInList(int ind, ref int edited)
        {

            var changed = false;
            var active = ActiveConfiguration;

            bool allowOverride = active == null || active == this;

            bool isActive = this == active;

            if (isActive)
                pegi.SetBgColor(Color.green);

            if (!allowOverride && !data.IsNullOrEmpty() && icon.Delete.ClickUnFocus(ref changed))
                data = null;

            pegi.edit(ref name);

            if (isActive)
            {
                if (icon.Red.ClickUnFocus())
                    ActiveConfiguration = null;
            }
            else
            {

                if (!data.IsNullOrEmpty())
                {
                    if (icon.Play.ClickUnFocus())
                        ActiveConfiguration = this;
                }
                else if (icon.SaveAsNew.ClickUnFocus())
                    SaveCurrentState();
            }

            if (allowOverride)
            {
                if (icon.Save.ClickUnFocus())
                    SaveCurrentState();
            }


            pegi.RestoreBGcolor();
        }

        #endregion

        #region Encode & Decode

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", name)
            .Add_IfNotEmpty("d", data);

        public void Decode(string key, CfgData d)
        {
            switch (key)
            {
                case "n": name = d.ToString(); break;
                case "d": data = d.ToString(); break;
            }
        }

        #endregion

        public Configuration()
        {
            name = "New Config";
        }

        public Configuration(string name)
        {
            this.name = name;
        }

    }

}

