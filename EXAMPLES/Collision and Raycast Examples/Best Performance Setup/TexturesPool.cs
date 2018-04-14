using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class TexturesPool : MonoBehaviour {

        public static TexturesPool _inst;
        public static TexturesPool inst { get { if (_inst == null) new GameObject().AddComponent<TexturesPool>().gameObject.name = "Textures Pool"; return _inst; } }

        public int width = 256;

        [NonSerialized]
        public List<RenderTexture> rtList = new List<RenderTexture>();
        [NonSerialized]
        public List<Texture2D> t2dList = new List<Texture2D>();

        public Texture2D GetTexture2D()
        {
            if (t2dList.Count > 0)
                return t2dList.RemoveLast();
            else
            {
                var rt = new Texture2D(width, width, TextureFormat.ARGB32, false);
                rt.wrapMode = TextureWrapMode.Repeat;
                rt.name = "Tex2D_fromPool";
                return rt;
            }
        }

        public RenderTexture GetRenderTexture() {
            if (rtList.Count > 0)
                return rtList.RemoveLast();
            else {
                var rt = new RenderTexture(width, width, 0);
                rt.wrapMode = TextureWrapMode.Repeat;
                rt.useMipMap = false;
                rt.name = "RenderTexture_fromPool";
                return rt;
            }
        }

        public void ReturnOne(RenderTexture rt)
        {
            rtList.Add(rt);
        }

        public void ReturnOne(Texture2D tex)
        {
            t2dList.Add(tex);
        }

        void OnEnable()
        {
            _inst = this;
            width = Mathf.ClosestPowerOfTwo(width);
        }
    }
}

