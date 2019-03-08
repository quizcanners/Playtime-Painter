using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{


    public class AdaptableBlock : MonoBehaviour
    {
        public enum MegaVoxelRole { Solid, Damaged, Decorative }
        
        public int scale;
        public AddCubeCfg config;

        public bool AddAptCollidedWith(Vector3 other, float oScale, float myScale)
        {
            var dx = other.x - transform.position.x;
            if (!(dx < myScale) || !(dx > -oScale)) return false;

            var dy = other.y - transform.position.y;
            if (!(dy < myScale) || !(dy > -oScale)) return false;

            var dz = other.z - transform.position.z;

            return dz < myScale && dz > -oScale;
        }

        /* public bool EmptySpace(Vector3 at, float scale, List<AdaptableBlock> blocks)  {
             Vector3 tmp = QcMath.FloorDiv(at, (int)scale);

             for (int i = 0; i < blocks.Count; i++)
                 if (blocks[i].gameObject.activeSelf && AddAptCollidedWith(tmp, blocks[i].scale, scale))
                         return false;

             return true;
         }*/


        public void UpdateCubeConfig(List<AdaptableBlock> blocks)
        {
            config.SetAllTo(AddCubeCfg.BlockSetting.Empty);

            foreach (var t in blocks)
                if (t.gameObject.activeSelf)
                {
                    var other = t;
                    if ((other.scale < scale) || (other == this)) continue;
                    if ((other.config.role != MegaVoxelRole.Solid) &&
                        (other.config.role != MegaVoxelRole.Damaged || config.role != MegaVoxelRole.Solid) &&
                        (other.config.role != MegaVoxelRole.Decorative || config.role != MegaVoxelRole.Damaged))
                        continue;
                   

                    var mi = transform.position;
                    var destScale = other.scale;

                    var dist =  Vector3.one* (destScale - scale) * 0.5f - mi;

                    var touchDist = (destScale + scale)/2;

                    if ((!(touchDist >= Mathf.Abs(dist.x))) || (!(touchDist >= Mathf.Abs(dist.z))) ||
                        (!(touchDist >= Mathf.Abs(dist.y)))) continue;

                    dist = other.transform.position - transform.position;//.DistanceV3To(other._dta.pos);
                    dist /= scale;
                    var wid = other.scale / scale;

                    for (var x = 0; x < wid; x++)
                    for (var y = 0; y < wid; y++)
                    for (var z = 0; z < wid; z++)
                        config.AssignValue(x + (int)dist.x, y + (int)dist.y, z + (int)dist.z, AddCubeCfg.BlockSetting.Full);
                }
        }

        public bool GetProperPiece(AddCubeCfg[] adds, int category, ref int rotation, ref int meshNumber, List<AdaptableBlock> blocks)
        {

            UpdateCubeConfig(blocks);

            if (!config.IsVisible()) return false;

            meshNumber = 0;

            var maxSimilarity = -1;

            for (var i = 0; i < adds.Length; i++)
            {
                var addY = adds[i];
                if (category != addY.category && (category != 0)) continue;

                var newRotation = 0;

                var newCoopt = config.CompareWithWorld(addY, ref newRotation);

                if (newCoopt <= maxSimilarity) continue;

                rotation = newRotation;

                meshNumber = i; maxSimilarity = newCoopt;
            }

            return true;
        }
    }
}