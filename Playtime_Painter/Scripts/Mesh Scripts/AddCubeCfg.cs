using UnityEngine;

namespace Playtime_Painter
{
    public class AddCubeCfg
    {

        public enum BlockSetting
        {
            Any,
            Empty,
            Full
        }

        public int category = 0;
        public AdaptableBlock.MegaVoxelRole role = new AdaptableBlock.MegaVoxelRole();
        public BlockSetting[] p = new BlockSetting[27];

        public static readonly int[] cubeRotMtx =
        {
            2, 4, 6,
            -2, 0, 2,
            -6, -4, -2
        };

        public static BlockSetting[] holder = new BlockSetting[27];

        public void AssignValue(int x, int y, int z, BlockSetting bs)
        {
            // Debug.Log("Assigning value " + x + " y " + y + " z " + z);
            if ((Mathf.Abs(x) > 1) || (Mathf.Abs(y) > 1) || (Mathf.Abs(z) > 1)) return;
            p[(y + 1) * 9 + (z + 1) * 3 + x + 1] = bs;
            //Debug.Log("Success!");
        }

        public bool IsVisible()
        {
            return (!(p[4] == p[10] && p[10] == p[12] && p[12] == p[14] && p[14] == p[16]
                      && p[16] == p[22] && p[22] == BlockSetting.Full));
        }

        public void SetAllTo(BlockSetting to)
        {
            for (var i = 0; i < 27; i++)
                p[i] = to;
        }

        public void CopyFrom(AddCubeCfg from)
        {
            for (var i = 0; i < 27; i++)
                p[i] = from.p[i];
        }

        public void Spin()
        {
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 9; j++)
            {
                var ind = i * 9 + j;
                holder[ind] = p[ind + cubeRotMtx[j]];
            }

            for (var i = 0; i < 27; i++)
                p[i] = holder[i];
        }

        public void WriteCubeCfg(AddCubeCfg a)
        {
            var str = "fdsf ";
            for (var i = 0; i < 27; i++)
                str += a.p[i] + " ";
            Debug.Log(str);

            for (var i = 2; i >= 0; i--)
            for (var j = 2; j >= 0; j--)
                Debug.Log(a.p[i * 9 + j * 3] + "," + a.p[i * 9 + j * 3 + 1] + "," + a.p[i * 9 + j * 3 + 2] + " ----- " +
                          UnityEngine.Random.Range(0, 99999).ToString());

        }

        private static int Compare(AddCubeCfg a, AddCubeCfg b)
        {

            var sum = 0;
            for (var i = 0; i < 27; i++)
            {
                if (a.p[i] == b.p[i])
                    sum += (a.p[i] == BlockSetting.Any ? 0 : 1);
                else if (a.p[i] != BlockSetting.Any && b.p[i] != BlockSetting.Any)
                    return -1;
            }

            return sum;
        }


        private static readonly AddCubeCfg TmpCubeCfg = new AddCubeCfg();

        public int CompareWithWorld(AddCubeCfg w, ref int rot)
        {


            TmpCubeCfg.CopyFrom(this);

            var maxScore = -1;

            for (var i = 0; i < 4; i++)
            {
                var val = Compare(w, TmpCubeCfg);
                if (val > maxScore)
                {
                    maxScore = val;
                    rot = i;
                }

                if (i < 3) TmpCubeCfg.Spin();
            }

            return maxScore;
        }
    }
}