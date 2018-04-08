using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

    public enum texTarget {  Texture2D, RenderTexture }

    public static class PlaytimePainterExtensions {

        public static bool TargetIsTexture2D(this imgData id) {
            if (id == null) return false;
            return id.destination == texTarget.Texture2D;
        }

        public static bool TargetIsRenderTexture(this imgData id) {
            if (id == null) return false;
            return id.destination == texTarget.RenderTexture;
        }

        public static bool TargetIsBigRenderTexture(this imgData id) {
            if (id == null) return false;
            return (id.destination == texTarget.RenderTexture) && (id.renderTexture == null);
        }

        public static imgData getImgDataIfExists(this Texture texture)
        {
            if (texture == null)
                return null;

            foreach (imgData id in PainterManager.inst.imgdatas)
                if ((id.texture2D == texture) || (id.renderTexture == texture)) 
                    return id;

            return null;
        }

        public static imgData getImgData(this Texture texture) {
            if (texture == null)
                return null;
            
            var nid = texture.getImgDataIfExists();
            if (nid != null) return nid;

             nid = ScriptableObject.CreateInstance<imgData>().init(texture);

            return nid;
        }

        public static bool isBigRenderTexturePair(this Texture tex) {
            return ((tex != null) && PainterManager.GotBuffers() && ((tex == PainterManager.inst.BigRT_pair[0])));
        }

        public static bool ContainsDuplicant(this List<imgData> texs, imgData other) {

            if (other == null)
                return true;

            for (int i=0; i<texs.Count; i++)
                if (texs[i] == null) { texs.RemoveAt(i); i--;}

            foreach (imgData t in texs)
                if (t.Equals(other))
                    return true;

            return false;
        }

        public static Texture getDestinationTexture(this Texture texture) {

            imgData id = texture.getImgDataIfExists();
            if (id != null)
                return id.currentTexture();

            return texture;
        }

        public static RenderTexture currentRenderTexture(this imgData id) {
            if (id == null)
                return null;
            return id.renderTexture == null ? PainterManager.inst.BigRT_pair[0] : id.renderTexture;
        }

        public static Texture exclusiveTexture(this imgData id) {
            if (id == null)
                return null;
            switch (id.destination) {
                case texTarget.RenderTexture:
                    return id.renderTexture == null ? (Texture)id.texture2D : (Texture)id.renderTexture;
                case texTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static Texture currentTexture(this imgData id) {
            if (id == null)
                return null;
            switch (id.destination) {
                case texTarget.RenderTexture:
                    return id.renderTexture == null ? PainterManager.inst.BigRT_pair[0] : id.renderTexture;
                case texTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

    }

    [Serializable]
    public class imgData : PainterStuffScriptable {
   
        const float bytetocol = 1f / 255f;
        public texTarget destination;
        public RenderTexture renderTexture;
        public static Texture2D sampler;
        public Texture2D texture2D;
        public int width = 128;
        public int height = 128;
        public bool useTexcoord2;

        public bool needsToBeSaved { get { return ((texture2D != null && texture2D.SavedAsAsset()) || ( renderTexture != null && renderTexture.SavedAsAsset())); } }

        [NonSerialized]
        public Color[] _pixels;

        public Color[] pixels { get { if (_pixels == null) PixelsFromTexture2D(texture2D); return _pixels; }
            set { _pixels = value; }
        }

        [NonSerialized]
        public UndoCache cache = new UndoCache();

        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string SaveName = "No Name";

        public override string ToString() {
            return (texture2D != null ? texture2D.name : (renderTexture != null ? renderTexture.name : SaveName));
        }

        // ##################### For Stroke Vector Recording
        [NonSerialized]
        public List<string> recordedStrokes = new List<string>();
        [NonSerialized]
        public List<string> recordedStrokes_forUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo
        public bool recording;

        public void StartRecording() {
            recordedStrokes = new List<string>();
            recordedStrokes_forUndoRedo = new List<string>();
            recording = true;
        }

        public void ContinueRecording() {
            StartRecording();

            recordedStrokes.Add(cfg.GetRecordingData(SaveName));
        }

        public void SaveRecording() {

            cfg.RemoveRecord(SaveName);

            StringBuilder bildy = new StringBuilder();

            foreach (string ass in recordedStrokes)
                bildy.Append(ass);

            string text = bildy.ToString();

            ResourceSaver.SaveToResources(cfg.texturesFolderName, cfg.vectorsFolderName, SaveName, text);

            cfg.recordingNames.Add(SaveName);

            recording = false;

           

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        // ####################### Textures MGMT

        public bool Equals(Texture tex)
        {
            return (tex != null && tex == texture2D) || (renderTexture != null && renderTexture == tex);
        }

        public RenderTexture AddRenderTexture() {
            return AddRenderTexture(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, FilterMode.Bilinear, null);
        }

        public RenderTexture AddRenderTexture(int nwidth, int nheight, RenderTextureFormat format, RenderTextureReadWrite dataType, FilterMode filterMode, string global) {

            if ((destination == texTarget.RenderTexture) && (texture2D != null))
                RenderTexture_To_Texture2D(texture2D);


            width = nwidth;
            height = nheight;

            renderTexture = new RenderTexture(width, height, 0, format, dataType);
            renderTexture.filterMode = filterMode;

            renderTexture.name = SaveName;

            if ((global != null) && (global.Length > 0))
                Shader.SetGlobalTexture(global, renderTexture);

            if (destination == texTarget.RenderTexture)
                Texture2D_To_RenderTexture();

            return renderTexture;
        }

        public void Texture2D_To_RenderTexture() {
            if (texture2D != null)
                PainterManager.inst.Render(texture2D, this);
        }

        public void RenderTexture_To_Texture2D(Texture2D tex) {

            RenderTexture rt = renderTexture == null ? PainterManager.inst.painterRT_toBuffer(width, height)
                : renderTexture;

            tex.CopyFrom(rt);

            PixelsFromTexture2D(tex);




            /* MAC: 
                    Linear Space
                        Big RT
                            Editor 
                                Linear Texture = To Linear
                                sRGB Texture = 
                            Playtime
                                Linear Texture = To Linear
                                sRGB Texture = 
                        Exclusive
                            Editor 
                                Linear Texture = 
                                sRGB Texture = 
                            Playtime
                                Linear Texture 
                                sRGB Texture = 
                    Gamma Space
                        Big RT
                            Editor 
                                Linear Texture =
                                sRGB Texture = 
                            Playtime
                                Linear Texture 
                                sRGB Texture = 
                        Exclusive
                            Editor 
                                Linear Texture = 
                                sRGB Texture = 
                            Playtime
                                Linear Texture =
                                sRGB Texture = 
            */

          //  Debug.Log("We are linear: "+RenderTexturePainter.inst.isLinearColorSpace + " tex is sRGB: "+tex.isColorTexturee());

            if ((PainterManager.inst.isLinearColorSpace) && (!tex.isColorTexturee())) {
                pixelsToLinear();
                 //pixelsToGamma();
                //Debug.Log("Pixels to gamma");
            }


            //if (!RenderTexturePainter.inst.isLinearColorSpace)
            //pixelsToLinear ();

            SetAndApply(true);

        }

        public void pixelsToGamma() {
            var p = pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].gamma;
        }

        public void pixelsToLinear() {
            var p = pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].linear;
        }

        void UVto01(ref Vector2 uv) {
            uv.x %= 1;
            uv.y %= 1;
            if (uv.x < 0) uv.x += 1;
            if (uv.y < 0) uv.y += 1;
        }

        public void Colorize(Color col) {
            var p = pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = col;

        }

        public Color SampleAT(Vector2 uv) {
            return (destination == texTarget.Texture2D) ?
            pixel(uvToPixelNumber(uv)) :
                SampleRenderTexture(uv);
        }

        public Color SampleRenderTexture(Vector2 uv) {

            RenderTexture curRT = RenderTexture.active;

           // Debug.Log("Sampling Render Texture");

            PainterManager rtp = PainterManager.inst;
            int size = PainterManager.renderTextureSize / 4;
            RenderTexture.active = (renderTexture == null) ? rtp.painterRT_toBuffer(size, size) : renderTexture;

            if (sampler == null) sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (renderTexture == null)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRT;

            var pix = sampler.GetPixel(0, 0);

            if (PainterManager.inst.isLinearColorSpace)
                pix = pix.linear;

            return pix;
        }

        public void TextureToRenderTexture(Texture2D tex) {
            if (tex != null)
                PainterManager.inst.Render(tex, this);
        }

        public void PixelsFromTexture2D(Texture2D tex) {

            if (tex == null) {
                Debug.Log("Texture 2D was not assigned");
                return;
            }

            pixels = tex.GetPixels();
            width = tex.width;
            height = tex.height;

        }

        public void updateDestination(texTarget changeTo, Material mat, string parameter, PlaytimePainter painter) {

            if (changeTo != destination) {
                //   Debug.Log("Changing destination");

                if (changeTo == texTarget.RenderTexture) {
                    if (renderTexture == null)
                        PainterManager.inst.changeBufferTarget(this, mat, parameter, painter);
                  //  Debug.Log("Assigning Render Texture Target ");
                    TextureToRenderTexture(texture2D);
                } else {
                    if (texture2D == null)
                        return;

                    RenderTexture_To_Texture2D(texture2D);
                  
                    if (renderTexture == null)
                        PainterManager.inst.EmptyBufferTarget();

                }
                destination = changeTo;
                painter.setTextureOnMaterial();

            }

          

        }

        public void FromRenderTextureToNewTexture2D() {
            texture2D = new Texture2D(width, height);
            RenderTexture_To_Texture2D(texture2D);
        }

        public void From(Texture2D texture) {

            texture2D = texture;
            SaveName = texture.name;

#if UNITY_EDITOR
            if (texture != null)
            {
                var imp = texture.getTextureImporter();
                if (imp != null) {

                  /*  var name =  AssetDatabase.GetAssetPath(texture);
                    var extension = name.Substring(name.LastIndexOf(".") + 1);
                    
                    if (extension != "png") {
                        ("Converting " + name + " to .png").showNotification();
                        texture = texture.CreatePngSameDirectory(texture.name);
                    }*/

                    texture.Reimport_IfNotReadale();
                }
            }
#endif

            PixelsFromTexture2D(texture2D);
        }

		public Color pixel(myIntVec2 v) {
            v.x %= width;
            while (v.x < 0)
                v.x += width;

            v.y %= height;
            while (v.y < 0)
                v.y += height;

            return pixels[((int)v.y) * width + (int)v.x];
        }
        
		public int pixelNo(myIntVec2 v) {
			int x = v.x;
			int y = v.y;

            x %= width;
            if (x < 0)
                x += width;
            y %= height;
            if (y < 0)
                y += height;
            return y * width + x;
        }

		public myIntVec2 uvToPixelNumber(Vector2 uv) {
			return new myIntVec2(uv.x * width, uv.y * height);
        }

        public void SetAndApply(bool mipmaps) {
            if (_pixels == null) return;
            texture2D.SetPixels(_pixels);
            texture2D.Apply(mipmaps);
        }

        public imgData init (Texture tex) {
          //  Debug.Log("Creating image data for ."+tex.name);

            if (tex.GetType() == typeof(Texture2D))
                useTex2D((Texture2D)tex);
            else
                 if (tex.GetType() == typeof(RenderTexture))
                useRenderTexture((RenderTexture)tex);

            PainterManager.inst.imgdatas.Add(this);
            return this;
        }

        void useRenderTexture(RenderTexture rt) {
            renderTexture = rt;
            width = rt.width;
            height = rt.height;
            destination = texTarget.RenderTexture;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(rt);
            if (!string.IsNullOrEmpty(path)) {
                SaveName = rt.name;
                // saved = true;
            } else
#endif
               if (SaveName == null)
                SaveName = "New img";
        }

        void useTex2D(Texture2D tex) {

            From(tex);
            destination = texTarget.Texture2D;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path)) {
                SaveName = tex.name;
                // saved = true;
            } else
#endif
                if (SaveName == null)
                SaveName = "New img";
        }

        public imgData init (int renderTextureSize) {

            Debug.Log("Creating rt data.");

            width = renderTextureSize;
            height = renderTextureSize;
            AddRenderTexture();
            //destination = dest.RenderTexture;
            //Debug.Log("new RT");
            PainterManager.inst.imgdatas.Add(this);
            return this;
        }

        public bool gotUNDO() {
            return cache.undo.gotData();
        }

        public bool undo_redo_PEGI() {
            bool changed = false;

            if (cache == null) cache = new UndoCache();
            if (recordedStrokes == null) recordedStrokes = new List<string>();
            if (recordedStrokes_forUndoRedo == null)  recordedStrokes_forUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo

            if (cache.undo.gotData()) {
                if (pegi.Click(icon.Undo.getIcon(), "Press Z to undo (Scene View)", 25)) {
                    cache.undo.ApplyTo(this);
                    changed = true;
                }
            } else
                pegi.Click(icon.UndoDisabled.getIcon(), "Nothing to Undo (set number of undo frames in config)", 25);

            if (cache.redo.gotData()) {
                if (pegi.Click(icon.Redo.getIcon(), "X to Redo", 25)) {
                    changed = true;
                    cache.redo.ApplyTo(this);
                } 
            } else
                pegi.Click(icon.RedoDisabled.getIcon(), "Nothing to Redo", 25);


            pegi.newLine();

#if UNITY_EDITOR
            if (recording) {
               /* if (pegi.Bttn("REC")) {
                    StartRecording();
                    changed = true;
                }*/
                
          //  } else {
                pegi.write("Recording... " + recordedStrokes.Count + " vectors");
                pegi.newLine();
                pegi.write("Will Save As ", 70);
                pegi.edit(ref SaveName);
               
                if (pegi.Click(icon.Record.getIcon(), "Stop, don't save", 25))
                    recording = false;
                if (pegi.Click(icon.Done.getIcon(), "Finish ans Save", 25))
                    SaveRecording();
               
                pegi.newLine();
            }
#endif

            return changed;
        }

       

    }

}