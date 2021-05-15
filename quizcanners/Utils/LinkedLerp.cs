using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuizCanners.Lerp
{

    public interface ILinkedLerping
    {
        void Portion(LerpData ld);
        void Lerp(LerpData ld, bool canSkipLerp);
    }

    public class LerpData : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
    {
        private float _linkedPortion = 1;
        public string dominantParameter = "None";

        public void AddPortion(float value, ILinkedLerping lerp)
        {
            if (value < MinPortion)
            {
                dominantParameter = lerp.GetNameForInspector();
                MinPortion = value;
            }
        }

        public float Portion(bool skipLerp = false) => skipLerp ? 1 : _linkedPortion;

        public bool Done => Mathf.Approximately(_linkedPortion, 1);//Math.Abs(_linkedPortion - 1) < float.Epsilon*10;

        public float MinPortion
        {
            get { return _linkedPortion; }
            set { _linkedPortion = Mathf.Min(_linkedPortion, value); }
        }

        public void Reset()
        {
            _linkedPortion = 1;
            _resets++;
        }

        #region Inspector
        private int _resets;

        public string NameForPEGI
        {
            get { return dominantParameter; }
            set { dominantParameter = value; }
        }


        public int CountForInspector() => _resets;

        public void Inspect()
        {

            "Dominant Parameter".edit(ref dominantParameter).nl();

            "Reboot calls".edit(ref _resets).nl();
        }

        public void InspectInList(int ind, ref int edited)
        {
            "Lerp DP: {0} [{1}]".F(dominantParameter, _resets).write();

            if (icon.Refresh.Click("Reset stats"))
            {
                dominantParameter = "None";
                _resets = 0;
            }

            if (icon.Enter.Click())
                edited = ind;
        }

        #endregion
    }

    public static class LerpUtils
    {

        #region Float

        private static float SpeedToPortion(this float speed, float dist) =>
            Math.Abs(dist) > (float.Epsilon * 10) ? Mathf.Clamp01(speed * Time.deltaTime / Mathf.Abs(dist)) : 1;

        public static bool SpeedToMinPortion(this float speed, float dist, ref float portion)
        {

            var nPortion = speed.SpeedToPortion(dist);
            if (!(nPortion < portion))
                return (1 - portion) < float.Epsilon && dist > 0;

            portion = nPortion;

            return true;

        }

        public static bool IsLerpingBySpeed(ref float from, float to, float speed)
        {
            if (Mathf.Approximately(from, to))
                return false;

            from = Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));
            return true;
        }

        public static float LerpBySpeed(float from, float to, float speed)
            => Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));

        public static float LerpBySpeed(float from, float to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Mathf.Abs(from - to));
            return Mathf.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Double

        public static bool IsLerpingBySpeed(ref double from, double to, double speed)
        {
            if (Math.Abs(from - to) < double.Epsilon * 10)
                return false;

            double diff = to - from;

            double dist = Math.Abs(diff);

            from += diff * QcMath.Clamp01(speed * Time.deltaTime / dist);
            return true;
        }

        public static double LerpBySpeed(double from, double to, double speed)
        {
            if (Math.Abs(from - to) < double.Epsilon * 10)
                return from;

            double diff = to - from;

            double dist = Math.Abs(diff);

            return from + diff * QcMath.Clamp01(speed * Time.deltaTime / dist);
        }

        #endregion

        #region Vectors & Color

        public static bool IsLerpingBySpeed(ref Vector2 from, Vector2 to, float speed)
        {
            if (from == to)
                return false;

            from = Vector2.LerpUnclamped(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));
            return true;
        }

        public static Vector2 LerpBySpeed(this Vector2 from, Vector2 to, float speed) =>
            Vector2.LerpUnclamped(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));

        public static Vector2 LerpBySpeed(this Vector2 from, Vector2 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector2.Distance(from, to));
            return Vector2.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpBySpeed(this Vector3 from, Vector3 to, float speed) =>
            Vector3.LerpUnclamped(from, to, speed.SpeedToPortion(Vector3.Distance(from, to)));

        public static bool IsLerpingBySpeed(ref Vector3 from, Vector3 to, float speed)
        {
            if (from == to)
                return false;

            from = Vector3.LerpUnclamped(from, to, speed.SpeedToPortion(Vector3.Distance(from, to)));
            return true;
        }

        public static Vector3 LerpBySpeed(this Vector3 from, Vector3 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector3.Distance(from, to));
            return Vector3.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpBySpeed_DirectionFirst(this Vector3 from, Vector3 to, float speed)
        {

            const float precision = float.Epsilon * 10;

            var fromMagn = from.magnitude;
            var toMagn = to.magnitude;

            float dist = Vector3.Distance(from, to);

            float pathThisFrame = speed * Time.deltaTime;

            if (pathThisFrame >= dist)
                return to;

            if (fromMagn * toMagn < precision)
                return from.LerpBySpeed(to, speed);

            var toNormalized = to.normalized;

            var targetDirection = toNormalized * (fromMagn + toMagn) * 0.5f;

            var toTargetDirection = targetDirection - from;

            float rotDiffMagn = toTargetDirection.magnitude;

            if (pathThisFrame > rotDiffMagn)
            {

                pathThisFrame -= rotDiffMagn;

                from = targetDirection;

                var newDiff = to - from;

                from += newDiff.normalized * pathThisFrame;

            }
            else
                from += toTargetDirection * pathThisFrame / rotDiffMagn;


            return from;
        }

        public static Vector4 LerpBySpeed(this Vector4 from, Vector4 to, float speed) =>
            Vector4.LerpUnclamped(from, to, speed.SpeedToPortion(Vector4.Distance(from, to)));

        public static Vector4 LerpBySpeed(this Vector4 from, Vector4 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector4.Distance(from, to));
            return Vector4.LerpUnclamped(from, to, portion);
        }

        public static Quaternion LerpBySpeed(this Quaternion from, Quaternion to, float speedInDegrees) =>
            Quaternion.LerpUnclamped(from, to, speedInDegrees.SpeedToPortion(Quaternion.Angle(from, to)));

        public static Quaternion LerpBySpeed(this Quaternion from, Quaternion to, float speedInDegrees, out float portion)
        {
            portion = speedInDegrees.SpeedToPortion(Quaternion.Angle(from, to));
            return Quaternion.LerpUnclamped(from, to, portion);
        }

        public static float DistanceRgb(this Color col, Color other)
            =>
                (Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b));

        public static float DistanceRgba(this Color col, Color other) =>
                ((Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b)) * 0.33f +
                 Mathf.Abs(col.a - other.a));

        public static float DistanceRgba(this Color col, Color other, ColorMask mask) =>
             (mask.HasFlag(ColorMask.R) ? Mathf.Abs(col.r - other.r) : 0) +
             (mask.HasFlag(ColorMask.G) ? Mathf.Abs(col.g - other.g) : 0) +
             (mask.HasFlag(ColorMask.B) ? Mathf.Abs(col.b - other.b) : 0) +
             (mask.HasFlag(ColorMask.A) ? Mathf.Abs(col.a - other.a) : 0);

        public static Color LerpBySpeed(this Color from, Color to, float speed) =>
            Color.LerpUnclamped(from, to, speed.SpeedToPortion(from.DistanceRgb(to)));

        public static Color LerpRgb(this Color from, Color to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(from.DistanceRgb(to));
            to.a = from.a;
            return Color.LerpUnclamped(from, to, portion);
        }

        public static Color LerpRgba(this Color from, Color to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(from.DistanceRgba(to));
            return Color.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Components

        public static bool IsLerpingAlphaBySpeed(this CanvasGroup grp, float alpha, float speed)
        {
            if (!grp) return false;

            var current = grp.alpha;

            if (IsLerpingBySpeed(ref current, alpha, speed))
            {
                grp.alpha = current;
                return true;
            }

            return false;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this List<T> graphicList, float alpha, float speed) where T : Graphic
        {

            if (graphicList.IsNullOrEmpty()) return false;

            var changing = false;

            foreach (var i in graphicList)
                changing |= i.IsLerpingAlphaBySpeed(alpha, speed);

            return changing;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this T img, float alpha, float speed) where T : Graphic
        {
            if (!img) return false;

            var changing = false;

            var col = img.color;
            col.a = LerpBySpeed(col.a, alpha, speed);

            img.color = col;
            changing |= Mathf.Approximately(col.a, alpha) == false;

            return changing;
        }

        public static bool IsLerpingRgbBySpeed<T>(this T img, Color target, float speed) where T : Graphic
        {
            bool changing = false;

            if (img)
            {
                img.color = img.color.LerpRgb(target, speed, out float portion);

                changing = portion < 1;
            }

            return changing;
        }

        public static bool IsLerpingBySpeed_Volume(this AudioSource src, float target, float speed)
        {
            if (!src)
                return false;

            var vol = src.volume;

            if (IsLerpingBySpeed(ref vol, target, speed))
            {
                src.volume = vol;
                return true;
            }

            return false;
        }

        #endregion


        public static void Update(this LerpData ld, ILinkedLerping target, bool canSkipLerp)
        {
            ld.Reset();
            target.Portion(ld);
            target.Lerp(ld, canSkipLerp: canSkipLerp);
        }

        public static void SkipLerp<T>(this T obj, LerpData ld) where T : ILinkedLerping
        {
            ld.Reset();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void SkipLerp<T>(this T obj) where T : ILinkedLerping
        {
            var ld = new LerpData();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void Portion<T>(this T[] list, LerpData ld) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Length - 1; i >= 0; i--)
                {

                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }
            }
            else for (int i = list.Length - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (e != null)
                        e.Portion(ld);
                }
        }

        public static void Portion<T>(this List<T> list, LerpData ld) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (e != null)
                        e.Portion(ld);
                }

        }

        public static void Lerp<T>(this T[] array, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = array.Length - 1; i >= 0; i--)
                {

                    var e = array[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }
            }
            else for (int i = array.Length - 1; i >= 0; i--)
                {
                    var e = array[i];
                    if (e != null)
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }
        }

        public static void Lerp<T>(this List<T> list, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (e != null)
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }

        }

    }


}