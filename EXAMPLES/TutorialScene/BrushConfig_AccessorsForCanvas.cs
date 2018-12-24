using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter {

    public class BrushConfig_AccessorsForCanvas : MonoBehaviour {

        #region Brush Configurations
        BrushConfig Brush => PainterCamera.Data.brushConfig;

        public float Size2D { get { return Brush.Brush2D_Radius;  } set { Brush.Brush2D_Radius = value; } }

        public float Size3D { get { return Brush.Brush3D_Radius; } set { Brush.Brush3D_Radius = value; } }

        public float alpha { get { return Brush.colorLinear.a; } set { Brush.colorLinear.a = value; } }



        #endregion

        #region Painter Configurations
        public PlaytimePainter painterComponent;
        
        ImageData GetCurrentImage() {

            if (painterComponent) {
                var img = painterComponent.ImgData;

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
                img.SaveName = "TextureFrom {0}".F(painterComponent.gameObject.name);
                nameOfSavedTexture = img.SaveInPlayer();
            }
        }

        public void LoadTexture() {
            var img = GetCurrentImage();

            if (img != null) 
                img.LoadInPlayer(nameOfSavedTexture); 
            
        }

        public void LoadFromURL(string url) =>
                painterComponent?.loadingOrder.Add(PainterCamera.downloadManager.StartDownload(url), painterComponent.GetMaterialTexturePropertyName);

        public string urlName;

        public string UrlName { set { urlName = value; } }

        public void LoadMyURL() => LoadFromURL(urlName);

        // To fix the error above, inside PlaytimePainter.cs replace loadingOrder line with:
        // [NonSerialized] public Dictionary<int, string> loadingOrder = new Dictionary<int, string>();

        #endregion
    }
}
