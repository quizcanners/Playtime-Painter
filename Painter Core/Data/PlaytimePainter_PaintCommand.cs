using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using static PainterTool.Painter.Command;


#pragma warning disable IDE0019 // Use pattern matching

namespace PainterTool
{
    public static partial class Painter
    {
        public static class Command
        {
            public abstract class Base
            {
                public Stroke Stroke { get; set; }

                internal TextureMeta TextureData { get; set; }

                public Brush Brush { get; set; }

                public bool usedAlphaBuffer;
                public float strokeAlphaPortion = 1;

                public abstract bool Is3DBrush { get; }

                public void OnStrokeComplete()
                {
                    TextureData.AfterStroke(Stroke);
                }

                protected Base(Stroke stroke, Texture texture, Brush brush)
                {
                    Stroke = stroke;
                    TextureData = texture.GetTextureMeta();
                    Brush = brush;
                }

                protected Base(Stroke stroke, TextureMeta textureData, Brush brush)
                {
                    Stroke = stroke;
                    TextureData = textureData;
                    Brush = brush;
                }

            }

            public class UV : Base
            {
                public override bool Is3DBrush => false;

                public UV(Stroke stroke, Texture texture, Brush brush) : base(stroke, texture, brush) { }

                public UV(Stroke stroke, TextureMeta textureData, Brush brush) : base(stroke, textureData, brush) { }
            }

            public abstract class WorldSpaceBase : Base
            {
                public abstract GameObject GameObject { get; set; }

                public abstract SkinnedMeshRenderer SkinnedMeshRenderer { get; set; }

                public abstract Mesh Mesh { get; set; }

                public abstract int SubMeshIndexFirst { get; set; }

                public abstract List<int> SelectedSubMeshes { get; set; }

                protected WorldSpaceBase(Stroke stroke, Texture texture, Brush brush) : base(stroke, texture, brush) { }

                public WorldSpaceBase(Stroke stroke, TextureMeta textureData, Brush brush) : base(stroke, textureData, brush) { }
            }

            public class WorldSpace : WorldSpaceBase
            {
                public sealed override GameObject GameObject { get; set; }
                public sealed override SkinnedMeshRenderer SkinnedMeshRenderer { get; set; }
                public sealed override Mesh Mesh { get; set; }

                public override bool Is3DBrush => Brush.Is3DBrush(TextureData);

                private List<int> _selectedSubMeshes = new(1);

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

                public WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, Mesh mesh, int firstSubMeshIndex, GameObject gameObject) : base(stroke, textureData, brush)
                {
                    Mesh = mesh;
                    _selectedSubMeshes.ForceSet(0, firstSubMeshIndex);
                    GameObject = gameObject;
                }

                public WorldSpace(Stroke stroke, Texture texture, Brush brush, Mesh mesh, int subMeshIndexFirst, GameObject gameObject) : base(stroke, texture, brush)
                {
                    Mesh = mesh;
                    SubMeshIndexFirst = subMeshIndexFirst;
                    GameObject = gameObject;
                }

                internal WorldSpace(Stroke stroke, TextureMeta textureData, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndexFirst) : base(stroke, textureData, brush)
                {
                    SkinnedMeshRenderer = skinnedMeshRenderer;
                    SubMeshIndexFirst = subMeshIndexFirst;
                    GameObject = skinnedMeshRenderer.gameObject;
                }

                public WorldSpace(Stroke stroke, Texture texture, Brush brush, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndexFirst) : base(stroke, texture, brush)
                {
                    SkinnedMeshRenderer = skinnedMeshRenderer;
                    SubMeshIndexFirst = subMeshIndexFirst;
                    GameObject = skinnedMeshRenderer.gameObject;
                }
            }

            public class WorldSpaceBufferBlit
            {
                public RenderTexture Texture { get; set; }
                public Shader Shader;
                public GameObject Go;
                public Mesh Mesh { get; set; }

                private readonly List<int> _selectedSubMeshes = new(1);
                public WorldSpaceBufferBlit(RenderTexture texture, Shader shader, MeshFilter mesh, int submeshIndex = 0)
                {
                    Mesh = mesh.mesh;
                    Texture = texture;
                    Shader = shader;
                    Go = mesh.gameObject;
                    _selectedSubMeshes.ForceSet(0, submeshIndex);
                }

                Singleton_PainterCamera TexMGMT => Singleton.Get<Singleton_PainterCamera>();

                public void Paint()
                {
                    TexMGMT.brushRenderer.UseMeshAsBrush(Go, Mesh, _selectedSubMeshes);
                    TexMGMT.Prepare(Texture, Shader).Render();
                }
            }

            public class ForPainterComponent : WorldSpaceBase
            {
                public readonly PainterComponent painter;

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
                    get => new() { painter.selectedSubMesh };
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

                public ForPainterComponent(Stroke stroke, Brush brush, PainterComponent painter) : base(stroke, painter.TexMeta, brush)
                {
                    SkinnedMeshRenderer = painter.skinnedMeshRenderer;
                    Mesh = painter.GetMesh();
                    SubMeshIndexFirst = painter.selectedSubMesh;
                    GameObject = painter.gameObject;
                    this.painter = painter;
                }
            }


        }

        public static void Paint(this Base command) => command.Brush.Paint(command);

        public static T Reset<T>(this T command) where T : Base
        {
            command.strokeAlphaPortion = 1;
            command.usedAlphaBuffer = false;
            return command;
        }

        public static T SetStroke<T>(this T command, Stroke stroke) where T : Base
        {
            command.Stroke = stroke;
            return command;
        }

        public static T SetBrush<T>(this T command, Brush brush) where T : Base
        {
            command.Brush = brush;
            return command;
        }

        public static PainterComponent TryGetPainter<T>(this T command) where T : Base
        {
            var painterCommand = command as ForPainterComponent;
            if (painterCommand != null && painterCommand.painter)
                return painterCommand.painter;

            return null;
        }
    }
}