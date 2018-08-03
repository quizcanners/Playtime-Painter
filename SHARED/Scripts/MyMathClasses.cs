using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff
{

    public static class MyMath
    {

        public static double Miliseconds_To_Seconds(this double interval) => (interval*0.001);

        public static double Seconds_To_Miliseconds(this double interval) => (interval * 1000);

        public static float Miliseconds_To_Seconds(this float interval) => (interval * 0.001f);

        public static float Seconds_To_Miliseconds(this float interval) => (interval * 1000);

        public static float ClampZeroTo (this float value, float Max) {
            value = Mathf.Max(0, Mathf.Min(value, Max-1));
            return value;
        }

        public static int ClampZeroTo(this int value, int Max)
        {
            value = Mathf.Max(0, Mathf.Min(value, Max - 1));
            return value;
        }

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees);
            float cos = Mathf.Cos(degrees);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
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

        static float SpeedToPortion (this float speed, float dist) => dist > 0 ? Mathf.Clamp01(speed* Time.deltaTime / dist) : 1;

        public static Quaternion Lerp(this Quaternion from, Quaternion to, float speed) => Quaternion.Lerp(from, to, speed.SpeedToPortion(Quaternion.Angle(from, to)));

        public static Quaternion Lerp(this Quaternion from, Quaternion to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Quaternion.Angle(from, to));
            return Quaternion.Lerp(from, to, portion);
        }

        public static Vector4 Lerp(Vector4 from, Vector4 to, float speed) => Vector4.Lerp(from, to, speed.SpeedToPortion(Vector4.Distance(from, to)));

        public static Vector4 Lerp(Vector4 from, Vector4 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector4.Distance(from, to));
            return Vector4.Lerp(from, to, portion);
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float speed) => Vector3.Lerp(from, to, speed.SpeedToPortion(Vector3.Distance(from, to)));
        
        public static Vector3 Lerp(Vector3 from, Vector3 to, float speed, out float portion) {
            portion = speed.SpeedToPortion(Vector3.Distance(from, to)); 
            return Vector3.Lerp(from, to, portion);
        }

        public static Vector2 Lerp(this Vector2 from, Vector2 to, float speed) => Vector2.Lerp(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));

        public static Vector2 Lerp(this Vector2 from, Vector2 to, float speed, out float portion) {
            portion = speed.SpeedToPortion(Vector3.Distance(from, to));
            return Vector3.Lerp(from, to, portion);
        }

        public static bool Lerp(ref float from, float to, float speed)
        {
            if (from == to)
                return false;

            from = Mathf.Lerp(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));
            return true;
        }

        public static bool Lerp(ref float from, float to, float speed, out float portion)
        {
            if (from == to) {
                portion = 1;
                return false;
            }

            portion = speed.SpeedToPortion(Mathf.Abs(from-to));
            from = Mathf.Lerp(from, to, portion);

            return true;
        }

        public static float Lerp(float from, float to, float speed) => Mathf.Lerp(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));

        public static float Lerp(float from, float to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Mathf.Abs(from - to));
            return Mathf.Lerp(from, to, portion);
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

    }

    [Serializable]
    public class MyIntVec2
    {
        public int x;
        public int y;

        public int Max { get { return x > y ? x : y; } }

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

        public MyIntVec2()
        {

        }
    }

}