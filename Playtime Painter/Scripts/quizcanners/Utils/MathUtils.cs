using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace QuizCannersUtilities {

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static partial class QcMath {

        #region Checks



        public static bool IsNaN(this Vector3 q) => float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z);
        
        public static bool IsNaN(this float f) => float.IsNaN(f);
        
        #endregion

        #region Time

        public static double Miliseconds_To_Seconds(this double interval) => (interval*0.001);

        public static double Seconds_To_Miliseconds(this double interval) => (interval * 1000);

        public static float Miliseconds_To_Seconds(this float interval) => (interval * 0.001f);

        public static float Seconds_To_Miliseconds(this float interval) => (interval * 1000);

        #endregion
        
        #region Adjust

        public static Vector2 ToM11Space(this Vector2 v2) => (v2 - new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y)));

        public static Vector2 To01Space(this Vector2 v2) {

            v2.x = v2.x % 1;
            v2.y = v2.y % 1;

            v2 += Vector2.one;

            v2.x = v2.x % 1;
            v2.y = v2.y % 1;

            return v2;
        }

        public static Vector2 Floor(this Vector2 v2) => new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y));
        
        public static float ClampZeroTo(this float value, float max)
        {
            value = Mathf.Max(0, Mathf.Min(value, max - 1));
            return value;
        }

        public static bool ClampIndexToLength(this Array ar, ref int value, int min = 0)
        {
            if (!ar.IsNullOrEmpty()) {
                value = Mathf.Max(min, Mathf.Min(value, ar.Length - 1));
                return true;
            }
            return false;
        }

        public static bool ClampIndexToCount(this IList list, ref int value, int min = 0)
        {
            if (!list.IsNullOrEmpty()) {
                value = Mathf.Max(min, Mathf.Min(value, list.Count - 1));
                return true;
            }
            return false;
        }

        public static Vector3 RoundDiv(Vector3 v3, int by)
        {
            v3 /= by;
            return ((new Vector3(Mathf.Round(v3.x), Mathf.Round(v3.y), Mathf.Round(v3.z))) * by);
        }

        public static Vector3 FloorDiv(Vector3 v3, int by)
        {
            v3 /= by;
            return ((new Vector3((int)v3.x, (int)v3.y, (int)v3.z)) * by);
        }

        public static Vector3 Round(Vector3 v3)
        {
            return new Vector3(Mathf.Round(v3.x), Mathf.Round(v3.y), Mathf.Round(v3.z));
        }

        public static Vector3 Floor(Vector3 v3)
        {
            return new Vector3((int)v3.x, (int)v3.y, (int)v3.z);
        }

        #endregion

        #region Trigonometry
        static List<Vector3> randomNormalized = new List<Vector3>();
        static int currentNormalized = 0;
        
        public static Vector3 GetRandomPointWithin(this Vector3 v3)
        {

            const int maxRands = 512;

            if (randomNormalized.Count < maxRands)
            {
                var newOne = UnityEngine.Random.insideUnitSphere;
                randomNormalized.Add(newOne);
                v3.Scale(newOne);
            }
            else
            {
                currentNormalized = (currentNormalized + 1) % maxRands;
                v3.Scale(randomNormalized[currentNormalized]);
            }

            return v3;
        }
        
        public static Vector3 BezierCurve(float portion, Vector3 from, Vector3 mid, Vector3 to)
        {
            Vector3 m1 = Vector3.LerpUnclamped(from, mid, portion);
            Vector3 m2 = Vector3.LerpUnclamped(mid, to, portion);
            return Vector3.LerpUnclamped(m1, m2, portion);
        }

        public static float Angle(this Vector2 vec) =>
             (vec.x < 0) ? 360 - (Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg * -1) :
             Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg;
        

        public static bool IsAcute(float a, float b, float c)
        {
            if (c == 0) return true;
            float longest = Mathf.Max(a, b);
            longest *= longest;
            float side = Mathf.Min(a, b);


            return (longest > (c * c + side * side));

        }
        
        public static bool IsPointOnLine(float a, float b, float line, float percision)
        {
            percision *= line;
            float dist;

            if (IsAcute(a, b, line)) dist = Mathf.Min(a, b);
            else
            {
                float s = (a + b + line) / 2;
                float h = 4 * s * (s - a) * (s - b) * (s - line) / (line * line);
                dist = Mathf.Sqrt(h);
            }

            return dist < percision;

            // return ((line > pnta) && (line > pntb) && ((pnta + pntb) < line + percision));

        }

        public static bool IsPointOnLine(Vector3 a, Vector3 b, Vector3 point, float percision)
        {
            float line = (b - a).magnitude;
            float pnta = (point - a).magnitude;
            float pntb = (point - b).magnitude;

            return ((line > pnta) && (line > pntb) && ((pnta + pntb) < line + percision));
        }
        
        public static float HeronHforBase(float _base, float a, float b)
        {
            float sidesSum = a + b;

            if (_base > sidesSum)
                _base = sidesSum * 0.98f;

            float s = (_base + sidesSum) * 0.5f;
            float area = Mathf.Sqrt(s * (s - _base) * (s - a) * (s - b));
            return area / (0.5f * _base);
        }

        public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
        {
            
            float dotNumerator = Vector3.Dot(planePoint - linePoint, planeNormal);
            float dotDenominator = Vector3.Dot(lineVec, planeNormal);

            //line and plane are not parallel
            if (dotDenominator != 0.0f) {

                float length = dotNumerator / dotDenominator;

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + lineVec.normalized * length;

                return true;
            }

            //output not valid
            intersection = Vector3.zero;

            return false;
            
        }

        public static Vector3 GetNormalOfTheTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 p1 = b - a;
            Vector3 p2 = c - a;
            return Vector3.Cross(p1, p2).normalized;
        }

        public static Quaternion Avarage(this List<Quaternion> quaternions)
        {
            Quaternion average = new Quaternion(0, 0, 0, 0);

            int amount = 0;

            foreach (var quaternion in quaternions)
            {
                amount++;

                average = Quaternion.Slerp(average, quaternion, 1f / amount);
            }

            return average;
        }


        #endregion

        #region Transformations

        public static Vector2 YX(this Vector2 vec) => new Vector2(vec.y, vec.x);
        
        public static Vector2 ZW(this Vector4 vec) => new Vector2(vec.z, vec.w);

        public static Vector2 XY(this Vector4 vec) => new Vector2(vec.x, vec.y);

        public static Vector2 XW(this Vector4 vec) => new Vector2(vec.x, vec.w);

        public static Vector2 ZY(this Vector4 vec) => new Vector2(vec.z, vec.y);

        public static Vector2 Clamp01(this Vector2 v2) {
            v2.x = Mathf.Clamp01(v2.x);
            v2.y = Mathf.Clamp01(v2.y);

            return v2;
        }

        public static Vector3 Clamp01(this Vector3 v3)
        {
            v3.x = Mathf.Clamp01(v3.x);
            v3.y = Mathf.Clamp01(v3.y);
            v3.z = Mathf.Clamp01(v3.z);

            return v3;
        }

        public static Vector3 Clamp01(this Vector4 v4)
        {
            v4.x = Mathf.Clamp01(v4.x);
            v4.y = Mathf.Clamp01(v4.y);
            v4.z = Mathf.Clamp01(v4.z);
            v4.w = Mathf.Clamp01(v4.w);

            return v4;
        }

        public static Vector2 Abs(this Vector2 v2) {
            v2.x = Mathf.Abs(v2.x);
            v2.y = Mathf.Abs(v2.y);
            return v2;
        }

        public static Vector3 Abs(this Vector3 v3)
        {
            v3.x = Mathf.Abs(v3.x);
            v3.y = Mathf.Abs(v3.y);
            v3.z = Mathf.Abs(v3.z);
            return v3;
        }

        public static Vector4 Abs(this Vector4 v4)
        {
            v4.x = Mathf.Abs(v4.x);
            v4.y = Mathf.Abs(v4.y);
            v4.z = Mathf.Abs(v4.z);
            v4.w = Mathf.Abs(v4.w);
            return v4;
        }

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 Rotate_Radians(this Vector2 v, float radians)
        {
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector4 ToVector4(this Color col) => new Vector4(col.r, col.g, col.b, col.a);
        
        public static Vector2 XY(this Vector3 vec) => new Vector2(vec.x, vec.y);

        public static Vector4 ToVector4(this Vector2 v2, float z = 0, float w = 0) => new Vector4(v2.x, v2.y, z, w);

        public static Vector2 ToVector2(this Vector3 v3) => new Vector2(v3.x, v3.y);
        
        public static Vector4 ToVector4(this Vector3 v3, float w = 0) => new Vector4(v3.x, v3.y, v3.z, w);

        public static Vector3 ToVector3(this Vector2 v2, float z = 0) => new Vector3(v2.x, v2.y, z);

        public static Vector4 ToVector4(this Vector2 v2xy, Vector2 v2zw) => new Vector4(v2xy.x, v2xy.y, v2zw.x, v2zw.y);

        public static Vector4 ToVector4(this Rect rect) => new Vector4(rect.x, rect.y, rect.width, rect.height);

        public static Rect ToRect(this Vector4 v4) => new Rect(v4.x,v4.y,v4.z,v4.w);

        #endregion
    }
    
    public enum ColorChanel { R = 0, G = 1, B = 2, A = 3 }

    [Flags]
    public enum BrushMask { R = 1, G = 2, B = 4, A = 8, Color = 7, All = 15 }

    [Serializable]
    public struct MyIntVec2
    {
        public int x;
        public int y;

        public int Max => x > y ? x : y;

        public override string ToString() => "x:" + x + " y:" + y;
        
        public void Clamp(int min, int max)  {
            x = Mathf.Clamp(x, min, max);
            y = Mathf.Clamp(y, min, max);
        }

        public void Clamp(int min, MyIntVec2 max) {
            x = Mathf.Clamp(x, min, max.x);
            y = Mathf.Clamp(y, min, max.y);
        }

        public MyIntVec2 MultiplyBy(int val) {
            x *= val;
            y *= val;
            return this;
        }

        public MyIntVec2 Subtract(MyIntVec2 other) {
            x -= other.x;
            y -= other.y;
            return this;
        }

        public Vector2 ToFloat() => new Vector2(x, y);
        
        public MyIntVec2 From(Vector2 vec) {
            x = (int)vec.x;
            y = (int)vec.y;
            return this;
        }

        public MyIntVec2(MyIntVec2 other) {
            x = other.x;
            y = other.y;
        }

        public MyIntVec2(float nx, float ny)
        {
            x = (int)nx;
            y = (int)ny;
        }

        public MyIntVec2(int nx, int ny)
        {
            x = nx;
            y = ny;
        }

        public MyIntVec2(int val)
        {
            x = y = val;
        }

    }
    
    [Serializable]
    public class LinearColor : AbstractCfg
    {
        public float r, g, b, a;

        Color LCol => new Color(r, g, b, a);

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("r", r)
            .Add("g", g)
            .Add("b", b)
            .Add("a", a);

        public override bool Decode(string tg, string data)
        {

            switch (tg)
            {
                case "r": r = data.ToFloat(); break;
                case "g": g = data.ToFloat(); break;
                case "b": b = data.ToFloat(); break;
                case "a": a = data.ToFloat(); break;
                default: return false;
            }
            return true;

        }
        
        public void From(Color c) {
            c = c.linear;
            r = c.r;
            g = c.g;
            b = c.b;
            a = c.a;
        }

        public void From(Color c, BrushMask bm) {
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
        
        public Color ToGamma() => LCol.gamma;
        
        public void ToGamma(ref Color tmp) => tmp = LCol.gamma;
        
        public Vector4 Vector4 => new Vector4(r, g, b, a);

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

        public LinearColor(Color col)
        {
            From(col);
        }
    }

    [Serializable]
    public struct DynamicRangeFloat : ICfg {

        [SerializeField] public float min;
        [SerializeField] public float max;
        [SerializeField] public float value;

        public void SetValue(float nVal)
        {
            value = nVal;
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        #region Inspector
        #if PEGI
        private bool _showRange;

        public bool Inspect() {
            var changed = false;
            var rangeChanged = false;

            var tmp = value;
            if (pegi.edit(ref tmp, min, max).changes(ref changed))
                value = tmp;
            
            if (!_showRange && icon.Edit.ClickUnFocus("Edit Range", 20))
                _showRange = true;

            if (_showRange)  {
                pegi.nl();

                if (icon.FoldedOut.ClickUnFocus("Hide Range"))
                    _showRange = false;

                "Range: [".write(60);

                var before = min;

                tmp = min;

                if (pegi.editDelayed(ref tmp, 40).changes(ref rangeChanged))
                {
                    min = tmp;
                    if (min >= max)
                        max = min + (max - before);
                }

                "-".write(10);
                tmp = max;
                if (pegi.editDelayed(ref tmp, 40).changes(ref rangeChanged))
                {
                    max = tmp;
                    min = Mathf.Min(min, max);

                }

                "]".write(10);

                pegi.nl();

                "Tap Enter to apply Range change in the field (will Clamp current value)".writeHint();

                pegi.nl();
                
                if (rangeChanged)
                    value = Mathf.Clamp(value, min, max);
            }


            return changed | rangeChanged;
        }
        #endif
        #endregion

        #region Encode & Decode

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_IfNotEpsilon("m", min)
            .Add_IfNotEpsilon("v", value)
            .Add_IfNotEpsilon("x", max);

        public void Decode(string data) => data.DecodeTagsFor(this);

        public bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "m": min = data.ToFloat(); break;
                case "v": value = data.ToFloat(); break;
                case "x": max = data.ToFloat(); break;
                default: return false;
            }

            return true;
        }
        #endregion
        
        public DynamicRangeFloat(float min = 0, float max = 1, float value = 0.5f)
        {
            this.min = min;
            this.max = max;
            this.value = value;
#if PEGI
            _showRange = false;
#endif
        }
    }

}

