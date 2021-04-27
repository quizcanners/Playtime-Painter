using QuizCanners.Inspect;
using QuizCanners.Migration;

namespace PainterTool
{

    public abstract class ImageMetaModuleBase : PainterClassCfg, IGotClassTag, ICfg
    {

        public TextureMeta parentMeta;
        
        #region Abstract Serialized

        public abstract string ClassTag { get;  }

        #endregion

        public virtual void ShowHideSectionInspect() { }

        public virtual void OnPaintingDrag(PainterComponent painter) { }

        public virtual void OnUndo(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void OnRedo(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void OnTextureBackup(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void ManagedUpdate() { }

        public virtual void OnPainting(PainterComponent painter) { }

        #region Inspector

        public virtual void BrushConfigPEGI(PainterComponent painter) { }

        #endregion

    }
}