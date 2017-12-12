using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using StoryTriggerData;

public enum ColorChanel { R = 0, G = 1, B = 2, A = 3 }

[System.Serializable]
public class linearColor : abstract_STD  {
    public float r, g, b, a;

    public override stdEncoder Encode() {
        stdEncoder cody = new stdEncoder();

        cody.Add("r",r);
        cody.Add("g", g);
        cody.Add("b", b);
        cody.Add("a", a);

        return cody;
    }

   

    public override void Decode(string tag, string data) {

        switch (tag) {
            case "r": r = data.ToFloat(); break;
            case "g": g = data.ToFloat(); break;
            case "b": b = data.ToFloat(); break;
            case "a": a = data.ToFloat(); break;
        }

    }

    public const string toryTag = "LCol";

    public override string getDefaultTagName() { return toryTag; }



    public linearColor GetCopy(){
        return new linearColor(this);
    }

    public float GetChanel(ColorChanel chan) {
     
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

    public float GetChanel01(ColorChanel chan) {
        return Mathf.Abs(Mathf.Sqrt(GetChanel(chan)));
    }


    public void SetChanelFrom01(ColorChanel chan, float value)  {
        value *= value;
        switch (chan) {
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

    public void From(Color c) {
        r = c.r * c.r;
        g = c.g * c.g;
        b = c.b * c.b;
        a = c.a * c.a;
    }

    public void From(Color c, BrushMask bm)  {
        if ((bm & BrushMask.R) != 0)
            r = c.r * c.r;
        if ((bm & BrushMask.G) != 0)
            g = c.g * c.g;
        if ((bm & BrushMask.B) != 0)
            b = c.b * c.b;
        if ((bm & BrushMask.A) != 0)
            a = c.a * c.a;
    }

    public void From(linearColor c, BrushMask bm)
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

    public void CopyFrom(linearColor col)
    {
        r = col.r;
        g = col.g;
        b = col.b;
        a = col.a;
    }

    public Color ToColor(float alpha) {
        Color tmp = new Color();
        tmp.r = Mathf.Sqrt(r);
        tmp.g = Mathf.Sqrt(g);
        tmp.b = Mathf.Sqrt(b);
        tmp.a = alpha;
        return tmp;
    }

    public Color ToColor()
    {
        Color tmp = new Color();
        tmp.r = Mathf.Sqrt(r);
        tmp.g = Mathf.Sqrt(g);
        tmp.b = Mathf.Sqrt(b);
        tmp.a = Mathf.Sqrt(a);
        return tmp;
    }

    public void ToColor(ref Color tmp) {
        tmp.r = Mathf.Sqrt(r);
        tmp.g = Mathf.Sqrt(g);
        tmp.b = Mathf.Sqrt(b);
        tmp.a = Mathf.Sqrt(a);
    }

    public Vector4 ToV4() {
        Vector4 to = new Vector4();

            to.x = r;
        
            to.y = g;
      
            to.z = b;
      
            to.w = a;

        return to;
    }

    public void ToV4(ref Vector4 to , BrushMask bm )
    {
        if ((bm & BrushMask.R) != 0)
            to.x = r ;
        if ((bm & BrushMask.G) != 0)
            to.y = g ;
        if ((bm & BrushMask.B) != 0)
            to.z = b ;
        if ((bm & BrushMask.A) != 0)
            to.w = a ;
    }

    public void Add(linearColor other) {
        r += other.r;
        g += other.g;
        b += other.b;
        a += other.a;
    }

    public void Add(Color other) {
        r += other.r * other.r;
        g += other.g * other.g;
        b += other.b * other.b;
        a += other.a * other.a;
    }


    public void LerpTo (linearColor other, float portion) {
        r += (other.r - r) * portion;
        g += (other.g - g) * portion;
        b += (other.b - b) * portion;
        a += (other.a - a) * portion;

    }

    public void AddPortion(linearColor other, Color portion)
    {
        r += other.r * portion.r;
        g += other.g * portion.g;
        b += other.b * portion.b;
        a += other.a * portion.a;
    }

    public void AddPortion(linearColor other, float portion) {
        r += other.r * portion;
        g += other.g * portion;
        b += other.b * portion;
        a += other.a * portion;
    }

    public void MultiplyBy (float val) {
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

    public static Color Multiply(linearColor a, linearColor b)  {
        Color tmp = new Color();

        tmp.r = (Mathf.Sqrt(a.r) * Mathf.Sqrt(b.r));
        tmp.g = (Mathf.Sqrt(a.g) * Mathf.Sqrt(b.g));
        tmp.b = (Mathf.Sqrt(a.b) * Mathf.Sqrt(b.b));
        tmp.a = (Mathf.Sqrt(a.a) * Mathf.Sqrt(b.a));

        return tmp;
    }

    public void Zero() {
        r = g = b = a = 0;
    }

    public linearColor()
    {
        r = g = b = a = 0;
    }

    public linearColor(Color col) {
        From(col);
    }

    public linearColor(linearColor col)
    {
        CopyFrom(col);
    }

   
    public static ArrayManager<linearColor> array = new ArrayManager<linearColor>();

}



