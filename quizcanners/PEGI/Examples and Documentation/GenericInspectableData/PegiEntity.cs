using System.Collections;

namespace QuizCanners.Inspect.Examples
{
    public abstract class PegiEntity : IGotIndex, IGotName, IPEGI_ListInspect
    {
        public abstract int IndexForPEGI { get; set; }

        public abstract string NameForPEGI { get; set; }
        
        public virtual void InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;

            "ID".write(25);

            var id = IndexForPEGI;
            if (pegi.edit(ref id, 35))
                IndexForPEGI = id;
            var name = NameForPEGI;

            if (pegi.edit(ref name))
                NameForPEGI = name;

            if (icon.Enter.Click(ref changed))
                edited = ind;
        }
    }
}
