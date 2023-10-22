using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{

    [ExecuteInEditMode]
    [AddComponentMenu("Playtime Painter/Textures Pool")]
    public class Singleton_TexturesPool : Singleton.BehaniourBase  
    {
        public static Singleton_TexturesPool ForcedInstance
        {
            get
            {
                var srv = Singleton.Get<Singleton_TexturesPool>();

                if (!srv) {
                    srv = new GameObject().AddComponent<Singleton_TexturesPool>();
                    srv.gameObject.name = "Textures Pool";
                }

                return srv;
            }
        }

        public int defaultWidth = 256;

        public bool nonColorData = true;

        [NonSerialized] private readonly Dictionary<int, TexturesCollection> collectionBySize = new();

        public override string InspectedCategory => nameof(PainterComponent);

        private RenderTexture CreateRenderTexture(int width, TextureWrapMode mode = TextureWrapMode.Repeat)
        {
            return new RenderTexture(width, width, 0) 
            {
                wrapMode = mode,
                useMipMap = false,
                name = "RenderTexture_fromPool"
            };
        }

        public RenderTexture GetRenderTexture(Color textureColor, TextureWrapMode mode = TextureWrapMode.Repeat) => GetRenderTexture(textureColor, defaultWidth, mode);

        public RenderTexture GetRenderTexture(Color textureColor, int width, TextureWrapMode mode = TextureWrapMode.Repeat)
        {
            CheckSize(ref width);

            var list = collectionBySize.GetOrCreate(width)._rtList;

            RenderTexture rt = (list.Count>0) ? list.RemoveLast() : CreateRenderTexture(width);

            Painter.Camera.Prepare(textureColor, rt).Render();

            rt.wrapMode = mode;

            return rt;
        }

        public RenderTexture GetRenderTexture(TextureWrapMode mode = TextureWrapMode.Repeat) => GetRenderTexture(defaultWidth, mode);

        public RenderTexture GetRenderTexture(int width, TextureWrapMode mode = TextureWrapMode.Repeat)
        {
            CheckSize(ref width);

            var list = collectionBySize.GetOrCreate(width)._rtList;

            if (list.Count > 0)
            {
                var el = list.RemoveLast();
                el.wrapMode = mode;
            }

            return CreateRenderTexture(width, mode);
        }

        public void ReturnOne(RenderTexture rt)
        {
            collectionBySize.GetOrCreate(rt.width)._rtList.Add(rt);
        }
        private void CheckSize(ref int size) 
        {
            if (size > 2 && Mathf.IsPowerOfTwo(size)) 
            {
                return;
            } else 
            {
                Debug.LogError("Requested size ({0}) is not Power of two. Correcting".F(size), this);
                size = Mathf.NextPowerOfTwo(size);
            }
        }

        protected override void OnAfterEnable()
        {
            defaultWidth = Mathf.ClosestPowerOfTwo(defaultWidth);
        }

        #region Inspector

        private readonly pegi.CollectionInspectorMeta collection = new("Render Textures");

        public override void Inspect()
        {
            pegi.Nl();
            
            collection.Edit_Dictionary(collectionBySize).Nl();

            if (!collection.IsAnyEntered)
            {
                "Data (Non Color) Texture".PegiLabel().ToggleIcon(ref nonColorData).Nl();
                "Default Size:".PegiLabel(90).SelectPow2(ref defaultWidth, 16, 4096).Nl();
            }
        }

        #endregion


        private class TexturesCollection : IPEGI_ListInspect
        {
            [NonSerialized] public readonly List<RenderTexture> _rtList = new();

            public void Return(RenderTexture texture)
            {
                texture.Release();
                if (_rtList.Count > 4)
                {
                    texture.DestroyWhatever();
                }
            }

            public void InspectInList(ref int edited, int index)
            {
                if (_rtList.Count == 0)
                    "Empty".PegiLabel().Write();
                else
                {
                    var first = _rtList[0];
                    "x {0} Textures. {1} pixels".F(_rtList.Count, (first.width * first.height * _rtList.Count).ToReadableString()).PegiLabel().Write();
                }
            }
        }

    }

    [PEGI_Inspector_Override(typeof(Singleton_TexturesPool))] internal class TexturesPoolDrawer : PEGI_Inspector_Override { }

}

