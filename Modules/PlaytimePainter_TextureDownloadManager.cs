//#define QC_USE_NETWORKING


using System.Collections;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
#if QC_USE_NETWORKING
using UnityEngine.Networking;
#endif

namespace PainterTool
{

    internal class TextureDownloadManager : IPEGI
    {
        private readonly List<WebRequestMeta> _loadedTextures = new();

        private class WebRequestMeta : IGotName, IPEGI_ListInspect, IPEGI
        {
            
#if QC_USE_NETWORKING
            private UnityWebRequest _request;
            #endif

            private string _url;
            public string URL => _url;
            private Texture _texture;
            private bool _failed;

            public string NameForInspector
            {
                get { return _url; }
                set { _url = value; }
            }

            private Texture Take()
            {
                var tmp = _texture;
                _texture = null;
                _failed = false;
                DisposeRequest();
                return tmp;
            }

            public bool TryGetTexture(out Texture tex, bool remove = false)
            {
                tex = _texture;

                if (remove && _texture) Take();

                if (_failed) return true;


#if QC_USE_NETWORKING
                if (_request != null)
                {
                    if (_request.isNetworkError || _request.isHttpError)
                    {

                        _failed = true;

#if UNITY_EDITOR
                        Debug.Log(_request.error);
#endif
                        DisposeRequest();
                        return true;
                    }

                    if (_request.isDone)
                    {
                        if (_texture)
                            _texture.DestroyWhatever();
                        _texture = ((DownloadHandlerTexture)_request.downloadHandler).texture;
                        DisposeRequest();
                        tex = _texture;

                        if (remove && _texture)
                            Take();
                    }
                    else return false;
                }
                else if (!_texture) Start();
#endif

                return true;
            }

            private void Start()
            {

#if QC_USE_NETWORKING
                _request?.Dispose();
                _request = UnityWebRequestTexture.GetTexture(_url);
                _request.SendWebRequest();
                _failed = false;
#else
                Debug.Log("Can't Load {0} : QC_USE_NETWORKING is disabled".F(_url));
#endif
            }

            public WebRequestMeta(string URL)
            {
                _url = URL;
                Start();
            }

            private void DisposeRequest()
            {

#if QC_USE_NETWORKING
                _request?.Dispose();
                _request = null;
#endif
            }

            public void Dispose()
            {
                if (_texture)
                    _texture.DestroyWhatever();

                DisposeRequest();
            }

            #region Inspector

            public void InspectInList(ref int edited, int ind)
            {
              


#if QC_USE_NETWORKING

                TryGetTexture(out Texture tex);

                if (_request != null)
                    "Loading".PegiLabel().write(60);
                if (_failed)
                    "Failed".PegiLabel().write(50);

                if (_texture)
                {
                    if (icon.Refresh.Click())
                        Start();

                    if (_texture.Click())
                        edited = ind;

                }
                else
                {

                    if (_failed)
                    {
                        if (icon.Refresh.Click("Failed"))
                            Start();
                        "Failed ".F(_url).write(40);
                    }
                    else
                    {
                        icon.Active.write();
                        "Loading ".PegiLabel().write(40);
                    }

                }
#else
                "QC_USE_NETWORKING is disabled (to prevent unwanted android permissions)".PegiLabel().WriteWarning();

                    pegi.Nl();

                    if ("Enable QC_USE_NETWORKING".PegiLabel().Click())
                        QcUnity.SetPlatformDirective("QC_USE_NETWORKING", true);

#endif
                _url.PegiLabel().Write();
            }

            void IPEGI.Inspect()
            {
                TryGetTexture(out Texture tex);

                if (tex)
                    pegi.Draw(tex, 200);
            }

            #endregion
        }

        public string GetURL(int ind)
        {
            var el = _loadedTextures.TryGet(ind);
            return (el == null) ? "" : el.URL;
        }

        public bool TryGetTexture(int ind, out Texture tex, bool remove = false)
        {
            tex = null;
            var el = _loadedTextures.TryGet(ind);
            return el == null || el.TryGetTexture(out tex, remove);
        }

        public int StartDownload(string address)
        {
            if (!_loadedTextures.TryGetByIGotName(address, out var el))
            {
                el = new WebRequestMeta(address);
                _loadedTextures.Add(el);
            }

            return _loadedTextures.IndexOf(el);
        }

        public void Dispose()
        {
            foreach (var t in _loadedTextures)
                t.Dispose();

            _loadedTextures.Clear();
        }

        #region Inspector

        private int inspected = -1;
        private string tmp = "";
        void IPEGI.Inspect()
        {

            "Textures and Requests".PegiLabel().Edit_List(_loadedTextures, ref inspected);

            "URL".ConstLabel().Edit(ref tmp);
            if (tmp.Length > 0)
                Icon.Add.Click(() => StartDownload(tmp)).Nl();

        }

        #endregion
    }

}