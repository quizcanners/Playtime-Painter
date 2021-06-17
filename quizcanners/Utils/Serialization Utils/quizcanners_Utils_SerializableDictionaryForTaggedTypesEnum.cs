using QuizCanners.CfgDecode;
using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    public abstract class SerializableDictionaryForTaggedTypesEnum<T> : Dictionary<string, T>, ISerializationCallbackReceiver, IPEGI, IGotDisplayName where T : IGotClassTag
    {

        protected enum SerializationMode { Json = 0, ICfg = 1 }

        [HideInInspector] [SerializeField] protected List<string> keys = new List<string>();
        [HideInInspector] [SerializeField] protected List<string> values = new List<string>();
        [HideInInspector] [SerializeField] protected List<SerializationMode> modes = new List<SerializationMode>();

        protected TaggedTypesCfg Cfg => TaggedTypesCfg.TryGetOrCreate(typeof(T));

        public G GetOrCreate<G>() where G: T , new()
        {
            G tmp;
            if (TryGet(out tmp))
                return tmp;

            G tmpG = new G();

            this[Cfg.GetTag(typeof(G))] = tmpG;

            return tmpG;
        }

        public bool TryGet<G>(out G value) where G : T, new()
        {
            var tag = Cfg.GetTag(typeof(G));

            T tmp;
            var result = TryGetValue(tag, out tmp);

            value = (G)tmp;

            return result;
        }

        #region Serialization
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            modes.Clear();

            foreach (var pair in this)
            {
                string serializedValue;
                var instance = pair.Value;
                var icfg = instance as ICfg;

                var mode = icfg != null ? SerializationMode.ICfg : SerializationMode.Json;

                try
                {
                    switch (mode)
                    {
                        case SerializationMode.ICfg: serializedValue = icfg.Encode().ToString(); break;
                        case SerializationMode.Json:
                        default: serializedValue = JsonUtility.ToJson(instance); break;
                    }
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                    continue;
                }

                keys.Add(instance.ClassTag);
                modes.Add(mode);
                values.Add(serializedValue);
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
                var types = Cfg.TaggedTypes;

                if (types.Count < 1)
                {
                    Debug.LogError("Found no Tagged Types derrived from {0} ".F(typeof(T).ToPegiStringType()));
                }
                else
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        string key = keys[i];
                        SerializationMode mode = modes.TryGet(i, defaultValue: SerializationMode.Json);
                        Type type = types.TryGet(keys[i]);

                        if (type == null)
                        {
                            type = types.ElementAt(0).Value;

                            Debug.LogError("Could not find a class derived from {0} for Tag {1}. Using Default ({2})".F(typeof(T).ToString(), key, type.ToPegiStringType()));
                        }

                        try 
                        {
                            T tmp;
                            switch (mode) 
                            {
                                case SerializationMode.ICfg: 
                                    tmp = (T)Activator.CreateInstance(type);
                                    var icfg = tmp as ICfg;
                                    if (icfg != null)
                                    {
                                        icfg.DecodeFull(new CfgData(values[i]));
                                        tmp = (T)icfg;
                                    }
                                    else Debug.LogError("{0} is not ICfg".F(type));
                                    break;
                                case SerializationMode.Json:
                                default: tmp = (T)JsonUtility.FromJson(values[i], type);  break;
                            }

                            Add(keys[i], tmp);
                        } catch (Exception ex) 
                        {
                            Debug.LogException(ex);
                        }


                       
                    }
                }
            }

            keys.Clear();
            modes.Clear();
            values.Clear();
        }

        #endregion

        #region Inspector

        private int _inspected = -1;

        private string _selectedTag = "_";
        public void Inspect()
        {
            NameForDisplayPEGI().edit_Dictionary(this, ref _inspected).nl();

            if (_inspected == -1) 
            {
                "Tag".select(ref _selectedTag, Cfg.DisplayNames).nl();

                var type = Cfg.TaggedTypes.TryGet(_selectedTag);

                if (type != null)
                {
                    if (ContainsKey(_selectedTag))
                        "Type {0} is already in the Dictionary".F(type).writeHint();
                    else if ("Create {0}".F(type.ToPegiStringType()).Click())
                        this[_selectedTag] = (T)Activator.CreateInstance(type);
                }

                pegi.nl();
            }
        }

        public virtual string NameForDisplayPEGI()
        {
            var tmp = typeof(T).ToString();

            var parts = tmp.Split('.', '+');

            if (parts.Length > 1)
                return parts[parts.Length-2];

            return tmp;
        }

        #endregion
    }
}
