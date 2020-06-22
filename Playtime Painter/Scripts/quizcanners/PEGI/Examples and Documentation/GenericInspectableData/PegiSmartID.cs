using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace PlayerAndEditorGUI.Examples
{

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class PegiSmartID<T> : IPEGI_ListInspect, IPEGI, IGotDisplayName where T: IGotIndex
    {
        public int id;

        public abstract List<T> GetEnities();

        public bool TryGetEntity(out T value)
        {
            var prots = GetEnities();

            if (prots == null)
            {
                value = default(T);
                return false;
            }

            value = prots.GetByIGotIndex(id);

            return value != null; 
        }

        public virtual bool Inspect()
        {
            var changed = false;

            T val;

            if (TryGetEntity(out val))
                pegi.Try_Nested_Inspect(val).nl(ref changed);
            else
                (GetEnities() == null ? "No Prototypes" : "ID {0} not found in Prototypes".F(id)).nl();

            return changed;
        }

        public virtual bool InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;

            "ID: {0} ".F(id).write(45);

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".write();

            pegi.select_iGotIndex(ref id, prots);

            if (icon.Enter.ClickUnFocus())
                edited = ind;

            return changed;
        }

        public virtual string NameForDisplayPEGI()
        {
            T ent;
            return TryGetEntity(out ent) ? ent.GetNameForInspector() : "Id: {0} NOT FOUND".F(id);
        }
    }
}