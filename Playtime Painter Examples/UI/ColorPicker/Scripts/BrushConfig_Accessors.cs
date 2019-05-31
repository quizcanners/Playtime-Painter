using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlaytimePainter.Examples {

    public class BrushConfig_Accessors : MonoBehaviour {

        #region Brush Configurations
        BrushConfig Brush => PainterCamera.Data.brushConfig;

        public List<Graphic> graphicToShowScale = new List<Graphic>();

        public float Size2D { get { return Brush.brush2DRadius; } set { Brush.brush2DRadius = value; Brush.previewDirty = true; graphicToShowScale.TrySetLocalScale(0.6f + value / 256f); } }

        public float Size3D { get { return Brush.brush3DRadius; } set { Brush.brush3DRadius = value; Brush.previewDirty = true; } }

        public float ColorAlpha { get { return Brush.Color.a; } set { Brush.Color.a = value; Brush.previewDirty = true; } }

        public float Speed { get { return Brush.Speed; } set { Brush.Speed = value; Brush.previewDirty = true; } }
        
        #endregion

        bool defaultSet = false;

        private void Update()
        {
            if (!defaultSet && PainterCamera.Data){
                Size2D = Size2D;
                defaultSet = true;
            }
        }

        #region Painter Configurations
        public PlaytimePainter painterComponent;
        
        ImageMeta GetCurrentImage() {

            if (painterComponent) {
                var img = painterComponent.ImgMeta;

                if (img != null)
                    return img;
                else
                    Debug.LogError("Painter on {0} is not currently editing any of it's Material's Textures".F(painterComponent.gameObject.name));
            }
            else
                Debug.LogError("No Painter attached");

            return null;
        }

        string nameOfSavedTexture;
        
        public void SaveTexture() {
            var img = GetCurrentImage();

            if (img != null) {
                img.saveName = "TextureFrom {0}".F(painterComponent.gameObject.name);
                nameOfSavedTexture = img.SaveInPlayer();
            }
        }

        public void LoadTexture() {
            var img = GetCurrentImage();

            if (img != null) 
                img.LoadInPlayer(nameOfSavedTexture); 
            
        }

        public void LoadFromURL(string url) =>
                painterComponent?.loadingOrder.Add(PainterCamera.DownloadManager.StartDownload(url), painterComponent.GetMaterialTextureProperty);

        public string urlName;

        public string UrlName { set { urlName = value; } }

        public void LoadMyURL() => LoadFromURL(urlName);

        #endregion
    }
}
