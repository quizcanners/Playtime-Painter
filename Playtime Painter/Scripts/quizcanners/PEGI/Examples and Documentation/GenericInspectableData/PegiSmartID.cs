using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace PlayerAndEditorGUI.Examples
{

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class PegiSmartID<T> : IPEGI_ListInspect, IPEGI, IGotDisplayName, IGotIndex where T: IGotIndex, IGotName, new()
    {
        public int id = -1;

        public int IndexForPEGI {
            get { return id; }
            set { id = value; }
        }

        public abstract List<T> GetEnities();

        public T GetEntity()
        {
            if (id == -1)
                return default(T);

            var prots = GetEnities();

            if (prots == null)
                return default(T);
            
            return prots.GetByIGotIndex(id);
        }

        public T GetOrCreateEntity()
        {
            var ent = GetEntity();
            if (ent != null)
                return ent;

            var prots = GetEnities();

            id = prots.GetFreeIndex();

            ent = new T
            {
                IndexForPEGI = id
            };

            return ent;
        }

        public T GetOrCreateEntityByIGotName(string name)
        {
            var ent = GetEntity();
            if (ent != null)
                return ent;

            var prots = GetEnities();
            
            id = prots.GetFreeIndex();

            ent = new T
            {
                IndexForPEGI = id,
                NameForPEGI = name
            };

            prots.AddOrReplaceByIGotName(ent);

            return ent;
        }
        
        public virtual bool Inspect()
        {
            var changed = false;

            T val = GetEntity();

            if (val != null)
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
            T ent = GetEntity();
            return ent!= null ? ent.GetNameForInspector() : "Id: {0} NOT FOUND".F(id);
        }
    }


    public static class PegiIdExtensions
    {
        public static T TryGetEntity<T>(this PegiSmartID<T> id) where T: IGotIndex, IGotName, new()
           => id == null ? default(T) : id.GetEntity();

        public static G GetOrCreate<G, T>(this List<G> list, int index) where T : IGotIndex, IGotName, new() where G : PegiSmartID<T>, new()
        {
            while (list.Count<=index)
            {
                list.Add(new G());
            }

            return list[index];
        }
    }
}