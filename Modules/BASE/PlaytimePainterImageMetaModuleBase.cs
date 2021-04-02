using QuizCanners.CfgDecode;

namespace PlaytimePainter
{
    
    public abstract class ImageMetaModuleBase : PainterClassCfg, IGotClassTag {

        public TextureMeta parentMeta;
        
        #region Abstract Serialized

        public abstract string ClassTag { get;  }
    
        #endregion

        public virtual bool ShowHideSectionInspect() => false;

        public virtual void OnPaintingDrag(PlaytimePainter painter) { }

        public virtual void OnUndo(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void OnRedo(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void OnTextureBackup(PaintingUndoRedo.TextureBackup backup) { }

        public virtual void ManagedUpdate() { }

        public virtual void OnPainting(PlaytimePainter painter) { }

        #region Inspector

        public virtual bool BrushConfigPEGI(PlaytimePainter painter) => false;

        #endregion

    }
}