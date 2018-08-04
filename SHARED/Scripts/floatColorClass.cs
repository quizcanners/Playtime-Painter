using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

namespace SharedTools_Stuff
{

    public enum ColorChanel { R = 0, G = 1, B = 2, A = 3 }

    [Flags]
    public enum BrushMask { R = 1, G = 2, B = 4, A = 8 }

    [System.Serializable]
    public class LinearColor : Abstract_STD
    {
        public float r, g, b, a;

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return r;
                    case 1: return g;
                    case 2: return b;
                    case 3: return a;
                }

                return a;
            }
            set
            {
                switch (index)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    case 3: a = value; break;
                }
            }
        }


        Color L_col { get { return new Color(r, g, b, a); } }
        
        public override StdEncoder Encode() => new StdEncoder()
            .Add("r", r)
            .Add("g", g)
            .Add("b", b)
            .Add("a", a);
            
        public override bool Decode(string tag, string data)
        {

            switch (tag)
            {
                case "r": r = data.ToFloat(); break;
                case "g": g = data.ToFloat(); break;
                case "b": b = data.ToFloat(); break;
                case "a": a = data.ToFloat(); break;
                default: return false;
            }
            return true;

        }


        public LinearColor GetCopy()
        {
            return new LinearColor(this);
        }

        public float GetChanel(ColorChanel chan)
        {

            switch (chan)
            {
                case ColorChanel.R:
                    return r;
                case ColorChanel.G:
                    return g;
                case ColorChanel.B:
                    return b;
                default:
                    return a;
            }
        }

        public float GetChanel01(ColorChanel chan)
        {
            return Mathf.Abs(Mathf.Sqrt(GetChanel(chan)));
        }


        public void SetChanelFrom01(ColorChanel chan, float value)
        {
            value *= value;
            switch (chan)
            {
                case ColorChanel.R:
                    r = value;
                    break;
                case ColorChanel.G:
                    g = value;
                    break;
                case ColorChanel.B:
                    b = value;
                    break;
                case ColorChanel.A:
                    a = value;
                    break;
            }
        }

        public void From(Color c)
        {
            c = c.linear;
            r = c.r;
            g = c.g;
            b = c.b;
            a = c.a;
        }

        public void From(Color c, BrushMask bm)
        {
            c = c.linear;
            if ((bm & BrushMask.R) != 0)
                r = c.r;
            if ((bm & BrushMask.G) != 0)
                g = c.g;
            if ((bm & BrushMask.B) != 0)
                b = c.b;
            if ((bm & BrushMask.A) != 0)
                a = c.a;
        }

        public void From(LinearColor c, BrushMask bm)
        {
            if ((bm & BrushMask.R) != 0)
                r = c.r;
            if ((bm & BrushMask.G) != 0)
                g = c.g;
            if ((bm & BrushMask.B) != 0)
                b = c.b;
            if ((bm & BrushMask.A) != 0)
                a = c.a;
        }

        public void CopyFrom(LinearColor col)
        {
            r = col.r;
            g = col.g;
            b = col.b;
            a = col.a;
        }

        public Color ToGamma(float alpha)
        {
            Color tmp = L_col.gamma;
            tmp.a = alpha;
            return tmp;
        }



        public Color ToGamma()
        {

            return L_col.gamma;
        }

        public void ToGamma(ref Color tmp)
        {
            tmp = L_col.gamma;
        }

        public Vector4 Vector4 => new Vector4 ( r, g, b, a);
    
        public void ToV4(ref Vector4 to, BrushMask bm)
        {
            if ((bm & BrushMask.R) != 0)
                to.x = r;
            if ((bm & BrushMask.G) != 0)
                to.y = g;
            if ((bm & BrushMask.B) != 0)
                to.z = b;
            if ((bm & BrushMask.A) != 0)
                to.w = a;
        }

        public void Add(LinearColor other)
        {
            r += other.r;
            g += other.g;
            b += other.b;
            a += other.a;
        }

        public void Add(Color other)
        {
            other = other.linear;
            r += other.r;
            g += other.g;
            b += other.b;
            a += other.a;
        }


        public void LerpTo(LinearColor other, float portion)
        {
            r += (other.r - r) * portion;
            g += (other.g - g) * portion;
            b += (other.b - b) * portion;
            a += (other.a - a) * portion;

        }

        public void AddPortion(LinearColor other, Color portion)
        {
            r += other.r * portion.r;
            g += other.g * portion.g;
            b += other.b * portion.b;
            a += other.a * portion.a;
        }

        public void AddPortion(LinearColor other, float portion)
        {
            r += other.r * portion;
            g += other.g * portion;
            b += other.b * portion;
            a += other.a * portion;
        }

        public void MultiplyBy(float val)
        {
            r *= val;
            g *= val;
            b *= val;
            a *= val;
        }

        public void MultiplyBy(Color val)
        {
            r *= val.r;
            g *= val.g;
            b *= val.b;
            a *= val.a;
        }

        public static Color Multiply(LinearColor a, LinearColor b)
        {
            Color tmp = a.L_col.gamma * b.L_col.gamma;
            return tmp;
        }

        public void Zero()
        {
            r = g = b = a = 0;
        }

        public LinearColor()
        {
            r = g = b = a = 0;
        }

        public LinearColor(Color col)
        {
            From(col);
        }

        public LinearColor(LinearColor col)
        {
            CopyFrom(col);
        }


        public static ArrayManager<LinearColor> array = new ArrayManager<LinearColor>();

    }



}