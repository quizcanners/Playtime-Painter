using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Painter;

public class AdaptableBlock : MonoBehaviour {

    public int scale;
    public AddCubeCfg config;

    public bool AddAptCollidedWith(Vector3 other, float oscale, float myScale)
    {
        float dx = other.x - transform.position.x;
        if ((dx < myScale) && (dx > -oscale))
        {
            float dy = other.y - transform.position.y;
            if ((dy < myScale) && (dy > -oscale))
            {
                float dz =  other.z - transform.position.z;
                if ((dz < myScale) && (dz > -oscale)) return true;
            }
        }
        return false;
    }

    public bool EmptySpace(Vector3 at, float scale, List<AdaptableBlock> blocks)  {
        Vector3 tmp = MyMath.FloorDiv(at, (int)scale);

        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i].gameObject.activeSelf)
            {
                AdaptableBlock other = blocks[i];
                if (AddAptCollidedWith(tmp, other.scale, scale))
                    return false;
            }


        return true;
    }


    public void UpdateCubeConfig(List<AdaptableBlock> blocks)
    {
        config.SetAllTo(BlockSetting.Empty);

        for (int i = 0; i < blocks.Count; i++)
            if (blocks[i].gameObject.activeSelf)
            {
                AdaptableBlock other = blocks[i];
                if ((other.scale >= scale) && (other != this))
                {

                    if ((other.config.role == MegavoxelRole.Solid) ||
                    (other.config.role == MegavoxelRole.Damaged && config.role == MegavoxelRole.Solid) ||
                    (other.config.role == MegavoxelRole.Decorative && config.role == MegavoxelRole.Damaged))
                    {

                        Vector3 dist = new Vector3();
                        Vector3 dest = other.transform.position;
                        Vector3 mi = transform.position;
                        int destScale = other.scale;
                        dist.x =  (dest.x + destScale / 2) - (mi.x + scale / 2);
                        dist.z =  (dest.z + destScale / 2) - (mi.z + scale / 2);
                        dist.y = (dest.y + destScale / 2) - (mi.y + scale / 2);
                        int touchDist = (destScale + scale) / 2;
                        if ((touchDist >= Mathf.Abs(dist.x)) && (touchDist >= Mathf.Abs(dist.z)) && (touchDist >= Mathf.Abs(dist.y)))
                        {

                            dist = other.transform.position - transform.position;//.DistanceV3To(other._dta.pos);
                            dist /= scale;
                            int wid = other.scale / scale;

                            for (int x = 0; x < wid; x++)
                                for (int y = 0; y < wid; y++)
                                    for (int z = 0; z < wid; z++) {
                                        config.AssignValue(x + (int)dist.x, y + (int)dist.y, z + (int)dist.z, BlockSetting.Full);
                                    }
                        }
                    }
                }
            }
    }

    public bool GetPropperPiece(AddCubeCfg[] adds, int category, ref int rotation, ref int meshNumber, List<AdaptableBlock> blocks)
    {

        UpdateCubeConfig(blocks);

        if (config.IsVisible())
        {
            meshNumber = 0;
            int maxCoopt = -1;

            for (int i = 0; i < adds.Length; i++)
            {
                AddCubeCfg addy = adds[i];
                if (((category == addy.Category) || (category == 0)))
                {

                    int newRotation = 0;
                    int newCoopt = config.CompareWithWorld(addy, ref newRotation);
                    if (newCoopt > maxCoopt)
                    {
                        rotation = newRotation;
                        meshNumber = i; maxCoopt = newCoopt;
                        // Debug.Log("Success at " + bestRotation); 
                    }
                }
            }

            return true;
        }
        return false;
    }
}
