using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class SmartId
    {
        public abstract bool SameAs(SmartId other);
    }

    public abstract class SmartIdGeneric<TValue> : SmartId, IPEGI_ListInspect, IPEGI, IGotReadOnlyName where TValue : IGotName, new()
    {
        public string Id;

        protected abstract SerializableDictionary<string, TValue> GetEnities();

        public virtual TValue GetEntity()
        {
            var prots = GetEnities();

            if (prots != null)
                return prots.TryGet(Id);

            return default(TValue);
        }

        public override bool SameAs(SmartId other) 
        {
            if (other == null)
                return false;

            if (GetType() != other.GetType())
                return false;
            
            var asId = other as SmartIdGeneric<TValue>;

            return Id.Equals(asId.Id);
        }

        #region Inspector

        [NonSerialized] private int _inspectedStuff = -1;
        [NonSerialized] private int _inspectedElement = -1;

        public virtual void Inspect()
        {
            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".nl();

            if (_inspectedStuff == -1)
                pegi.select(ref Id, prots).nl();

            TValue val = GetEntity();

            if (val.GetNameForInspector().isEntered(ref _inspectedStuff, 1).nl())
            { 
                if (val != null)
                    pegi.Try_Nested_Inspect(val).nl();
                else
                    ("ID {0} not found in Prototypes".F(Id)).nl();
            }

            if ("{0} Dictionary".F(typeof(TValue).ToPegiStringType()).isEntered(ref _inspectedStuff, 2).nl())
            {
                typeof(TValue).ToPegiStringType().edit_Dictionary(GetEnities(), ref _inspectedElement);

                if (_inspectedElement == -1)
                    pegi.addDictionaryPairOptions(GetEnities(), newElementName: "A Band of Knuckleheads");
            }
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

        public virtual string GetNameForInspector()
        {
            TValue ent = GetEntity();
            return ent != null ? "Smart Id of {0}".F(ent.GetNameForInspector()) : "Id: {0} NOT FOUND".F(Id);
        }
        #endregion
    }
}