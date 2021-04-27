using PainterTool.CameraModules;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{
    public static partial class Painter
    {
        internal static readonly PlaytimePainter_BrushMeshGenerator BrushMeshGenerator = new();
        internal static readonly MeshEditorManager MeshManager = new();
        internal static readonly TextureDownloadManager DownloadManager = new();

        public static Singleton_PainterCamera Camera
        {
            get
            {
                var cam = Singleton.Get<Singleton_PainterCamera>();
               // if (!cam)
                    //TryInstanciateCamera(out cam);

                return cam;
            }
        }

        public static bool IsLinearColorSpace
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;
#else
                      return Data.isLineraColorSpace;
#endif
            }
            set
            {
                if (Data)
                    Data.isLineraColorSpace = value;
            }
        }

        internal static PainterComponent FocusedPainter => PainterComponent.selectedInPlaytime;


        public static SO_PainterDataAndConfig Data
        {
            get
            {
                var painterCam = Camera;

                if (!painterCam)
                    return null;

                if (!painterCam._triedToFindPainterData && !painterCam.dataHolder)
                {

                    var allConfigs = Resources.LoadAll<SO_PainterDataAndConfig>("");

                    painterCam.dataHolder = allConfigs.TryGet(0);

                    if (!painterCam.dataHolder)
                        painterCam._triedToFindPainterData = true;
                }

                return painterCam.dataHolder;
            }
        }

        public static bool TryInstanciateCamera(out Singleton_PainterCamera isnt)
        {            
            var go = Resources.Load(SO_PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/" + SO_PainterDataAndConfig.PainterCameraName) as GameObject;
            isnt = Object.Instantiate(go).GetComponent<Singleton_PainterCamera>();
            isnt.name = SO_PainterDataAndConfig.PainterCameraName;
            CameraModuleBase.RefreshModules();
            return isnt;
        }

        internal static Singleton_DepthProjectorCamera GetOrCreateProjectorCamera()
        {
            var inst = Singleton.Get<Singleton_DepthProjectorCamera>();
            if (inst)
                return inst;

            if (!Singleton.Get<Singleton_DepthProjectorCamera>())
                inst = QcUnity.Instantiate<Singleton_DepthProjectorCamera>();

            return inst;
        }
    }
}
