﻿using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class PlaytimePainter_TexturesPool : PainterSystemMono  {

        public static PlaytimePainter_TexturesPool inst;
        public static PlaytimePainter_TexturesPool ForceInstance
        {
            get
            {
                if (!inst && !ApplicationIsQuitting) {
                    inst = new GameObject().AddComponent<PlaytimePainter_TexturesPool>();
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
            pegi.nl();

            "Data (Non Color) Texture".toggleIcon(ref nonColorData).nl();

            "Textures 2D".edit_List_UObj(_t2DList).nl();

            "Render Textures".edit_List_UObj(_rtList).nl();

            "Size:".selectPow2(ref width, 16, 4096).nl();
        }
    }


    [PEGI_Inspector_Override(typeof(PlaytimePainter_TexturesPool))] internal class TexturesPoolDrawer : PEGI_Inspector_Override { }


}

