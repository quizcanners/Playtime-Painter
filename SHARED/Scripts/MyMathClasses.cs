using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff
{

    public static class MyMath
    {

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

        public static Vector3 Lerp(Vector3 from, Vector3 to, float speed)
        {

            float dist = Vector3.Distance(from, to);
            return Vector3.Lerp(from, to, dist > 0 ? Mathf.Clamp01(speed*Time.deltaTime / dist) : 1);

        }

        public static Vector2 Lerp(this Vector2 from, Vector2 to, float speed)
        {

            float dist = Vector2.Distance(from, to);
            return Vector2.Lerp(from, to, dist > 0 ? Mathf.Clamp01(speed*Time.deltaTime / dist) : 1);

        }

        public static void Lerp(ref float from, float to, float speed)
        {
            float dist = Mathf.Abs(from - to);
            from = Mathf.Lerp(from, to, dist > 0 ? Mathf.Clamp01(speed*Time.deltaTime / dist) : 1);
        }

        public static Quaternion Lerp(this Quaternion from, Quaternion to, float speed)
        {
            float dist = Quaternion.Angle(from, to);
            float portion = (dist > 0) ? speed*Time.deltaTime / dist : 1;
            return Quaternion.Lerp(from, to, portion);
        }


        public static bool isAcute(float a, float b, float c)
        {
            if (c == 0) return true;
            float longest = Mathf.Max(a, b);
            longest *= longest;
            float side = Mathf.Min(a, b);


            return (longest > (c * c + side * side));

        }


        public static bool isPointOnLine(float a, float b, float line, float percision)
        {
            percision *= line;
            float dist;

            if (isAcute(a, b, line)) dist = Mathf.Min(a, b);
            else
            {
                float s = (a + b + line) / 2;
                float h = 4 * s * (s - a) * (s - b) * (s - line) / (line * line);
                dist = Mathf.Sqrt(h);
            }

            return dist < percision;

            // return ((line > pnta) && (line > pntb) && ((pnta + pntb) < line + percision));

        }

        public static bool isPointOnLine(Vector3 a, Vector3 b, Vector3 point, float percision)
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
    public class myIntVec2
    {
        public int x;
        public int y;

        public int max { get { return x > y ? x : y; } }

        public override string ToString()
        {
            return "x:" + x + " y:" + y;
        }

        public void Clamp(int min, int max)
        {
            x = Mathf.Clamp(x, min, max);
            y = Mathf.Clamp(y, min, max);
        }

        public void Clamp(int min, myIntVec2 max)
        {
            x = Mathf.Clamp(x, min, max.x);
            y = Mathf.Clamp(y, min, max.y);
        }

        public myIntVec2 MultiplyBy(int val)
        {
            x *= val;
            y *= val;
            return this;
        }

        public myIntVec2 Subtract(myIntVec2 other)
        {
            x -= other.x;
            y -= other.y;
            return this;
        }

        public Vector2 ToFloat()
        {
            return new Vector2(x, y);
        }

        public myIntVec2 From(Vector2 vec)
        {
            x = (int)vec.x;
            y = (int)vec.y;
            return this;
        }

        public myIntVec2(myIntVec2 other)
        {
            x = other.x;
            y = other.y;
        }

        public myIntVec2(float nx, float ny)
        {
            x = (int)nx;
            y = (int)ny;
        }

        public myIntVec2(int nx, int ny)
        {
            x = nx;
            y = ny;
        }

        public myIntVec2(int val)
        {
            x = y = val;
        }

        public myIntVec2()
        {

        }
    }
    [Serializable]
    public class myIntVec3
    {
        public int x;
        public int y;
        public int z;


        public Vector3 Lerp(Vector3 current, float speed, out float dist)
        {
            float xx = (float)x - current.x;
            float yy = (float)y - current.y;
            float zz = (float)z - current.z;

            dist = Mathf.Sqrt(zz * zz + yy * yy + xx * xx);

            float portion = (dist > 0) ? speed * Time.deltaTime / dist : 1;
            return Vector3.Lerp(current, new Vector3(x, y, z), portion);

        }


        public Vector3 atomicLerp(Vector3 current, out bool finished)
        {
            int xx = x - (int)current.x;
            int yy = y - (int)current.y;
            int zz = z - (int)current.z;

            finished = ((zz == yy) && (yy == xx) && (xx == 0));

            return (current + new Vector3(xx != 0 ? (xx / Mathf.Abs(xx)) : 0
                                        , yy != 0 ? (yy / Mathf.Abs(yy)) : 0
                                        , zz != 0 ? (zz / Mathf.Abs(zz)) : 0));
        }

        public Vector3 atomicLerp(Vector3 current)
        {
            int xx = x - (int)current.x;
            int yy = y - (int)current.y;
            int zz = z - (int)current.z;

            return (current + new Vector3(xx != 0 ? (xx / Mathf.Abs(xx)) : 0
                                        , yy != 0 ? (yy / Mathf.Abs(yy)) : 0
                                        , zz != 0 ? (zz / Mathf.Abs(zz)) : 0));
        }

        public bool Compare(myIntVec3 other)
        {

            return ((x == other.x) && (y == other.y) && (z == other.z));

        }

        public void CopyFrom(myIntVec3 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
        }

        public Vector3 ToV3()
        {
            return new Vector3(x, y, z);
        }
        public void FromV3(Vector3 v)
        {
            x = (int)v.x;
            y = (int)v.y;
            z = (int)v.z;
        }
        public void setTo(int size)
        {
            x = y = z = size;
        }
        public void zero()
        {
            x = y = z = 0;
        }

        public myIntVec3 DeepCopy()
        {
            myIntVec3 tmp = new myIntVec3();
            tmp.x = x;
            tmp.y = y;
            tmp.z = z;
            return tmp;
        }

        public myIntVec3(int val)
        {
            x = y = z = val;
        }
        public myIntVec3()
        {

        }

    }

    /*
    [Serializable]
    public class myVec2 : abstract_STD {
        public float x;
        public float y;


        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("x", x);
            cody.Add("y", y);

            return cody;
        }

        public override void PEGI() { }
        public override void Decode(string tag, string data) {
            switch (tag) {
                case "x": x = data.ToFloat(); break;
                case "y": y = data.ToFloat(); break;
            }
        }

        public const string storyTag = "mv2";
        public override string getDefaultTagName() { return storyTag; }


        public static ArrayManager<myVec2> array = new ArrayManager<myVec2>();

        public override string ToString()
        {
            return "X:" + x + "Y:" + y;
           // return base.ToString();
        }

        public Vector2 ToV2()
        {
            return new Vector2(x, y);
        }

        public Vector4 ToV4()
        {
            return new Vector4(x, y,0,0);
        }

        public void FromV2(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public void CopyFrom(myVec2 from)
        {
            x = from.x;
            y = from.y;
        }

        public myVec2()
        {
            x = 0;
            y = 0;
        }

        public myVec2(myVec2 v)
        {
            x = v.x;
            y = v.y;
        }

        public myVec2(float nx, float ny) {
            x = nx;
            y = ny;
        }

        public myVec2(Vector2 v)
        {
            FromV2(v);
        }

        public void Add(Vector2 v2)
        {
            x += v2.x;
            y += v2.y;
        }
    }
    [Serializable]
    public class myVec3 : CanCopy<myVec3> {
        public float x;
        public float y;
        public float z;

        public static ArrayManager<myVec3> array = new ArrayManager<myVec3>();
        public ArrayManager<myVec3> getArrMan()
        {
            return array;
        }

        public void TransferToSmallerScale(myVec3 o, float coefficient) {
            float d = x % 1;
            o.x += d * coefficient;
            x -= d;

            d = y % 1;
            o.y += d * coefficient;
            y -= d;

            d = z % 1;
            o.z += d * coefficient;
            z -= d;
        }

        public void TransferToLargerScale(myVec3 o, float coefficient) {
            float d = (int)(x / coefficient);
            x -= d * coefficient;
            o.x += d;

            d = (int)(y / coefficient);
            y -= d * coefficient;
            o.y += d;

            d = (int)(z / coefficient);
            z -= d * coefficient;
            o.z += d;
        }

        public override string ToString()
        {
            return "(" + x + "," + y + ","+z+")";
        }

        public Color ToColor(float alpha) {
            return new Color(x, y, z, alpha);

        }

        public myVec3 DeepCopy()
        {
            return new myVec3(this);
        }

        public void Add(myVec3 v3, float portion)
        {
            x += v3.x * portion;
            y += v3.y * portion;
            z += v3.z * portion;
        }

        public void Add(myVec3 v3)
        {
            x += v3.x;
            y += v3.y;
            z += v3.z;
        }

        public void Add(Vector3 v3)
        {
            x += v3.x;
            y += v3.y;
            z += v3.z;
        }

        public void Subtract(Vector3 v3)
        {
            x -= v3.x;
            y -= v3.y;
            z -= v3.z;
        }

        public void Divide(float by)
        {
            x /= by;
            y /= by;
            z /= by;
        }

        public void Normalize()
        {
            float sum = Mathf.Sqrt(x * x + y * y + z * z);
            x /= sum;
            y /= sum;
            z /= sum;
        }

        public Vector3 DistanceV3To(myVec3 other) {
            float dx = other.x - x;
            float dy = other.y - y;
            float dz = other.z - z;
            return new Vector3(dx, dy, dz); //Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float DistanceTo(myVec3 other)
        {
            float dx = other.x - x;
            float dy = other.y - y;
            float dz = other.z - z;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public void LerpTo(myVec3 To, float portion)
        {
            //Vector3 tmp = new Vector3();
            x += (To.x - x) * portion;
            y += (To.y - y) * portion;
            z += (To.z - z) * portion;
        }

        public Vector3 GetLerpV3(myVec3 To, float portion)
        {
            Vector3 tmp = new Vector3();
            tmp.x = x + (To.x - x) * portion;
            tmp.y = y + (To.y - y) * portion;
            tmp.z = z + (To.z - z) * portion;
            return tmp;
        }

        public myVec3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public myVec3(int size)
        {
            x = y = z = size;
        }
        public myVec3(myVec3 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
        }
        public myVec3()
        {
            return;
        }

        public Vector3 ToV3()
        {
            return new Vector3(x, y, z);
        }
        public void FromV3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        public void CopyFrom(myVec3 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
        }
        public void CopyFrom(Vector3 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
        }

        public void Set(float to)
        {
            x = y = z = to;
        }

        public void zero()
        {
            x = y = z = 0;
        }


        public void PEGI() {
            pegi.write("X", 20);
            pegi.edit(ref x);
            pegi.newLine();
            pegi.write("Y", 20);
            pegi.edit(ref y);
            pegi.newLine();
            pegi.write("Z", 20);
            pegi.edit(ref z);
            pegi.newLine();

        }

    }
    */

    [Serializable]
    public class myRGB
    {
        public float r;
        public float g;
        public float b;

        public Color ToColor(float alpha)
        {
            return new Color(r, g, b, alpha);
        }

        public myRGB()
        {
            r = g = b = 1;
        }

        public myRGB(float nr, float ng, float nb)
        {
            r = nr;
            g = ng;
            b = nb;
        }

    }

    [Serializable]
    public class myVec4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static ArrayManager<myVec4> array = new ArrayManager<myVec4>();

        public void Add(Vector3 v3)
        {
            x += v3.x;
            y += v3.y;
            z += v3.z;
        }

        public void From(linearColor c, BrushMask bm)
        {
            if ((bm & BrushMask.R) != 0)
                x = c.r;
            if ((bm & BrushMask.G) != 0)
                y = c.g;
            if ((bm & BrushMask.B) != 0)
                z = c.b;
            if ((bm & BrushMask.A) != 0)
                w = c.a;
        }

        public void From(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public void From(Vector4 q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion toQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public Vector4 ToV4()
        {
            return new Vector4(x, y, z, w);
        }

        public void CopyFrom(myVec4 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
            w = from.w;
        }

        public myVec4(Vector4 from)
        {
            x = from.x;
            y = from.y;
            z = from.z;
            w = from.w;
        }

        public void Set(float nx, float ny, float nz, float nw)
        {
            x = nx;
            y = ny;
            z = nz;
            w = nw;
        }

        public myVec4(float nx, float ny, float nz, float nw)
        {
            Set(nx, ny, nz, nw);
        }

        public myVec4()
        {
            x = y = z = w = 0;
        }

        public void NormalizeXYZ()
        {
            float sum = Mathf.Sqrt(x * x + y * y + z * z);
            x /= sum;
            y /= sum;
            z /= sum;
        }
    }


}