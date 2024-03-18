using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{

    public static class TracedVolume
    {
        public static readonly ShaderProperty.FloatFeature VOLUME_VISIBILITY = new(name: "qc_VolumeAlpha", featureDirective: "qc_GOT_VOLUME");

        public static bool HasValidData
        {
            get => VOLUME_VISIBILITY.latestValue > 0.1f;
            set
            {
                if (value)
                    VOLUME_VISIBILITY.GlobalValue = 1;
                else
                    VOLUME_VISIBILITY.GlobalValue = 0;
            }
        }

        public static Vector3 GetDiscretePosition(Vector3 position, float size, out float scaledChunks, int segmentSize = 32)
        {
            segmentSize = Mathf.Max(1, segmentSize);
            scaledChunks = segmentSize * size;
            Vector3 currentPosition = position + (0.5f * scaledChunks * Vector3.one);
            Vector3 pos = Vector3Int.FloorToInt(currentPosition / scaledChunks);
            pos *= scaledChunks;

            return pos;
        }

        public static void OnSceneGUI(Vector3 center, float Width, float Height, float Size) 
        {
            //Vector3 GetDiscretePosition(Vector3 position, float size, out float scaledChunks, int segmentSize = 32) 

            center.y += Height * 0.5f * Size;

            var w = Width;
           //transform.position;
                                        //   var hOff = Height * 0.5f * Size;
                                        // center.y += hOff;
            var size = new Vector3(w, Height, w) * Size;

            pegi.Gizmo.DrawCube(center, size, Color.blue);

          //  pegi.Handle.DrawWireCube(center, Quaternion.identity, size);


        }
    }
}
