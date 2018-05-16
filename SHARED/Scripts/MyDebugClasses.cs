using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedTools_Stuff
{

    public static class MyDebugClasses
    {


        public static void DrawCubeDebug(Color col, Vector3 piecePos, Vector3 dest)
        {
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);

            Debug.DrawLine(new Vector3(dest.x, piecePos.y, piecePos.z), new Vector3(dest.x, dest.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, dest.z), new Vector3(piecePos.x, dest.y, dest.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, dest.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, dest.y, dest.z), col);

            piecePos.y = dest.y;

            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);

        }

        public static void DrawTransformedLine(Transform tf, Vector3 from, Vector3 to, Color col)
        {
            from = tf.TransformPoint(from);
            to = tf.TransformPoint(to);
            Debug.DrawLine(from, to, col);
        }



        public static void DrawTransformedCubeDebug(this Transform tf, Color col)
        {
            Vector3 dlb = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 dlf = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 drb = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 drf = new Vector3(-0.5f, 0.5f, 0.5f);

            Vector3 ulb = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 ulf = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 urb = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 urf = new Vector3(0.5f, 0.5f, 0.5f);

            DrawTransformedLine(tf, dlb, ulb, col);
            DrawTransformedLine(tf, dlf, ulf, col);
            DrawTransformedLine(tf, drb, urb, col);
            DrawTransformedLine(tf, drf, urf, col);

            DrawTransformedLine(tf, dlb, dlf, col);
            DrawTransformedLine(tf, dlf, drf, col);
            DrawTransformedLine(tf, drf, drb, col);
            DrawTransformedLine(tf, drb, dlb, col);

            DrawTransformedLine(tf, ulb, ulf, col);
            DrawTransformedLine(tf, ulf, urf, col);
            DrawTransformedLine(tf, urf, urb, col);
            DrawTransformedLine(tf, urb, ulb, col);

        }

        public static void DrawTransformedCubeGizmo(this Transform tf, Color col)
        {

            Vector3 dlb = tf.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f));
            Vector3 dlf = tf.TransformPoint(new Vector3(-0.5f, -0.5f, 0.5f));
            Vector3 drb = tf.TransformPoint(new Vector3(-0.5f, 0.5f, -0.5f));
            Vector3 drf = tf.TransformPoint(new Vector3(-0.5f, 0.5f, 0.5f));

            Vector3 ulb = tf.TransformPoint(new Vector3(0.5f, -0.5f, -0.5f));
            Vector3 ulf = tf.TransformPoint(new Vector3(0.5f, -0.5f, 0.5f));
            Vector3 urb = tf.TransformPoint(new Vector3(0.5f, 0.5f, -0.5f));
            Vector3 urf = tf.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f));

            Gizmos.color = col;

            Gizmos.DrawLine(dlb, ulb);
            Gizmos.DrawLine(dlf, ulf);
            Gizmos.DrawLine(drb, urb);
            Gizmos.DrawLine(drf, urf);

            Gizmos.DrawLine(dlb, dlf);
            Gizmos.DrawLine(dlf, drf);
            Gizmos.DrawLine(drf, drb);
            Gizmos.DrawLine(drb, dlb);

            Gizmos.DrawLine(ulb, ulf);
            Gizmos.DrawLine(ulf, urf);
            Gizmos.DrawLine(urf, urb);
            Gizmos.DrawLine(urb, ulb);

        }


    }
}