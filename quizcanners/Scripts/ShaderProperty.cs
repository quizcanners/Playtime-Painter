using PlayerAndEditorGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCannersUtilities {

    public static class ShaderProperty {

        #region Base
        public abstract class BaseShaderPropertyIndex : AbstractStd, IGotDisplayName, IPEGI_ListInspect
        {
            protected int id;
            private string _name;

            public override int GetHashCode() => id;

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                var bi = obj as BaseShaderPropertyIndex;

                return bi != null ? bi.id == id : _name.SameAs(obj.ToString());
            }
            private void UpdateIndex() => id = Shader.PropertyToID(_name);

            protected BaseShaderPropertyIndex() { }
            protected BaseShaderPropertyIndex(string name)
            {
                _name = name;
                UpdateIndex();
            }

            public string NameForDisplayPEGI => _name;

            public override string ToString() => _name;

            public abstract void SetOn(Material mat);
            
            public abstract void SetOn(MaterialPropertyBlock block);

            public Renderer SetOn(Renderer renderer, int materialIndex = 0) {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, materialIndex);
                SetOn(block);
                renderer.SetPropertyBlock(block, materialIndex);
                return renderer;
            }

#if PEGI
            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                "[{0}] {1}".F(id, _name).write();
                if (icon.Refresh.Click("Update Index (Shouldn't be necessary)"))
                    UpdateIndex();

                return false;
            }
#endif

            public override StdEncoder Encode() => new StdEncoder().Add_String("n", _name);


            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "n": _name = data; UpdateIndex(); break;
                    default: return false;
                }

                return true;
            }
        }

        public static bool Has<T>(this Material mat, T property) where T : BaseShaderPropertyIndex =>
            mat.HasProperty(property.GetHashCode());
        #endregion
        
        #region Generic Extensions
        public abstract class ShaderPropertyIndexGeneric<T> : BaseShaderPropertyIndex {
           
            public T lastValue;
            
            public abstract T Get(Material mat);

            public abstract T GlobalValue { get; set; }
            
            public virtual Material SetOn(Material material, T value)
            {
                lastValue = value;

                if (material)
                    SetOn(material);

                return material;
            }

            public Renderer SetOn(Renderer renderer, T value)
            {
                lastValue = value;
                SetOn(renderer);
                return renderer;
            }

            public void SetOn(MaterialPropertyBlock block, T value) {
                lastValue = value;
                SetOn(block);
            }
            
            public void SetGlobal(T value) => GlobalValue = value;

            public T GetGlobal(T value) => GlobalValue;

            protected ShaderPropertyIndexGeneric(string name) : base(name) { }

            protected ShaderPropertyIndexGeneric() { }
        }

        public static Material Set<T>(this Material mat, ShaderPropertyIndexGeneric<T> property) { property.SetOn(mat); return mat; }
        
        public static Material Set<T>(this Material mat, ShaderPropertyIndexGeneric<T> property, T value) => property.SetOn(mat, value);

        public static Renderer Set<T>(this Renderer renderer, ShaderPropertyIndexGeneric<T> property, T value) => property.SetOn(renderer, value);

        public static T Get<T>(this Material mat, ShaderPropertyIndexGeneric<T> property) => property.Get(mat);
        #endregion
        
        #region Float
        public class FloatValue : ShaderPropertyIndexGeneric<float>
        {

            public override void SetOn(Material material) => material.SetFloat(id, lastValue);
            
            public override float Get(Material material) => material.GetFloat(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetFloat(id, lastValue);
            
            public override float GlobalValue {
                get { return Shader.GetGlobalFloat(id); }
                set { Shader.SetGlobalFloat(id, value); lastValue = value; }
            }

            public FloatValue(string name) : base(name) { }

            public FloatValue() { }
        }

    
        #endregion

        #region Color
        public class ColorValue : ShaderPropertyIndexGeneric<Color>
        {

            public override void SetOn(Material material) => material.SetColor(id, lastValue);

            public override Color Get(Material material) => material.GetColor(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetColor(id, lastValue);
            
            public override Color GlobalValue
            {
                get { return Shader.GetGlobalColor(id); }
                set { Shader.SetGlobalColor(id, value); }
            }

            public ColorValue(string name) : base(name) { lastValue = Color.grey; }

            public ColorValue(string name, Color startingColor) : base(name) { lastValue = startingColor; }
            
            public ColorValue() { lastValue = Color.grey; }
        }
        #endregion

        #region Vector
        public class VectorValue : ShaderPropertyIndexGeneric<Vector4>
        {

            public override void SetOn(Material material) => material.SetVector(id, lastValue);
            
            public override void SetOn(MaterialPropertyBlock block) => block.SetVector(id, lastValue);
            
            public override Vector4 Get(Material mat) => mat.GetVector(id);

            public override Vector4 GlobalValue
            {
                get { return Shader.GetGlobalVector(id); }
                set { Shader.SetGlobalVector(id, value); }
            }

            public VectorValue(string name) : base(name) { }

            public VectorValue() { }
        }

        #endregion

        #region Matrix
        public class MatrixValue : ShaderPropertyIndexGeneric<Matrix4x4>
        {

            public override void SetOn(Material material) => material.SetMatrix(id, lastValue);

            public override void SetOn(MaterialPropertyBlock block) => block.SetMatrix(id, lastValue);

            public override Matrix4x4 Get(Material mat) => mat.GetMatrix(id);

            public override Matrix4x4 GlobalValue
            {
                get { return Shader.GetGlobalMatrix(id); }
                set { Shader.SetGlobalMatrix(id, value); }
            }

            public MatrixValue(string name) : base(name) { }

            public MatrixValue() { }
        }

        #endregion

        #region Texture
        public class TextureValue : ShaderPropertyIndexGeneric<Texture>  {

            public override Texture Get(Material mat) => mat.GetTexture(id);
            
            public override void SetOn(Material material) => material.SetTexture(id, lastValue);

            public override void SetOn(MaterialPropertyBlock block) => block.SetTexture(id, lastValue);

            public override Texture GlobalValue
            {
                get { return Shader.GetGlobalTexture(id); }
                set { Shader.SetGlobalTexture(id, value); }
            }

            #region Texture Specific
            private List<string> _usageTags = new List<string>();

            public TextureValue AddUsageTag(string value)
            {
                if (!_usageTags.Contains(value)) _usageTags.Add(value);
                return this;
            }

            public bool HasUsageTag(string tag) => _usageTags.Contains(tag);
            
            public Vector2 GetOffset(Material mat) => mat ? mat.GetTextureOffset(id) : Vector2.zero;

            public Vector2 GetTiling(Material mat) => mat ? mat.GetTextureScale(id) : Vector2.one;

            public void SetOffset(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureOffset(id, value);
            }

            public void SetTiling(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureScale(id, value);
            }
            #endregion

            public TextureValue(string name, string tag) : base(name) { AddUsageTag(tag); }

            public TextureValue(string name) : base(name) { }

            public TextureValue() { }

            #region Encode & Decode
            public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode())
                .Add("tgs", _usageTags);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "tgs": data.Decode_List(out _usageTags); break;
                    default: return false;
                }
                return true;
            }
            #endregion
        }
        
        public static Vector2 GetOffset(this Material mat, TextureValue property) => property.GetOffset(mat);

        public static Vector2 GetTiling(this Material mat, TextureValue property) => property.GetTiling(mat);

        public static void SetOffset(this Material mat, TextureValue property, Vector2 value) => property.SetOffset(mat, value);

        public static void SetTiling(this Material mat, TextureValue property, Vector2 value) => property.SetTiling(mat, value);
        #endregion
    }
}
