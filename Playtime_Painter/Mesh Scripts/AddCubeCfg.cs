using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Painter;

public enum BlockSetting { Any, Empty, Full }

[System.Serializable]
public class AddCubeCfg {

    public int Category = 0;
    public MegavoxelRole role = new MegavoxelRole();
    public BlockSetting[] p = new BlockSetting[27];

    public static readonly int[] cubeRotMtx = {    2,  4,  6,
                                                   -2,  0,  2,
                                                   -6, -4, -2};

    public static BlockSetting[] holder = new BlockSetting[27];

    public void AssignValue(int x, int y, int z, BlockSetting bs) {
       // Debug.Log("Assigning value " + x + " y " + y + " z " + z);
        if ((Mathf.Abs(x) > 1) || (Mathf.Abs(y) > 1) || (Mathf.Abs(z) > 1)) return;
        p[(y+1) * 9 + (z+1) * 3 + x+1] = bs;
        //Debug.Log("Success!");
    }

    public bool IsVisible() {
        return (!(p[4] == p[10] && p[10] == p[12] && p[12] == p[14] && p[14] == p[16] 
            && p[16] == p[22] && p[22] == BlockSetting.Full));
    }

    public void SetAllTo(BlockSetting to) {
        for (int i = 0; i < 27; i++)
            p[i] = to;
    }

    public void CopyFrom(AddCubeCfg from){
        for (int i = 0; i < 27; i++)
            p[i] = from.p[i];
    }

    public void Spin() {
        for (int i = 0; i < 3; i++) 
        for (int j=0; j<9; j++){
            int ind = i*9+j;
            holder[ind] = p[ind + cubeRotMtx[j]];
        }

        for (int i = 0; i < 27; i++)
            p[i] = holder[i];
    }

    public void WriteCubeCfg(AddCubeCfg a) {
        String str = "fdsf ";
        for (int i = 0; i < 27; i++)
           str+=a.p[i]+" ";
        Debug.Log(str);

            for (int i = 2; i >= 0; i--)
            {
                for (int j = 2; j >= 0; j--)
                    Debug.Log(a.p[i * 9 + j * 3] + "," + a.p[i * 9 + j * 3 + 1] + "," + a.p[i * 9 + j * 3 + 2] + " ----- " +

                        UnityEngine.Random.Range(0, 99999).ToString());
            }
    }

    static int Compare(AddCubeCfg a, AddCubeCfg b) {
      
        int sum=0;
        for (int i=0; i<27; i++){
            if (a.p[i] == b.p[i])
            {
                sum += (a.p[i] == BlockSetting.Any ? 0 : 1);
               // Debug.Log("Same at "+i+" a: "+a.p[i]);
            }
            else
                if ((a.p[i] != BlockSetting.Any) && (b.p[i] != BlockSetting.Any))
                {
                  //  Debug.Log("Failed at " + i + " a: " + a.p[i] + " b: " + b.p[i]);
                    return -1;
                }
            }
        return sum;
    }

    public int CompareWithWorld(AddCubeCfg w, ref int rot) {

      //  Debug.Log("Original: ");
       // WriteCubeCfg(w);

        AddCubeCfg tmp = Painter.MeshManager.inst.tmpCubeCfg;
       // tmp.p[13] = BlockSetting.Full;
        //p[13] = BlockSetting.Full;
        tmp.CopyFrom(this);
      


        int maxScore = -1;

        for (int i = 0; i < 4; i++) {
            int val = Compare(w, tmp);
            if (val > maxScore) {
              //  Debug.Log("Compare was successfull rot "+i);
                maxScore = val;
                rot = i;
            }
            if (i < 3) tmp.Spin();
        }


        return maxScore;
    }


    




 

}
