using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using PlayerAndEditorGUI;

namespace QuizCannersUtilities {

    public static class MyMath {

        #region Checks
        public static bool IsNaN(this Vector3 q)
        {
            return float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z);
        }

        public static bool IsNaN(this float f) => float.IsNaN(f);
        
        #endregion

        #region Time

        public static double Miliseconds_To_Seconds(this double interval) => (interval*0.001);

        public static double Seconds_To_Miliseconds(this double interval) => (interval * 1000);

        public static float Miliseconds_To_Seconds(this float interval) => (interval * 0.001f);

        public static float Seconds_To_Miliseconds(this float interval) => (interval * 1000);

        #endregion
        
        #region Adjust

        public static Vector2 To01Space(this Vector2 v2)
        {
            return (v2 - new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y)));
        }

        public static Vector2 Floor(this Vector2 v2)
        {
            return new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y));
        }
        
        public static float ClampZeroTo(this float value, float Max)
        {
            value = Mathf.Max(0, Mathf.Min(value, Max - 1));
            return value;
        }

        public static bool ClampIndexToLength(this Array ar, ref int value, int min = 0)
        {
            if (ar != null && ar.Length > 0) {
                value = Mathf.Max(min, Mathf.Min(value, ar.Length - 1));
                return true;
            }
            return false;
        }

        public static bool ClampIndexToCount(this IList list, ref int value, int min = 0)
        {
            if (list != null && list.Count > 0) {
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

        #region Lerps

        public static float SpeedToPortion(this float speed, float dist) => dist != 0 ? Mathf.Clamp01(speed * Time.deltaTime / Mathf.Abs(dist)) : 1;

        public static bool SpeedToMinPortion(this float speed, float dist, ref float portion)
        {

            var nportion = speed.SpeedToPortion(dist);
            if (nportion < portion)
            {
                portion = nportion;
                return true;
            }

            if (portion == 1 && dist > 0)
                return true;

            return false;
        }
        
        public static bool isLerping_bySpeed(ref float from, float to, float speed)
        {
            if (from == to)
                return false;

            from = Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));
            return true;
        }

        public static bool Lerp(ref float from, float to, float speed, out float portion)
        {
            if (from == to)
            {
                portion = 1;
                return false;
            }

            portion = speed.SpeedToPortion(Mathf.Abs(from - to));
            from = Mathf.LerpUnclamped(from, to, portion);

            return true;
        }

        public static float Lerp_bySpeed(float from, float to, float speed)
            => Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));

        public static float Lerp(float from, float to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Mathf.Abs(from - to));
            return Mathf.LerpUnclamped(from, to, portion);
        }

        public static bool LerpAngle_bySpeed(ref float from, float to, float speed)
        {
            float dist = Mathf.Abs(Mathf.DeltaAngle(from, to));
            if (dist > 0)
            {
                float portion = speed.SpeedToPortion(dist);
                from = Mathf.LerpAngle(from, to, portion);
                return true;
            }
            return false;
        }
        
        public static Vector2 Lerp_bySpeed(this Vector2 from, Vector2 to, float speed) => Vector2.LerpUnclamped(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));

        public static Vector2 Lerp(this Vector2 from, Vector2 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector3.Distance(from, to));
            return Vector3.LerpUnclamped(from, to, portion);
        }
        
        public static Vector3 Lerp_bySpeed(this Vector3 from, Vector3 to, float speed) => Vector3.LerpUnclamped(from, to, speed.SpeedToPortion(Vector3.Distance(from, to)));

        public static Vector3 Lerp(this Vector3 from, Vector3 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector3.Distance(from, to));
            return Vector3.LerpUnclamped(from, to, portion);
        }
        
        public static Vector4 Lerp_bySpeed(this Vector4 from, Vector4 to, float speed) => Vector4.LerpUnclamped(from, to, speed.SpeedToPortion(Vector4.Distance(from, to)));

        public static Vector4 Lerp(this Vector4 from, Vector4 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector4.Distance(from, to));
            return Vector4.LerpUnclamped(from, to, portion);
        }
        
        public static Quaternion Lerp_bySpeed(this Quaternion from, Quaternion to, float speed) => Quaternion.LerpUnclamped(from, to, speed.SpeedToPortion(Quaternion.Angle(from, to)));

        public static Quaternion Lerp_bySpeed(this Quaternion from, Quaternion to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Quaternion.Angle(from, to));
            return Quaternion.LerpUnclamped(from, to, portion);
        }

        public static float DistanceRGB(this Color col, Color other)
            =>
            (Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b));

        public static float DistanceRGBA(this Color col, Color other)
        {

            float dist = ((Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b)) * 0.33f + Mathf.Abs(col.a - other.a));

            return dist;
        }

        public static Color Lerp_bySpeed(this Color from, Color to, float speed) => Color.LerpUnclamped(from, to, speed.SpeedToPortion(from.DistanceRGB(to)));

        public static Color Lerp_RGB(this Color from, Color to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(from.DistanceRGB(to));
            to.a = from.a;
            return Color.LerpUnclamped(from, to, portion);
        }

        public static bool isLerpingAlpha_BySpeed<T>(this List<T> imgs, float alpha, float speed) where T : Graphic
        {
            bool changing = false;

            if (!imgs.IsNullOrEmpty())
                foreach (var i in imgs)
                    changing |= i.isLerpingAlpha_BySpeed(alpha, speed);


            return changing;
        }

        public static bool isLerpingAlpha_BySpeed<T>(this T img, float alpha, float speed) where T : Graphic
        {
            bool changing = false;

            if (img)
            {
                var col = img.color;
                col.a = Lerp_bySpeed(col.a, alpha, speed);

                img.color = col;
                changing |= col.a != alpha;
            }

            return changing;
        }

        public static bool IsLerpingRGB_BySpeed<T>(this T img, Color target, float speed) where T : Graphic
        {
            bool changing = false;

            if (img)
            {
                float portion;
                img.color = img.color.Lerp_RGB(target, speed, out portion);
                
                changing = portion < 1;
            }

            return changing;
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
                var newOne = Vector3.one.OnSpherePosition();
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

        public static float Angle(this Vector2 vec)
        {
            if (vec.x < 0)
            {
                return 360 - (Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg;
            }
        }

        public static Vector3 OnSpherePosition(this Vector3 vec)
        {

            var v3 = new Vector3(
                UnityEngine.Random.Range(-10f, 10f),
                  UnityEngine.Random.Range(-10f, 10f),
                  UnityEngine.Random.Range(-10f, 10f)
                );

            v3.Normalize();
            v3.Scale(vec);

            return v3;
        }
        
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
            if (_base > a + b)
                _base = (a + b) * 0.98f;

            float s = (_base + a + b) * 0.5f;
            float area = Mathf.Sqrt(s * (s - _base) * (s - a) * (s - b));
            float h = area / (0.5f * _base);
            return h;
        }

        public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
        {


            float dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
            float dotDenominator = Vector3.Dot(lineVec, planeNormal);

            //line and plane are not parallel
            if (dotDenominator != 0.0f)
            {
                float length = dotNumerator / dotDenominator;

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + lineVec.normalized * length;

                return true;
            }

            //output not valid
            else
            {
                intersection = Vector3.zero;

                return false;
            }
        }

        public static Vector3 GetNormalOfTheTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 p1 = b - a;
            Vector3 p2 = c - a;
            return Vector3.Cross(p1, p2).normalized;
        }

        #endregion

        #region Transformations

        public static Vector2 YX(this Vector2 vec) => new Vector2(vec.y, vec.x);
        
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

        public static Vector4 ToVector4(this Color col)
        {
            return new Vector4(col.r, col.g, col.b, col.a);
        }
        
        public static Vector2 XY(this Vector3 vec) => new Vector2(vec.x, vec.y);

        public static Vector4 ToVector4(this Vector2 v2, float z = 0, float w = 0) => new Vector4(v2.x, v2.y, z, w);

        public static Vector2 ToVector2(this Vector3 v3) => new Vector2(v3.x, v3.y);
        
        public static Vector4 ToVector4(this Vector3 v3, float w = 0) => new Vector4(v3.x, v3.y, v3.z, w);

        public static Vector3 ToVector3(this Vector2 v2, float z = 0) => new Vector3(v2.x, v2.y, z);

        public static Vector4 ToVector4(this Vector2 v2xy, Vector2 v2zw) => new Vector4(v2xy.x, v2xy.y, v2zw.x, v2zw.y);
        #endregion
    }
    
    public enum ColorChanel { R = 0, G = 1, B = 2, A = 3 }

    [Flags]
    public enum BrushMask { R = 1, G = 2, B = 4, A = 8 }

    [Serializable]
    public struct MyIntVec2
    {
        public int x;
        public int y;

        public int Max => x > y ? x : y;

        public override string ToString()
        {
            return "x:" + x + " y:" + y;
        }

        public void Clamp(int min, int max)
        {
            x = Mathf.Clamp(x, min, max);
            y = Mathf.Clamp(y, min, max);
        }

        public void Clamp(int min, MyIntVec2 max)
        {
            x = Mathf.Clamp(x, min, max.x);
            y = Mathf.Clamp(y, min, max.y);
        }

        public MyIntVec2 MultiplyBy(int val)
        {
            x *= val;
            y *= val;
            return this;
        }

        public MyIntVec2 Subtract(MyIntVec2 other)
        {
            x -= other.x;
            y -= other.y;
            return this;
        }

        public Vector2 ToFloat()
        {
            return new Vector2(x, y);
        }

        public MyIntVec2 From(Vector2 vec)
        {
            x = (int)vec.x;
            y = (int)vec.y;
            return this;
        }

        public MyIntVec2(MyIntVec2 other)
        {
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
        
        public Color ToGamma() => L_col.gamma;
        
        public void ToGamma(ref Color tmp)
        {
            tmp = L_col.gamma;
        }

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

    }

    [Serializable]
    public struct DynamicRangeFloat : IPEGI_ListInspect {

        [SerializeField] float min;
        [SerializeField] float max;
        [SerializeField] public float value;

        #region Inspector
        #if PEGI
        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = false;
            bool rangeChanged = false;

            pegi.edit(ref value, min, max).changes(ref changed);
            
            "[".write(10);

            float before = min;

            if (pegi.editDelayed(ref min, 40).changes(ref rangeChanged) && min>=max)
                max = min + (max - before);
            "-".write(10);
            if (pegi.editDelayed(ref max, 40).changes(ref rangeChanged))
                min = Mathf.Min(min, max);

            "]".write(10);

            if (rangeChanged)
                value = Mathf.Clamp(value, min, max);

            return changed | rangeChanged;
        }
        #endif
        #endregion
    }
}