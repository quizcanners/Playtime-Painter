using QuizCanners.Utils;
//using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public class CopyPaste
        {
            public class Buffer
            {
                public string CopyPasteJson;
                public string CopyPasteJsonSourceName;
            }

            private static readonly Dictionary<System.Type, Buffer> _copyPasteBuffs = new Dictionary<System.Type, Buffer>();

            private static Buffer GetOrCreate(System.Type type) 
            {
                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff) == false)
                {
                    buff = new Buffer();
                    _copyPasteBuffs[type] = buff;
                }

                return buff;
            }

#pragma warning disable IDE0060 // Remove unused parameter
            public static bool InspectOptionsFor<T>(ref T el, Buffer buffer = null)
#pragma warning restore IDE0060 // Remove unused parameter
            {
                var type = typeof(T);

                var changed = ChangeTrackStart();

                if (type.IsSerializable)
                {
                    if (_copyPasteBuffs.TryGetValue(type, out buffer))
                    {
                        if (!buffer.CopyPasteJson.IsNullOrEmpty() && icon.Paste.Click("Paste " + buffer.CopyPasteJsonSourceName))
                            JsonUtility.FromJsonOverwrite(buffer.CopyPasteJson, el);
                    }

                    if (icon.Copy.Click().ignoreChanges())
                    {
                        if (buffer == null)
                        {
                            buffer = GetOrCreate(type);
                        }
                        buffer.CopyPasteJson = JsonUtility.ToJson(el);
                        buffer.CopyPasteJsonSourceName = el.GetNameForInspector();
                    }
                }
                return changed;
            }

            public static bool InspectOptions<T>(CollectionMetaData meta= null) 
            {
                if (meta != null && meta[CollectionInspectParams.showCopyPasteOptions] == false)
                    return false;

                var type = typeof(T);

                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff))
                {
                    nl();

                    "Copy Paste: {0}".F(buff.CopyPasteJsonSourceName).write();
                    if (icon.Clear.Click())
                        _copyPasteBuffs.Remove(type);

                    nl();
                }

                return false;
            }
        }
    }
}