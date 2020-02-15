using System.Collections;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.XR.WSA.Persistence;

namespace PlaytimePainter
{
    public static class PaintCommand
    {

        public class Painter : WorldSpace
        {
            public override GameObject GameObject
            {
                get
                {
                    return  painter.gameObject;
                }
                set
                {
                }
            }
            
            public override SkinnedMeshRenderer SkinnedMeshRenderer
            {
                get
                {
                    return  painter.skinnedMeshRenderer;
                }
                set
                {
                }
            }
            
            public override Mesh Mesh
            {
                get
                {
                    return  painter.GetMesh();
                }
                set
                {
                }
            }
            
            public override List<int> SelectedSubmeshes
            {
                get
                {
                    return new List<int>() { painter.selectedSubMesh };
                }
                set
                {
                }
            }

            public override int SubMeshIndexFirst
            {
                get
                {
                    return painter.selectedSubMesh;
                }
                set
                {
                }
            }

            public Painter(Stroke stroke, Brush brush, PlaytimePainter painter) : base(stroke, painter.TexMeta, brush, painter.skinnedMeshRenderer, 0, painter.gameObject)
            {
                SkinnedMeshRenderer = painter.skinnedMeshRenderer;
                Mesh = painter.GetMesh();
                SubMeshIndexFirst = painter.selectedSubMesh;
                GameObject = painter.gameObject;
                this.painter = painter;
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

            public override bool Is3DBrush => painter ? painter.Is3DBrush(brush) : brush.Is3DBrush(textureData);

            public virtual List<int> SelectedSubmeshes
            {
                get;
                set;
            }

            public virtual int SubMeshIndexFirst
            {
                get
                {
                   return painter ? painter.selectedSubMesh: SelectedSubmeshes.TryGet(0);
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

        public static T Reset<T>(this T command) where T : UV
        {
            command.strokeAlphaPortion = 1;
            command.usedAlphaBuffer = false;
            return command;
        }

        public class UV
        {
            public virtual Stroke stroke { get; set; }
            
            private TextureMeta _texMeta;

            public virtual TextureMeta textureData{get;set;}

            public virtual Brush brush { get; set; }
            
            public PlaytimePainter painter { get; protected set; }

            public bool usedAlphaBuffer;
            public float strokeAlphaPortion = 1;

            public virtual bool Is3DBrush => false; 

            public void OnStrokeComplete()
            {
                textureData.AfterStroke(stroke);
            }

            public UV(Stroke stroke, Brush brush, PlaytimePainter painter)
            {
                this.stroke = stroke;
                textureData = painter.TexMeta;
                this.brush = brush;
                this.painter = painter;
            }

            public UV(Stroke stroke, Texture texture, Brush brush)
            {
                this.stroke = stroke;
                textureData = texture.GetTextureMeta();
                this.brush = brush;
            }

            public UV(Stroke stroke, TextureMeta textureData, Brush brush)
            {
                this.stroke = stroke;
                this.textureData = textureData;
                this.brush = brush;
            }
        }

        public static T SetStroke<T>(this T command, Stroke stroke) where T : UV
        {
            command.stroke = stroke;
            return command;
        }

        public static T SetBrush<T>(this T command, Brush brush) where T : UV
        {
            command.brush = brush;
            return command;
        }

    }
}