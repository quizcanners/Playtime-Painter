using PlaytimePainter.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using System;
using System.IO;
#if UNITY_EDITOR
using  UnityEditor;
#endif
using UnityEngine;

namespace PlaytimePainter
{

    public partial class PlaytimePainter
    {

        [SerializeField] private CfgData _cfgData;

        public CfgEncoder Encode() => new CfgEncoder()
            .Add("mdls", Modules)
            .Add_IfTrue("invCast", invertRayCast);

        public CfgEncoder EncodeMeshStuff()
        {
            if (IsEditingThisMesh)
                MeshEditorManager.Inst.StopEditingMesh();

            return new CfgEncoder()
                .Add("m", SavedEditableMesh)
                .Add_String("prn", selectedMeshProfile);
        }

        public void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "mdls":
                    Modules.DecodeFull(data);
                    break;
                case "invCast":
                    invertRayCast = data.ToBool();
                    break;
                case "m":
                    SavedEditableMesh = data;
                    break;
                case "prn":
                    selectedMeshProfile = data.ToString();
                    break;
            }
        }
        


#if UNITY_EDITOR

        private void ForceReimportMyTexture(string path)
        {

            var importer = AssetImporter.GetAtPath("Assets{0}".F(path)) as TextureImporter;
            if (importer == null)
            {
                Debug.Log("No importer for {0}".F(path));
                return;
            }

            var id = TexMeta;

            TexMgmt.TryDiscardBufferChangesTo(id);

            importer.SaveAndReimport();
            if (id.TargetIsRenderTexture())
                id.Texture2DToRenderTexture(id.Texture2D);
            else if (id.Texture2D)
                id.PixelsFromTexture2D(id.Texture2D);

            SetTextureOnMaterial(id);
        }

        private bool TextureExistsAtDestinationPath() =>
            AssetImporter.GetAtPath(Path.Combine("Assets", GenerateTextureSavePath())) as TextureImporter != null;

        private string GenerateTextureSavePath() =>
            Path.Combine(Cfg.texturesFolderName, TexMeta.saveName + ".png");

        private readonly LoopLock _loopLock = new LoopLock();

        private bool OnBeforeSaveTexture(TextureMeta id)
        {

            if (id.TargetIsRenderTexture())
                id.RenderTexture_To_Texture2D();

            var tex = id.Texture2D;

            if (id.PreserveTransparency && !tex.TextureHasAlpha())
            {
                if (_loopLock.Unlocked)
                    using (_loopLock.Lock())
                    {
                        Debug.Log("Old Texture had no Alpha channel, creating new");

                        string tname = id.Texture2D.name + "_A";

                        id.Texture2D = id.Texture2D.CreatePngSameDirectory(tname);

                        id.saveName = tname;

                        id.Texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale();

                        SetTextureOnMaterial(id);
                    }

                return false;
            }

            id.SetAlphaSavePixel();

            return true;
        }

        private void OnPostSaveTexture(TextureMeta id)
        {
            SetTextureOnMaterial(id);
            UpdateOrSetTexTarget(id.Target);
            UpdateModules();

            id.UnsetAlphaSavePixel();
        }

        private void RewriteOriginalTexture_Rename(string texName)
        {

            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.Texture2D = id.Texture2D.RewriteOriginalTexture_NewName(texName);

            OnPostSaveTexture(id);
        }

        private void RewriteOriginalTexture()
        {
            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.Texture2D = id.Texture2D.RewriteOriginalTexture();
            OnPostSaveTexture(id);
        }

        private void SaveTextureAsAsset(bool asNew)
        {

            var id = TexMeta;

            if (OnBeforeSaveTexture(id))
            {
                id.Texture2D = id.Texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.saveName, asNew);

                id.Texture2D.Reimport_IfNotReadale();
            }

            OnPostSaveTexture(id);
        }

        internal void SaveMesh()
        {
            var m = this.GetMesh();
            var path = AssetDatabase.GetAssetPath(m);

            var folderPath = Path.Combine(Application.dataPath, Cfg.meshesFolderName);
            Directory.CreateDirectory(folderPath);

            try
            {
                if (path.Length > 0)
                    SharedMesh = Instantiate(SharedMesh);

                var sm = SharedMesh;

                Directory.CreateDirectory(Path.Combine("Assets", Cfg.meshesFolderName));

                AssetDatabase.CreateAsset(sm, Path.Combine("Assets", MeshEditorManager.GenerateMeshSavePath()));

                AssetDatabase.SaveAssets();

                UpdateMeshCollider();

                //if (meshCollider && !meshCollider.sharedMesh && sm)
                //  meshCollider.sharedMesh = sm;

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

#endif
    }
}