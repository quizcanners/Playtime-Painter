using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public sealed class Service
    {
        public static T Get<T>() => Singleton<T>.Instance;

        public static bool Try<T>(Action<T> onFound, Action onFailed = null) 
        {
            var inst = Singleton<T>.Instance;

            try
            {
                if (inst != null)
                {
                    onFound.Invoke(inst);
                    return true;
                }
            } catch (Exception ex) 
            {
                Debug.LogException(ex);
            }

            if (onFailed != null)
            {
                try
                {
                    onFailed.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return false;
        }

        public static class Locator
        {
            internal static int Version;

            private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public static object Get(Type type) => _services.TryGet(type);

            public static void RegisterService(object service, Type type) 
            {
                _services[type] = service;
                Version++;
            }

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
                Locator.RegisterService(this, type);
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
                Locator.RegisterService(this, GetType()); 
            }

            public string NameForDisplayPEGI() => QcSharp.AddSpacesInsteadOfCapitals(GetType().ToString().SimplifyTypeName(), keepCatipals: false);
        }

        private static class Singleton<T>
        {
            private static T instance;
            private static readonly Gate.Integer _versionGate = new Gate.Integer();
            public static T Instance
            {
                get
                {
                    if (_versionGate.TryChange(Locator.Version))
                    {  
                        instance = (T)Locator.Get(typeof(T));
                    }

                    return instance;
                }
                set
                {
                    instance = value;
                    Locator.RegisterService(value, typeof(T));
                }
            }
        }
    }
}
