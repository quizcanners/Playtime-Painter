using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class SmartId
    {
        public abstract bool SameAs(SmartId other);
    }

    public abstract class SmartIdGeneric<TKey, TValue> : SmartId, IPEGI_ListInspect, IPEGI, IGotDisplayName
    {
        public TKey Id;

        protected abstract SerializableDictionary<TKey, TValue> GetEnities();

        public override bool SameAs(SmartId other) 
        {
            if (other == null)
                return false;

            if (GetType() != other.GetType())
            {
                return false;
            }

            var asId = other as SmartIdGeneric<TKey, TValue>;

            return Id.Equals(asId.Id);
        }

        public virtual TValue GetEntity()
        {
            var prots = GetEnities();

            if (prots == null)
                return default(TValue);

            return prots.TryGet(Id);
        }

        #region Inspector
        public virtual void Inspect()
        {
            pegi.nl();

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".nl();
            else
                pegi.select(ref Id, prots).nl();

            TValue val = GetEntity();

            if (val != null)
                pegi.Try_Nested_Inspect(val).nl();
            else
                (GetEnities() == null ? "No Prototypes" : "ID {0} not found in Prototypes".F(Id)).nl();

        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            pegi.CopyPaste.InspectOptionsFor(ref Id);
            "Key: {0} ".F(Id).write(45);

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".write();

            pegi.select(ref Id, prots);

            if (icon.Enter.ClickUnFocus())
                edited = ind;

        }

        public virtual string NameForDisplayPEGI()
        {
            TValue ent = GetEntity();
            return ent != null ? ent.GetNameForInspector() : "Id: {0} NOT FOUND".F(Id);
        }
        #endregion
    }
}