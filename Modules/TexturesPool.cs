using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class TexturesPool : PainterSystemMono, IPEGI  {

        public static TexturesPool inst;
        public static TexturesPool ForceInstance
        {
            get
            {
                if (!inst && !ApplicationIsQuitting) {
                    inst = new GameObject().AddComponent<TexturesPool>();
                    inst.gameObject.name = "Textures Pool";
                }

                return inst;
            }
        }

        public int width = 256;

        public int texturesCreated;

        public bool nonColorData = true;

        [NonSerialized] private List<RenderTexture> _rtList = new List<RenderTexture>();
        [NonSerialized] private List<Texture2D> _t2DList = new List<Texture2D>();

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

        private RenderTexture CreateRenderTexture()
        {
            texturesCreated++;

            return new RenderTexture(width, width, 0) {
                wrapMode = TextureWrapMode.Repeat,
                useMipMap = false,
                name = "RenderTexture_fromPool"
            };
    }

        public RenderTexture GetRenderTexture(Color textureColor)
        {
            RenderTexture rt = (_rtList.Count>0) ?  _rtList.RemoveLast() : CreateRenderTexture();

            PainterCamera.Inst.Render(textureColor, rt);

            return rt;
        }

        public RenderTexture GetRenderTexture()
        {
            if (_rtList.Count > 0)
            {
                return _rtList.RemoveLast();
            }

            return CreateRenderTexture();
        }

        public void ReturnOne(RenderTexture rt) =>  _rtList.Add(rt);
        
        private void OnEnable()
        {
            inst = this;
            width = Mathf.ClosestPowerOfTwo(width);
        }

       public override void Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector(this);

            "Data (Non Color) Texture".toggleIcon(ref nonColorData).nl(ref changed);

            "Textures 2D".edit_List_UObj(ref _t2DList).nl(ref changed);

            "Render Textures".edit_List_UObj(ref _rtList).nl(ref changed);

            "Size:".selectPow2(ref width, 16, 4096).nl(ref changed);

        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(TexturesPool))]
    public class TexturesPoolDrawer : PEGI_Inspector_Mono<TexturesPool> { }
    #endif

}

