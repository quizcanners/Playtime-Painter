using System;
using PlayerAndEditorGUI;
using System.Collections;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;

namespace QuizCannersUtilities
{

    public static class ShaderProperty
    {

        #region Base

        public abstract class BaseShaderPropertyIndex : AbstractCfg, IGotDisplayName, IPEGI_ListInspect
        {
            protected int id;
            private string _name;
            public bool nonMaterialProperty;

            public override int GetHashCode() => id;

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                var bi = obj as BaseShaderPropertyIndex;

                return bi != null ? bi.id == id : _name.SameAs(obj.ToString());
            }

            private void UpdateIndex() => id = Shader.PropertyToID(_name);

            public string NameForDisplayPEGI => _name;

            public override string ToString() => _name;

            public abstract void SetOn(Material mat);

            public abstract void SetOn(MaterialPropertyBlock block);

            public Renderer SetOn(Renderer renderer, int materialIndex = 0)
            {
#if UNITY_2018_3_OR_NEWER
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, materialIndex);
                SetOn(block);
                renderer.SetPropertyBlock(block, materialIndex);
#else
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                SetOn(block);
                renderer.SetPropertyBlock(block);
#endif
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

            public override StdEncoder Encode() => new StdEncoder()
                .Add_String("n", _name)
                .Add_IfTrue("nm", nonMaterialProperty);
            
            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "n":
                        _name = data;
                        UpdateIndex();
                        break;
                    case "nm":
                        nonMaterialProperty = data.ToBool();
                        break;
                    default: return false;
                }

                return true;
            }

            protected BaseShaderPropertyIndex()
            {
            }

            protected BaseShaderPropertyIndex(string name)
            {
                _name = name;
                UpdateIndex();
            }

            protected BaseShaderPropertyIndex(string name, bool isNonMaterialProperty)
            {
                _name = name;
                nonMaterialProperty = isNonMaterialProperty;
                UpdateIndex();
            }

        }

        public static bool Has<T>(this Material mat, T property) where T : BaseShaderPropertyIndex =>
            mat.HasProperty(property.GetHashCode());

        #endregion

        #region Generic Extensions

        public abstract class ShaderPropertyIndexGeneric<T> : BaseShaderPropertyIndex
        {

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

            public void SetOn(MaterialPropertyBlock block, T value)
            {
                lastValue = value;
                SetOn(block);
            }

            public void SetGlobal(T value) => GlobalValue = value;

            public T GetGlobal(T value) => GlobalValue;

            protected ShaderPropertyIndexGeneric()
            {
            }

            protected ShaderPropertyIndexGeneric(string name) : base(name)
            {
            }

            protected ShaderPropertyIndexGeneric(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }
        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, ShaderPropertyIndexGeneric<T> property)
        {
            property.SetOn(block);
            return block;
        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, ShaderPropertyIndexGeneric<T> property, T value)
        {
            property.SetOn(block, value);
            return block;
        }

        public static Material Set<T>(this Material mat, ShaderPropertyIndexGeneric<T> property)
        {
            property.SetOn(mat);
            return mat;
        }

        public static Material Set<T>(this Material mat, ShaderPropertyIndexGeneric<T> property, T value) =>
            property.SetOn(mat, value);

        public static Renderer Set<T>(this Renderer renderer, ShaderPropertyIndexGeneric<T> property, T value) =>
            property.SetOn(renderer, value);

        public static T Get<T>(this Material mat, ShaderPropertyIndexGeneric<T> property) => property.Get(mat);

        #endregion

        #region Float

        public class FloatValue : ShaderPropertyIndexGeneric<float> {

            public override void SetOn(Material material) => material.SetFloat(id, lastValue);

            public override float Get(Material material) => material.GetFloat(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetFloat(id, lastValue);

            public override float GlobalValue
            {
                get { return Shader.GetGlobalFloat(id); }
                set
                {
                    Shader.SetGlobalFloat(id, value);
                    lastValue = value;
                }
            }

            public FloatValue()
            {
            }

            public FloatValue(string name) : base(name)
            {
            }

            public FloatValue(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }

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

            public ColorValue()
            {
                lastValue = Color.grey;
            }

            public ColorValue(string name) : base(name)
            {
                lastValue = Color.grey;
            }

            public ColorValue(string name, Color startingColor) : base(name)
            {
                lastValue = startingColor;
            }

            public ColorValue(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }


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

            public VectorValue()
            {
            }

            public VectorValue(string name) : base(name)
            {
            }

            public VectorValue(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }
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


            public MatrixValue()
            {
            }

            public MatrixValue(string name) : base(name)
            {
            }

            public MatrixValue(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }
        }

        #endregion

        #region Texture

        public class TextureValue : ShaderPropertyIndexGeneric<Texture>
        {

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

            public TextureValue()
            {
            }

            public TextureValue(string name, string tag) : base(name)
            {
                AddUsageTag(tag);
            }

            public TextureValue(string name) : base(name)
            {
            }

            public TextureValue(string name, bool nonMaterial) : base(name, nonMaterial)
            {
            }

            #region Encode & Decode

            public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode())
                .Add("tgs", _usageTags);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "tgs":
                        data.Decode_List(out _usageTags);
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion
        }

        public static Vector2 GetOffset(this Material mat, TextureValue property) => property.GetOffset(mat);

        public static Vector2 GetTiling(this Material mat, TextureValue property) => property.GetTiling(mat);

        public static void SetOffset(this Material mat, TextureValue property, Vector2 value) =>
            property.SetOffset(mat, value);

        public static void SetTiling(this Material mat, TextureValue property, Vector2 value) =>
            property.SetTiling(mat, value);

        #endregion
    }

    #region Shader Tags
    public class ShaderTag : IGotDisplayName
    {
        public readonly string tag;
        public string NameForDisplayPEGI => tag;
        public bool Has(Material mat) => mat.HasTag(tag);
        public string Get(Material mat, bool searchFallBacks = false, string defaultValue = "") => mat.GetTag(tag, searchFallBacks, defaultValue);

        public string Get(Material mat, ShaderProperty.BaseShaderPropertyIndex property,
            bool searchFallBacks = false) =>
            Get(mat, property.NameForDisplayPEGI, searchFallBacks);

        public string Get(Material mat, string prefix, bool searchFallBacks = false) =>
            mat.GetTag(prefix + tag, searchFallBacks);

        public ShaderTag(string nTag)
        {
            tag = nTag;
        }


    }

    public class ShaderTagValue : IGotDisplayName
    {
        public ShaderTag tag;
        public string NameForDisplayPEGI => value;
        public readonly string value;

        public bool Has(Material mat, bool searchFallBacks = false) =>
            value.Equals(tag.Get(mat, searchFallBacks));

        public bool Has(Material mat, ShaderProperty.BaseShaderPropertyIndex property, bool searchFallBacks = false) =>
            value.Equals(tag.Get(mat, property, searchFallBacks));

        public ShaderTagValue(string newValue, ShaderTag nTag)
        {
            value = newValue;
            tag = nTag;
        }
    }

    public static partial class ShaderTags
    {

        public static readonly ShaderTag Queue = new ShaderTag("Queue");

        public static class Queues 
        {
            public static readonly ShaderTagValue Background = new ShaderTagValue("Background", Queue);
            public static readonly ShaderTagValue Geometry = new ShaderTagValue("Geometry", Queue);
            public static readonly ShaderTagValue AlphaTest = new ShaderTagValue("Geometry", Queue);
            public static readonly ShaderTagValue Transparent = new ShaderTagValue("Transparent", Queue);
            public static readonly ShaderTagValue Overlay = new ShaderTagValue("Overlay", Queue);
        }
    }

    public static class ShaderTagExtensions
    {
        public static string Get(this Material mat, ShaderTag tag, bool searchFallBacks = false, string defaultValue = "") =>
            tag.Get(mat, searchFallBacks, defaultValue);

        public static string Get(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, propertyPrefix, searchFallBacks);

        public static string Get(this Material mat, string prefix, ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, prefix, searchFallBacks);

        public static bool Has(this Material mat, ShaderTag tag) => tag.Has(mat);

        public static bool Has(this Material mat, ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, searchFallBacks);

        public static bool Has(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, propertyPrefix, searchFallBacks);
    }
    #endregion
}


namespace PlayerAndEditorGUI
{
    // ReSharper disable InconsistentNaming
#pragma warning disable 1692
#pragma warning disable IDE1006

    public static partial class pegi
    {
        #if PEGI
        public static bool toggle(this Material mat, string keyword)
        {
            var val = Array.IndexOf(mat.shaderKeywords, keyword) != -1;

            if (!keyword.toggleIcon(ref val)) return false;

            if (val)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            return true;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name, float min, float max)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val, min, max))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.ColorValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.VectorValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.TextureValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }
        #endif
    }

}