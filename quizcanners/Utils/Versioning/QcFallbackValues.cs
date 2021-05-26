using QuizCanners.Inspect;
using System;

namespace QuizCanners.Utils
{
    public abstract class FallbackValueBase : IPEGI
    {
        public bool Fallback = true;

        public virtual void Inspect()
        {
            bool isOverride = !Fallback;
            if ("Override".toggleIcon(ref isOverride, hideTextWhenTrue: true))
                Fallback = !isOverride;
        }
    }


    [Serializable]
    public class FloatFallbackValue : FallbackValueBase
    {
        public float Value = 0;

        public override void Inspect()
        {
            base.Inspect();
            if (!Fallback)
                pegi.edit(ref Value);
        }
    }

    [Serializable]
    public class BoolFallbackValue : FallbackValueBase
    {
        public bool Value;

        public override void Inspect()
        {
            base.Inspect();
            if (!Fallback)
                pegi.toggle(ref Value);
        }
    }
}
