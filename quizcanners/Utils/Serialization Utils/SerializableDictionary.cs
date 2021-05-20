using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils 
{ 
    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> 
        : Dictionary<TKey, TValue>, ISerializationCallbackReceiver, IPEGI  {
        [HideInInspector][SerializeField] private List<TKey> keys = new List<TKey>();
        [HideInInspector][SerializeField] private List<TValue> values = new List<TValue>();

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

            for (int i = 0; i < keys.Count; i++)
                Add(keys[i], values[i]);

            keys.Clear();
            values.Clear();
        }

        #region Inspector
        [NonSerialized] private int _inspectedElement = -1;
        public virtual void Inspect()
        {
            ToString().edit_Dictionary(this,  ref _inspectedElement).nl();
        }
        #endregion
    }
}
