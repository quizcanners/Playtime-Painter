using PainterTool.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using System;
using System.IO;
#if UNITY_EDITOR
using  UnityEditor;
#endif
using UnityEngine;

namespace PainterTool
{

    public partial class PainterComponent
    {

        [SerializeField] private CfgData _cfgData;

        public CfgEncoder Encode() => new CfgEncoder()
            .Add("mdls", Modules)
            .Add_IfTrue("invCast", invertRayCast);

        public CfgEncoder EncodeMeshStuff()
        {
            if (IsEditingThisMesh)
                Painter.MeshManager.StopEditingMesh();

            return new CfgEncoder()
                .Add("m", SavedEditableMesh)
                .Add_String("prn", selectedMeshProfile);
        }

        public void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "mdls":
                    Modules.Decode(data);
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

        private void ForceReimportMyTexture()
        {
            var path = AssetDatabase.GetAssetPath(TexMeta.Texture2D);

            var imp = AssetImporter.GetAtPath(path);

            var importer = imp as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("No importer for {0}".F(path));
                return;
            }

            var id = TexMeta;

            Painter.Camera.TryDiscardBufferChangesTo(id);

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
            Path.Combine(Painter.Data.texturesFolderName, TexMeta.saveName + ".png");

        private readonly LoopLock _loopLock = new();

        private bool OnBeforeSaveTexture(TextureMeta id)
        {

            if (id.TargetIsRenderTexture())
                id.RenderTexture_To_Texture2D();

            var tex = id.Texture2D;

            if (id[TextureCfgFlags.PreserveTransparency] && !tex.TextureHasAlpha())
            {
                if (_loopLock.Unlocked)
                    using (_loopLock.Lock())
                    {
                        Debug.Log("Old Texture had no Alpha channel, creating new");

                        string tname = id.Texture2D.name + "_A";

                        id.Texture2D = QcUnity.CreatePngSameDirectory(id.Texture2D, tname);

                        id.saveName = tname;

                        id.Texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale_Editor();

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

            QcUnity.TrySaveTexture(ref id.Texture2D, texName);

            OnPostSaveTexture(id);
        }

        private void RewriteOriginalTexture()
        {
            var id = TexMeta;

            if (!OnBeforeSaveTexture(id))
                return;

            QcUnity.TrySaveTexture(ref id.Texture2D);
            OnPostSaveTexture(id);
        }

        private void SaveTextureAsAsset(bool asNew)
        {

            var id = TexMeta;

            if (OnBeforeSaveTexture(id))
            {
                id.Texture2D = QcUnity.SaveTextureAsAsset(id.Texture2D, Painter.Data.texturesFolderName, ref id.saveName, asNew);
                id.Texture2D.Reimport_IfNotReadale_Editor();
            }

            OnPostSaveTexture(id);
        }

        internal void SaveMesh()
        {
            var mesh = this.GetMesh();
            var exists = QcUnity.IsSavedAsAsset(mesh);

            if (exists)
            {
               //var path = AssetDatabase.GetAssetPath(mesh);
                //SharedMesh = Instantiate(SharedMesh);

                // AssetDatabase.SaveAssets( sm, Path.Combine("Assets", MeshEditorManager.GenerateMeshSavePath()));

                mesh.SetToDirty();
                AssetDatabase.SaveAssets();
                UpdateMeshCollider();

            }
            else
            {


                var folderPath = Path.Combine(Application.dataPath, Painter.Data.meshesFolderName);
                Directory.CreateDirectory(folderPath);

                try
                {


                    var sm = SharedMesh;

                    Directory.CreateDirectory(Path.Combine("Assets", Painter.Data.meshesFolderName));

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
        }

#endif
    }
}