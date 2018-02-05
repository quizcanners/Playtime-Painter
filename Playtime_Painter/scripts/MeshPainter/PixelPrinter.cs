using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PixelPrinter : MonoBehaviour {
    public static PixelPrinter inst()
    {
        if (_inst == null)
            FindObjectOfType<PixelPrinter>();
        return _inst;
    }

    public static PixelPrinter _inst;

	// Use this for initialization
	void Awake () {
        _inst = this;
	}

    public void Clean()
    {

    }

    public void RenderSector(int fromx, int fromy, int width, int height, Vector4 values) {

    }

    public void RenderPixel(int x, int y, Vector4 values) {

    }

    // Update is called once per frame
    void Update () {
		
	}
}
