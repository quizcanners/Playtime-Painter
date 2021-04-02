using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;


#pragma warning disable IDE0019 // Use pattern matching

namespace PlaytimePainter
{
    public static partial class PaintCommand 
    {
        public class UV
        {
            public virtual Stroke Stroke { get; set; }

            private readonly TextureMeta _texMeta;

            public virtual TextureMeta TextureData { get; set; }

            public virtual Brush Brush { get; set; }

            public bool usedAlphaBuffer;
            public float strokeAlphaPortion = 1;

            public virtual bool Is3DBrush => false;

            public void OnStrokeComplete()
            {
                TextureData.AfterStroke(Stroke);
            }

            public UV(Stroke stroke, Texture texture, Brush brush)
            {
                this.Stroke = stroke;
                TextureData = texture.GetTextureMeta();
                this.Brush = brush;
            }

            public UV(Stroke stroke, TextureMeta textureData, Brush brush)
            {
                this.Stroke = stroke;
                this.TextureData = textureData;
                this.Brush = brush;
            }
        }

        public class WorldSpace : UV
        {

            public virtual GameObject GameObject
            {
                get;
                set;
            }

            public virtual SkinnedMeshRenderer SkinnedMeshRenderer
            {
                get;
                set;
            }

            public virtual Mesh Mesh
            {
                get;
                set;
            }

            public override bool Is3DBrush => Brush.Is3DBrush(TextureData);

            private List<int> _selectedSubmeshes = new List<int>(1);

            public virtual List<int> SelectedSubmeshes
            {
                get { return _selectedSubmeshes; }
                set { _selectedSubmeshes = value; }
            }

            public virtual int SubMeshIndexFirst
            {
                get
                {
                    return SelectedSubmeshes.TryGet(0);
                }
                set
                {
                    SelectedSubmeshes.ForceSet(0, value);
                }
            }

            public WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, Mesh mesh, int submeshIndexFirst, GameObject gameObject) : base(stroke, textureData, brush)
            {
                Mesh = mesh;
                SubMeshIndexFirst = submeshIndexFirst;
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, Texture texture, Brush brush, Mesh mesh, int subMeshIndexFirst, GameObject gameObject) : base(stroke, texture, brush)
            {
                Mesh = mesh;
                SubMeshIndexFirst = subMeshIndexFirst;
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int submeshIndexFirst, GameObject gameObject) : base(stroke, textureData, brush)
            {
                SkinnedMeshRenderer = skinnedMeshRenderer;
                SubMeshIndexFirst = submeshIndexFirst;
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, Texture texture, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndexFirst, GameObject gameObject) : base(stroke, texture, brush)
            {
                SkinnedMeshRenderer = skinnedMeshRenderer;
                SubMeshIndexFirst = subMeshIndexFirst;
                GameObject = gameObject;
            }
        }
        
        public class ForPainterComponent : WorldSpace
        {
            public PlaytimePainter painter;

            public override bool Is3DBrush => painter.Is3DBrush(Brush);

            public sealed override GameObject GameObject
            {
                get { return painter.gameObject; }
                set { }
            }

            public sealed override SkinnedMeshRenderer SkinnedMeshRenderer
            {
                get { return painter.skinnedMeshRenderer; }
                set { }
            }

            public sealed override Mesh Mesh
            {
                get { return painter.GetMesh(); }
                set { }
            }

            public override List<int> SelectedSubmeshes
            {
                get { return new List<int> { painter.selectedSubMesh }; }
                set { }
            }

            public sealed override int SubMeshIndexFirst
            {
                get { return painter.selectedSubMesh; }
                set
                {
                    if (painter)
                        painter.selectedSubMesh = value;
                }
            }

            public ForPainterComponent(Stroke stroke, Brush brush, PlaytimePainter painter) : base(stroke, painter.TexMeta, brush,
                painter.skinnedMeshRenderer, 0, painter.gameObject)
            {
                SkinnedMeshRenderer = painter.skinnedMeshRenderer;
                Mesh = painter.GetMesh();
                SubMeshIndexFirst = painter.selectedSubMesh;
                GameObject = painter.gameObject;
                this.painter = painter;
            }
        }

    
        public static T Reset<T>(this T command) where T : UV
        {
            command.strokeAlphaPortion = 1;
            command.usedAlphaBuffer = false;
            return command;
        }
        
        public static T SetStroke<T>(this T command, Stroke stroke) where T : UV
        {
            command.Stroke = stroke;
            return command;
        }

        public static T SetBrush<T>(this T command, Brush brush) where T : UV
        {
            command.Brush = brush;
            return command;
        }

        public static PlaytimePainter TryGetPainter<T>(this T command) where T : UV
        {
            var pntr = command as ForPainterComponent;
            if (pntr != null && pntr.painter)
                return pntr.painter;

            return null;
        }
    }
}