using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Utils
{

    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver, IPEGI
    {
        [HideInInspector] [SerializeField] private List<TKey> keys = new List<TKey>();
        [HideInInspector] [SerializeField] private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError(
                    "there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."
                    .F(keys.Count, values.Count));
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                    Add(keys[i], values[i]);
            }

            keys.Clear();
            values.Clear();
        }

        #region Inspector
        [NonSerialized] protected int inspectedElement = -1;
        public virtual void Inspect()
        {
            ToString().edit_Dictionary(this, ref inspectedElement).nl();
        }
        #endregion
    }

    [Serializable]
    public abstract class SerializableDictionary_ForEnum<TKey, TValue> : SerializableDictionary<TKey, TValue> where TValue : new()
    {
        public virtual void Create(TKey key)
        {
            this[key] = new TValue();
        }

        protected virtual void InspectElementInList(TKey key, int index) 
        {
            string name = key.ToString().SimplifyTypeName();

            var value = this.TryGet(key);

            if (value == null)
            {
                if ("Create {0}".F(name).Click())
                    Create(key);
            }
            else
            {
                if (icon.Delete.ClickConfirm(confirmationTag: "del" + key.ToString()))
                    Remove(key);
                else
                {
                    if (value is IPEGI_ListInspect pgi)
                        pgi.InspectInList(index, ref inspectedElement);
                    else
                        name.try_enter_Inspect(value, ref inspectedElement, index);
                }
            }
        }

        protected virtual void InspectElement(TKey key)
        {
            TValue element = this.TryGet(key);

            if (element == null)
            {
                "NULL".write();
                return;
            }

            if (element is IPEGI pgi)
                pgi.Nested_Inspect();
            else
                pegi.TryDefaultInspect(element as UnityEngine.Object);
        }

        public override void Inspect()
        {
            var type = typeof(TKey);

            type.ToString().nl(PEGI_Styles.ListLabel);

            TKey[] Keys = (TKey[])Enum.GetValues(typeof(TKey));

            if (inspectedElement != -1)
            {
                var key = Keys[inspectedElement];
                if (key.ToString().SimplifyTypeName().isEntered(ref inspectedElement, inspectedElement).nl())
                {
                    InspectElement(key);
                    pegi.nl();
                }
            }
            else
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    InspectElementInList(Keys[i], i);
                    pegi.nl();
                }
            }

        }
    }
}
