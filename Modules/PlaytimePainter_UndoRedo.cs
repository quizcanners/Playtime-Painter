using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{


    public class PaintingUndoRedo
    {

        public class TextureBackup
        {
            public int order;

            // Will replace with Dictionary of Encodings 
            public List<string> strokeRecord;

            internal void SetB(TextureMeta from, int globalOrder)
            {
                order = globalOrder;

                foreach (var module in from.Modules)
                    module.OnTextureBackup(this);

            }
        }

        public class Texture2DBackup : TextureBackup
        {
            public Color[] pixels;

            internal void Set(Color[] texturePixels, TextureMeta from, int globalOrder)
            {

                SetB(from, globalOrder);
                pixels = texturePixels;
            }

            internal Texture2DBackup(Color[] texturePixels, TextureMeta from, int globalOrder)
            {
                Set(texturePixels, from, globalOrder);
            }

        }

        public class RenderTextureBackup : TextureBackup
        {
            public RenderTexture rt;
            public bool exclusive;

            internal void Set(TextureMeta from, int globalOrder)
            {

                RenderTextureBuffersManager.Blit(from.CurrentRenderTexture(), rt);

                SetB(from, globalOrder);

                exclusive = from.RenderTexture != null;
            }

            internal RenderTextureBackup(TextureMeta from, int globalOrder)
            {
                RenderTexture frt = from.CurrentRenderTexture();

                rt = new RenderTexture(from.Width, from.Height, 0, RenderTextureFormat.ARGB32,
                    frt.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
                {
                    filterMode = frt.filterMode
                };
                Set(from, globalOrder);
            }

            public void DestroyRtex()
            {
                rt.DestroyWhatever();

            }

        }

        public class BackupsLineup
        {
            public readonly bool isUndo;
            private int _order;

            public readonly List<Texture2DBackup> tex2D = new();
            public readonly List<RenderTextureBackup> rTex = new();

            public BackupsLineup otherDirection;

            public string currentStep = "";

            public bool GotData => (tex2D.Count > 0) || (rTex.Count > 0);

            public void Clear()
            {
                foreach (var r in rTex)
                    r.DestroyRtex();

                tex2D.Clear();
                rTex.Clear();
            }

            private void ClearRenderTexturesTill(int maxTextures)
            {
                var toClear = rTex.Count - maxTextures;

                for (var i = 0; i < toClear; i++)
                    rTex[i].DestroyRtex();

                QcSharp.SetMaximumLength(rTex, maxTextures);
            }

            internal void ApplyTo(TextureMeta id)
            {

                var fromRt = (tex2D.Count == 0) ||
                             ((rTex.Count > 0) && (tex2D[^1].order < rTex[^1].order));

                var toRt = id.Target == TexTarget.RenderTexture;

                if (toRt)
                    otherDirection.BackupRenderTexture(int.MaxValue, id);
                else
                    otherDirection.BackupTexture2D(int.MaxValue, id);

                var rtBackup = fromRt ? TakeRenderTexture() : null;
                var pixBackup = fromRt ? null : TakeTexture2D();
                var backup = fromRt ? rtBackup : (TextureBackup) pixBackup;

                if (isUndo)
                {
                    foreach (var module in id.Modules)
                        module.OnUndo(backup);
                }
                else
                    foreach (var module in id.Modules)
                        module.OnRedo(backup);

                if (!fromRt)
                {
                    id.Pixels = pixBackup.pixels;
                    id.SetAndApply();
                }

                if (toRt)
                {
                    if (fromRt)
                        Painter.Camera.Render(rtBackup.rt, id);
                    else
                        Painter.Camera.Render(id.Texture2D, id);

                }
                else if (fromRt)
                {
                    id.Texture2D.CopyFrom(rtBackup.rt);
                    id.PixelsFromTexture2D(id.Texture2D);

                    var converted = false;

                    if ((Painter.IsLinearColorSpace) && !rtBackup.exclusive)
                    {
                        converted = true;
                        id.ConvertPixelsToGamma();
                    }

                    if (converted)
                        id.SetAndApply();
                    else
                        id.Texture2D.Apply(true);
                }

                if (fromRt)
                    rtBackup.DestroyRtex();

            }

            private Texture2DBackup TakeTexture2D()
            {
                var index = tex2D.Count - 1;
                var pixels = tex2D[index];
                tex2D.RemoveAt(index);
                return pixels;
            }

            private RenderTextureBackup TakeRenderTexture()
            {
                var index = rTex.Count - 1;
                var rt = rTex[index];
                rTex.RemoveAt(index);
                return rt;
            }

            internal void BackupTexture2D(int maxTextures, TextureMeta id)
            {

                QcSharp.SetMaximumLength(tex2D, maxTextures);

                if (maxTextures <= 0) return;

                var copyPix = (Color[]) id.Pixels.Clone();

                if (tex2D.Count < maxTextures)
                    tex2D.Add(new Texture2DBackup(copyPix, id, _order));
                else
                    QcSharp.MoveFirstToLast(tex2D).Set(copyPix, id, _order);



                _order++;

            }

            internal void BackupRenderTexture(int maxTextures, TextureMeta from)
            {

                ClearRenderTexturesTill(maxTextures);

                if (maxTextures <= 0) return;

                if (rTex.Count < maxTextures)
                    rTex.Add(new RenderTextureBackup(from, _order));
                else
                    QcSharp.MoveFirstToLast(rTex).Set(from, _order);

                _order++;

            }

            public BackupsLineup(bool undo)
            {
                isUndo = undo;
            }

        }

        public class UndoCache
        {

            public readonly BackupsLineup undo;
            public readonly BackupsLineup redo;

            public UndoCache()
            {
                undo = new BackupsLineup(true);
                redo = new BackupsLineup(false);

                undo.otherDirection = redo;
                redo.otherDirection = undo;
            }

        }
    }
}