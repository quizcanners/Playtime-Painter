using UnityEngine;

namespace PainterTool
{

    public static class VolumeTexture
    {
        public static Vector3 GetDiscretePosition(Vector3 position, float size, out float scaledChunks, int segmentSize = 32)
        {
            segmentSize = Mathf.Max(1, segmentSize);
            scaledChunks = segmentSize * size;
            Vector3 currentPosition = position + (0.5f * scaledChunks * Vector3.one);
            Vector3 pos = Vector3Int.FloorToInt(currentPosition / scaledChunks);
            pos *= scaledChunks;

            return pos;
        }


    }
}
