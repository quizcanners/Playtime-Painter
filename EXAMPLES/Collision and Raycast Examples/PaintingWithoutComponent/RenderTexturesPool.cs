using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class RenderTexturesPool : MonoBehaviour {

    public static RenderTexturesPool _inst;


    public int size = 256;
 
    public List<RenderTexture> list = new List<RenderTexture>();


    public RenderTexture GetOne() {
        if (list.Count > 0)
            return list.RemoveLast();
        else return new RenderTexture(size, size, 0);
    }

    public void ReturnOne(RenderTexture rt) {
        list.Add(rt);
     
    }

    void OnEnable() {
        _inst = this;
        size = Mathf.ClosestPowerOfTwo(size);
    }

}

