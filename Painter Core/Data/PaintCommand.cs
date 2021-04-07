using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;


#pragma warning disable IDE0019 // Use pattern matching

namespace PlaytimePainter
{
    public static class PaintCommand 
    {
        public class UV
        {
            public Stroke Stroke { get; set; }

            public TextureMeta TextureData { get; set; }

            public Brush Brush { get; set; }

            public bool usedAlphaBuffer;
            public float strokeAlphaPortion = 1;

            public virtual bool Is3DBrush => false;

            public void OnStrokeComplete()
            {
                TextureData.AfterStroke(Stroke);
            }

            public UV(Stroke stroke, Texture texture, Brush brush)
            {
                Stroke = stroke;
                TextureData = texture.GetTextureMeta();
                Brush = brush;
            }

            public UV(Stroke stroke, TextureMeta textureData, Brush brush)
            {
                Stroke = stroke;
                TextureData = textureData;
                Brush = brush;
            }
        }

        public abstract class WorldSpaceBase : UV
        {
            public abstract GameObject GameObject { get ; set ; }
            
            public abstract SkinnedMeshRenderer SkinnedMeshRenderer { get ; set ; }
            
            public abstract Mesh Mesh {get; set;}
            
            public abstract  int SubMeshIndexFirst {get; set;}
            
            public abstract List<int> SelectedSubMeshes {get; set; }
            
            protected WorldSpaceBase(Stroke stroke, Texture texture, Brush brush) : base(stroke, texture, brush) { }

            protected WorldSpaceBase(Stroke stroke, TextureMeta textureData, Brush brush): base(stroke, textureData, brush) { }
        }
        
        public class WorldSpace : WorldSpaceBase
        {
            public sealed override GameObject GameObject { get ; set ; }
            public sealed override SkinnedMeshRenderer SkinnedMeshRenderer { get; set; }
            public sealed override Mesh Mesh { get; set; }

            public override bool Is3DBrush => Brush.Is3DBrush(TextureData);

            private List<int> _selectedSubMeshes = new List<int>(1);

            public override List<int> SelectedSubMeshes
            {
                get => _selectedSubMeshes;
                set => _selectedSubMeshes = value;
            }

            public sealed override int SubMeshIndexFirst
            {
                get => SelectedSubMeshes.TryGet(0);
                set => SelectedSubMeshes.ForceSet(0, value);
            }

            public WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, Mesh mesh, int subMeshIndexFirst, GameObject gameObject) : base(stroke, textureData, brush)
            {
                Mesh = mesh;
                _selectedSubMeshes.ForceSet(0, subMeshIndexFirst); 
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, Texture texture, Brush brush, Mesh mesh, int subMeshIndexFirst, GameObject gameObject) : base(stroke, texture, brush)
            {
                Mesh = mesh;
                SubMeshIndexFirst = subMeshIndexFirst;
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndexFirst, GameObject gameObject) : base(stroke, textureData, brush)
            {
                SkinnedMeshRenderer = skinnedMeshRenderer;
                SubMeshIndexFirst = subMeshIndexFirst;
                GameObject = gameObject;
            }

            public WorldSpace(Stroke stroke, Texture texture, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndexFirst, GameObject gameObject) : base(stroke, texture, brush)
            {
                SkinnedMeshRenderer = skinnedMeshRenderer;
                SubMeshIndexFirst = subMeshIndexFirst;
                GameObject = gameObject;
            }
        }
        
        public class ForPainterComponent : WorldSpaceBase
        {
            public readonly PlaytimePainter painter;

            public override bool Is3DBrush => painter.Is3DBrush(Brush);

            public sealed override GameObject GameObject
            {
                get => painter.gameObject;
                set { }
            }

            public sealed override SkinnedMeshRenderer SkinnedMeshRenderer
            {
                get => painter.skinnedMeshRenderer;
                set { }
            }

            public sealed override Mesh Mesh
            {
                get => painter.GetMesh();
                set { }
            }

            public override List<int> SelectedSubMeshes
            {
                get => new List<int> { painter.selectedSubMesh };
                set { }
            }

            public sealed override int SubMeshIndexFirst
            {
                get => painter.selectedSubMesh;
                set
                {
                    if (painter)
                        painter.selectedSubMesh = value;
                }
            }

            public ForPainterComponent(Stroke stroke, Brush brush, PlaytimePainter painter) : base(stroke, painter.TexMeta, brush)
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
            var painterCommand = command as ForPainterComponent;
            if (painterCommand != null && painterCommand.painter)
                return painterCommand.painter;

            return null;
        }
    }
}