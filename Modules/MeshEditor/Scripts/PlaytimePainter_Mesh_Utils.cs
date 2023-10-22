using QuizCanners.Utils;
using System;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public static partial class MeshPainting
    {
        public static PainterComponent target;
        public static Transform targetTransform;
        public static Vector3 onGridLocal;
        public static Vector3 collisionPosLocal;
        public static Vector3 LatestMouseRaycastHit;
        public static Vector3 LatestMouseToGridProjection;

        public static void UpdateLocalSpaceMousePosition()
        {
            if (!target) return;

            onGridLocal = targetTransform.InverseTransformPoint(LatestMouseToGridProjection);
            collisionPosLocal = targetTransform.InverseTransformPoint(LatestMouseRaycastHit);
        }

        public static GridNavigator Grid
        {
            get
            {
                var srv = Singleton.Get<GridNavigator>();

                if (srv) 
                    return srv;

                srv = Painter.Camera.GetComponentInChildren<GridNavigator>();

                if (srv)
                    return srv;

                try
                {
                    var prefab = Resources.Load(SO_PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/grid") as GameObject;
                    srv = GameObject.Instantiate(prefab, Painter.Camera.transform).GetComponent<GridNavigator>();
                    //  srv.transform.parent = Painter.Camera.transform;
                    srv.name = "grid";

                    srv.gameObject.hideFlags = HideFlags.DontSave;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return srv;
            }
        }

        public static GridPlane CurrentPlane = GridPlane.xz;

        public static Vector3 ProjectToGrid(Vector3 src)
        {
            var pos = LatestMouseToGridProjection;

            switch (CurrentPlane)
            {
                case GridPlane.xy:
                    return new Vector3(src.x, src.y, pos.z);
                case GridPlane.xz:
                    return new Vector3(src.x, pos.y, src.z);
                case GridPlane.zy:
                    return new Vector3(pos.x, src.y, src.z);
                default:
                    break;
            }

            return Vector3.zero;
        }
    }

    public enum GridPlane
    {
        xz = 0,
        xy = 1,
        zy = 2
    }
}
