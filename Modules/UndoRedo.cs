﻿using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public class PaintingUndoRedo
    {

        public class TextureBackup
        {
            public int order;

            // Will replace with Dictionary of Encodings 
            public List<string> strokeRecord;

            protected void SetB(TextureMeta from, int globalOrder)
            {
                order = globalOrder;

                foreach (var module in from.Modules)
                    module.OnTextureBackup(this);

            }
        }

        public class Texture2DBackup : TextureBackup
        {
            public Color[] pixels;

            public void Set(Color[] texturePixels, TextureMeta from, int globalOrder)
            {

                SetB(from, globalOrder);
                pixels = texturePixels;
            }

            public Texture2DBackup(Color[] texturePixels, TextureMeta from, int globalOrder)
            {
                Set(texturePixels, from, globalOrder);
            }

        }

        public class RenderTextureBackup : TextureBackup
        {
            public RenderTexture rt;
            public bool exclusive;

            public void Set(TextureMeta from, int globalOrder)
            {

                RenderTextureBuffersManager.Blit(from.CurrentRenderTexture(), rt);

                SetB(from, globalOrder);

                exclusive = from.renderTexture != null;
            }

            public RenderTextureBackup(TextureMeta from, int globalOrder)
            {
                RenderTexture frt = from.CurrentRenderTexture();

                rt = new RenderTexture(from.width, from.height, 0, RenderTextureFormat.ARGB32,
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
            private static PainterCamera Rtp => PainterCamera.Inst;
            public readonly bool isUndo;
            private int _order;

            public readonly List<Texture2DBackup> tex2D = new List<Texture2DBackup>();
            public readonly List<RenderTextureBackup> rTex = new List<RenderTextureBackup>();

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

            public void ApplyTo(TextureMeta id)
            {

                var fromRt = (tex2D.Count == 0) ||
                             ((rTex.Count > 0) && (tex2D[tex2D.Count - 1].order < rTex[rTex.Count - 1].order));

                var toRt = id.target == TexTarget.RenderTexture;

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
                        Rtp.Render(rtBackup.rt, id);
                    else
                        Rtp.Render(id.texture2D, id);

                }
                else if (fromRt)
                {
                    id.texture2D.CopyFrom(rtBackup.rt);
                    id.PixelsFromTexture2D(id.texture2D);

                    var converted = false;

                    if ((PainterCamera.Inst.IsLinearColorSpace) && !rtBackup.exclusive)
                    {
                        converted = true;
                        id.PixelsToGamma();
                    }

                    if (converted)
                        id.SetAndApply();
                    else
                        id.texture2D.Apply(true);
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

            public void BackupTexture2D(int maxTextures, TextureMeta id)
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

            public void BackupRenderTexture(int maxTextures, TextureMeta from)
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