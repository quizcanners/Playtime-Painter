﻿using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace PlayerAndEditorGUI.Examples
{

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching

    public abstract class PegiSmartId : SafeIndexBase, IGotIndex
    {
        public int IndexForPEGI
        {
            get { return index; }
            set { index = value; }
        }
    }

    public abstract class PegiSmartIDGeneric<T> : PegiSmartId, IPEGI_ListInspect, IPEGI, IGotDisplayName  where T: IGotIndex, IGotName, new()
    {
        public abstract List<T> GetEnities();

        public T GetEntity()
        {
            if (index == -1)
                return default(T);

            var prots = GetEnities();

            if (prots == null)
                return default(T);
            
            return prots.GetByIGotIndex(index);
        }

        public T GetOrCreateEntity()
        {
            var ent = GetEntity();
            if (ent != null)
                return ent;

            var prots = GetEnities();

            index = prots.GetFreeIndex();

            ent = new T
            {
                IndexForPEGI = index
            };

            return ent;
        }

        public T SetOrCreateEntityByIGotName(string name)
        {
            var ent = GetEntity();
            if (ent != null && ent.NameForPEGI.Equals(name))
                return ent;

            var prots = GetEnities();

            ent = prots.GetByIGotName(name);

            if (ent != null)
            {
                IndexForPEGI = ent.IndexForPEGI;
                return ent;
            }

            index = prots.GetFreeIndex();

            ent = new T
            {
                IndexForPEGI = index,
                NameForPEGI = name
            };

            prots.Add(ent);

            return ent;
        }
        
        #region Inspector
        public virtual bool Inspect()
        {
            var changed = false;

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".nl();
            else
                pegi.select_iGotIndex(ref index, prots).nl();

            T val = GetEntity();

            if (val != null)
                pegi.Try_Nested_Inspect(val).nl(ref changed);
            else
                (GetEnities() == null ? "No Prototypes" : "ID {0} not found in Prototypes".F(index)).nl();

            return changed;
        }

        public virtual bool InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;

            "ID: {0} ".F(index).write(45);

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".write();

            pegi.select_iGotIndex(ref index, prots);

            if (icon.Enter.ClickUnFocus())
                edited = ind;

            return changed;
        }

        public virtual string NameForDisplayPEGI()
        {
            T ent = GetEntity();
            return ent!= null ? ent.GetNameForInspector() : "Id: {0} NOT FOUND".F(index);
        }
        #endregion
    }
    
    public static class PegiIdExtensions
    {
        public static T TryGetEntity<T>(this PegiSmartIDGeneric<T> id) where T: IGotIndex, IGotName, new()
           => id == null ? default(T) : id.GetEntity();

        public static G GetOrCreate<G, T>(this List<G> list, int index) where T : IGotIndex, IGotName, new() where G : PegiSmartIDGeneric<T>, new()
        {
            while (list.Count<=index)
            {
                list.Add(new G());
            }

            return list[index];
        }
    }
}