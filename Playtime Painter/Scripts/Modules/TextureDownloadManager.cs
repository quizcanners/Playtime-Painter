//#define QC_USE_NETWORKING


using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if QC_USE_NETWORKING
using UnityEngine.Networking;
#endif

namespace PlaytimePainter
{

    public class TextureDownloadManager : IPEGI
    {

        readonly List<WebRequestMeta> _loadedTextures = new List<WebRequestMeta>();

        class WebRequestMeta : IGotName, IPEGI_ListInspect, IPEGI
        {
            
#if QC_USE_NETWORKING
            private UnityWebRequest _request;
            #endif

            private string _url;
            public string URL => _url;
            private Texture _texture;
            private bool _failed = false;

            public string NameForPEGI
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

            void Start()
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

            public bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;
                Texture tex;
                TryGetTexture(out tex);


#if QC_USE_NETWORKING
                if (_request != null)
                    "Loading".write(60);
                if (_failed)
                    "Failed".write(50);

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
                        "Loading ".write(40);
                    }

                }
#else
                    "QC_USE_NETWORKING is disabled (to prevent unwanted android permissions)".writeWarning();

                    pegi.nl();

                    if ("Enable QC_USE_NETWORKING".Click())
                        QcUnity.SetPlatformDirective("QC_USE_NETWORKING", true);

#endif
                _url.write();
                return changed;
            }

            public bool Inspect()
            {
                Texture tex;
                TryGetTexture(out tex);

                if (_texture)
                    pegi.write(_texture, 200);

                return false;
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
            return (el != null) ? el.TryGetTexture(out tex, remove) : true;
        }

        public int StartDownload(string address)
        {
            var el = _loadedTextures.GetByIGotName(address);

            if (el == null)
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

        int inspected = -1;
        string tmp = "";
        public bool Inspect()
        {

            var changed = "Textures and Requests".write_List(_loadedTextures, ref inspected);

            "URL".edit(30, ref tmp);
            if (tmp.Length > 0 && icon.Add.Click().nl())
                StartDownload(tmp);

            return changed;
        }

        #endregion
    }

}