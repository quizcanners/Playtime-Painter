using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using static PlaytimePainter.PaintingUndoRedo;

namespace PlaytimePainter
{

    public class ImageMetaModuleAttribute : AbstractWithTaggedTypes {
        public override TaggedTypesCfg TaggedTypes => TaggedModulesList<ImageMetaModuleBase>.all;
    }

    [ImageMetaModule]
    public abstract class ImageMetaModuleBase : PainterSystemCfg, IGotClassTag {

        public ImageMeta parentMeta;
        
        #region Abstract Serialized

        public abstract string ClassTag { get;  }
    
        public TaggedTypesCfg AllTypes => TaggedModulesList<ImageMetaModuleBase>.all; 
        #endregion

        public virtual bool ShowHideSectionInspect() => false;

        public virtual void OnPaintingDrag(PlaytimePainter painter) { }

        public virtual void OnUndo(TextureBackup backup) { }

        public virtual void OnRedo(TextureBackup backup) { }

        public virtual void OnTextureBackup(TextureBackup backup) { }

        public virtual void ManagedUpdate() { }

        public virtual void OnPainting(PlaytimePainter painter) { }

        #region Inspector

        public virtual bool BrushConfigPEGI(PlaytimePainter painter) => false;

        #endregion

    }
}