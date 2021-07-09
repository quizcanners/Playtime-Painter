using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public sealed class Service
    {
        public static T Get<T>() => Singleton<T>.Instance;

        public static class Locator
        {
            internal static Dictionary<Type, object> _services = new Dictionary<Type, object>();

            private static int _inspectedService = -1;
            public static void Inspect()
            {
                "Services".edit_Dictionary(_services, ref _inspectedService, showKey: false).nl();
            }
        }

        public abstract class BehaniourBase : MonoBehaviour, IPEGI, IGotReadOnlyName, IPEGI_ListInspect, INeedAttention
        {
            protected virtual void OnEnable()
            {
                var type = GetType();
                Locator._services[type] = this;
            }

            #region Inspector

            public virtual string GetNameForInspector() => QcSharp.AddSpacesInsteadOfCapitals(GetType().ToPegiStringType(), keepCatipals: false);

            public virtual void Inspect()
            {
                string typeName = GetNameForInspector();

                if (typeName.Equals(gameObject.name) == false && "Set Go Name".Click())
                    gameObject.name = typeName;

                pegi.nl();
            }

            public virtual void InspectInList(ref int edited, int ind)
            {
                var myName = GetNameForInspector();

                if (!gameObject.name.Equals(myName) && icon.Refresh.Click())
                    gameObject.name = myName;

                if (GetNameForInspector().ClickLabel() || icon.Enter.Click())
                    edited = ind;

                this.ClickHighlight();

            }

            public virtual string NeedAttention()
            {
                if (!enabled || !gameObject.activeInHierarchy)
                    return "Object is Disabled";

                return null;
            }
            #endregion

        }

        public abstract class ClassBase 
        {
            public ClassBase()
            {
                Locator._services[GetType()] = this;
            }

            public string NameForDisplayPEGI() => QcSharp.AddSpacesInsteadOfCapitals(GetType().ToString().SimplifyTypeName(), keepCatipals: false);
        }

        private static class Singleton<T>
        {
            private static T instance;
            public static T Instance
            {
                get
                {
                    if (instance == null)
                        instance = (T)Locator._services.TryGet(typeof(T));

                    return instance;
                }
                set
                {
                    instance = value;
                    Locator._services[typeof(T)] = value;
                }
            }
        }
    }
}
