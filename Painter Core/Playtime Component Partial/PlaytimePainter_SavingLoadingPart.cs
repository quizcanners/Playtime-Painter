using QuizCanners.Inspect;
using PlaytimePainter.MeshEditing;
using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PlaytimePainter
{

    #pragma warning disable IDE0018 // Inline variable declaration

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
                id.Texture2DToRenderTexture(id.texture2D);
            else if (id.texture2D)
                id.PixelsFromTexture2D(id.texture2D);

            SetTextureOnMaterial(id);
        }

        private bool TextureExistsAtDestinationPath() =>
            AssetImporter.GetAtPath(Path.Combine("Assets", GenerateTextureSavePath())) as TextureImporter != null;

        private string GenerateTextureSavePath() =>
            Path.Combine(Cfg.texturesFolderName, TexMeta.saveName + ".png");

        LoopLock _loopLock = new LoopLock();

        private bool OnBeforeSaveTexture(TextureMeta id)
        {

            if (id.TargetIsRenderTexture())
                id.RenderTexture_To_Texture2D();

            var tex = id.texture2D;

            if (id.preserveTransparency && !tex.TextureHasAlpha())
            {


                if (_loopLock.Unlocked)
                    using (_loopLock.Lock())
                    {
                        //ChangeTexture(id.NewTexture2D());

                        //id.texture2D.name = id.texture2D.name + "_A";

                        Debug.Log("Old Texture had no Alpha channel, creating new");

                        string tname = id.texture2D.name + "_A";

                        id.texture2D = id.texture2D.CreatePngSameDirectory(tname);

                        id.saveName = tname;

                        id.texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale();

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
            UpdateOrSetTexTarget(id.target);
            UpdateModules();

            id.UnsetAlphaSavePixel();
        }

        private void RewriteOriginalTexture_Rename(string texName)
        {

            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.texture2D = id.texture2D.RewriteOriginalTexture_NewName(texName);

            OnPostSaveTexture(id);

        }

        private void RewriteOriginalTexture()
        {
            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.texture2D = id.texture2D.RewriteOriginalTexture();
            OnPostSaveTexture(id);

        }

        private void SaveTextureAsAsset(bool asNew)
        {

            var id = TexMeta;

            if (OnBeforeSaveTexture(id))
            {
                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.saveName, asNew);

                id.texture2D.Reimport_IfNotReadale();
            }

            OnPostSaveTexture(id);
        }

        public void SaveMesh()
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
                Debug.LogError(ex);
            }
        }

#endif
    }
}