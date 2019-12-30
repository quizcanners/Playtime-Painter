using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class TexturesPool : PainterSystemMono, IPEGI  {

        public static TexturesPool inst;
        public static TexturesPool Inst { get {
            if (!inst && !ApplicationIsQuitting) {
                var obj = new GameObject().AddComponent<TexturesPool>(); 
                    obj.gameObject.name = "Textures Pool";
            }

            return inst; } }

        public int width = 256;

        public int texturesCreated = 0;

        public bool nonColorData = true;

        [NonSerialized] private readonly List<RenderTexture> _rtList = new List<RenderTexture>();
        [NonSerialized] private readonly List<Texture2D> _t2DList = new List<Texture2D>();

        public Texture2D GetTexture2D()
        {
            if (_t2DList.Count > 0)
                return _t2DList.RemoveLast();

            texturesCreated++;

            return new Texture2D(width, width, TextureFormat.ARGB32, false, linear: nonColorData) {
                wrapMode = TextureWrapMode.Repeat,
                name = "Tex2D_fromPool"
            };

          

        }

        public RenderTexture GetRenderTexture() {
            if (_rtList.Count > 0)
                return _rtList.RemoveLast();

            texturesCreated++;

           return new RenderTexture(width, width, 0) {
                wrapMode = TextureWrapMode.Repeat,
                useMipMap = false,
                name = "RenderTexture_fromPool"
            };
            
        }

        public void ReturnOne(RenderTexture rt)
        {
            _rtList.Add(rt);
        }

        private void OnEnable()
        {
            inst = this;
            width = Mathf.ClosestPowerOfTwo(width);
        }

        public override bool Inspect()
        {
            var changed = false;

            "Data (Non Color) Texture".toggleIcon(ref nonColorData).nl();

            return changed;
        }
    }
}

