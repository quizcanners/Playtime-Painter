using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerAndEditorGUI.Examples
{
    
    public abstract class PegiEntityReference<T> : IPEGI_ListInspect, IGotIndex, IPEGI where T : IGotIndex
    {
        public abstract int IndexForPEGI { get; set; }
        
        protected abstract List<T> GetEntities();

        public T TryGetEntity()
        {
            var ent = GetEntities();

            if (ent == null)
                return default(T);

            var byId = ent.GetByIGotIndex(IndexForPEGI);

            return byId;
        }

        public bool InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;

            var ent = GetEntities();
            if (ent != null)
            {
                var id = IndexForPEGI;
                if ("Effect".select_iGotIndex(ref id, ent).changes(ref changed))
                    IndexForPEGI = id;
            }
            else
                "No Entities".write();
            
            if (icon.Enter.Click())
                edited = ind;

            return changed;
        }

        public virtual bool Inspect()
        {
            var changed = false;

            var ent = TryGetEntity();
            
            if (ent == null)
                "Not found {0}".F(IndexForPEGI).nl();
            else
                pegi.Try_Nested_Inspect(ent).changes(ref changed).nl();
            
            return changed;
        }
    }
}