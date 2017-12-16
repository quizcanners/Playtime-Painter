using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public static class VertAnimEditor  {
    /*
    public static void DrawAnimVertEditing() {
        ef.newLine();
        editableMeshController ips = MeshManager.inst()._target;
        if (ips == null) {
            ef.write("Not opened in mesh editor");
            return;
        }

        myShader shade = ips.getMyShader();
        if (shade.AnimatedVertices == false)
            ef.write("Shader does not support vert animation");    
        else
        {

            List<string> names = glob.edCfg.vertAnimationsNames;
            GeneratingMesh gm = glob._meshM._Mesh;

            ef.write("Cur Vert Anim: ");
            int current = ips.GetVertexAnimationNumber();
            if (current < names.Count) {
                ef.write(current + "|" + names[current]);
            }
            else ef.write("Ind="+current + " UNNAMED ");
            ef.newLine();

            SOA_limbFrame lf = ips.GetMyLimbFrame();
            bool animated = ((lf != null) && (lf.GetFlag(SOA_FrameFlags.ChangeUV_Y)));

            if (ef.select(names, gm.hasFrame, ref current)) {
                ips.SetVertexAnimationNumber(current);
                if (animated)
                    lf.Set(SOA_Int.VertexAnimationNo, current);
            }


           if (animated) {
                ef.newLine();
                ef.write("Will modify value of current animation "+ips.GetMySOAAnimation().name);
                ef.newLine();
            }
            //List<int> tagged = gm.hasAnimation.GetAllBool();

            ef.newLine();

            List<int> inds = ef.search(names);
            ef.newLine();
      
           
                for (int i = 0; i < inds.Count; i++) {
                    int ind = inds[i];
                    ef.write(names[ind]);
                    ef.toggle(ind, gm.hasFrame);
                    ef.newLine();
                }
           
                if ((ef.searchInFocus) && (inds.Count == 0) && (ef.Bttn("Add Vert Anim " + ef.searchBarInput)))
                    glob.edCfg.vertAnimationsNames.Add(ef.searchBarInput);
            
        }


        ef.newLine();
    }
    */
}
