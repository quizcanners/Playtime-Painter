using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using StoryTriggerData;
using PlayerAndEditorGUI;




[Flags]
public enum BrushMask
{
	R = 1, G = 2, B = 4, A = 8
}
		
[Serializable]
public class BrushConfig : abstract_STD
{

    public override stdEncoder Encode() {
        stdEncoder cody = new stdEncoder();

        // No use for this one yet

        Debug.Log("Brush is saved trough serializarion at the moment, but there is a function to provide stroke data");

        return cody;
    }

    public override void Decode(string tag, string data) {

        switch (tag) {
            case "typeRT": brushType_rt = data.ToInt(); break;

            case "size2D": Brush2D_Radius = data.ToFloat(); break;
            case "size3D": Brush3D_Radius = data.ToFloat(); break;

            case "useMask" : useMask = data.ToBool(); break;

            case "mask": mask = (BrushMask)data.ToInt();  break;

            case "mode": bliTMode = data.ToInt(); break;

            case linearColor.toryTag: color.Reboot(data); break;

            case "source": selectedSourceTexture = data.ToInt(); break;

            case "blur": blurAmount= data.ToFloat(); break;

            case "decA": decalAngle= data.ToFloat(); break;
            case "decNo": selectedDecal= data.ToInt(); break;

            case "Smask": selectedSourceMask= data.ToInt(); break;
            case "maskTil": maskTiling= data.ToFloat(); break;
            case "maskFlip": flipMaskAlpha= data.ToBool(); break;

            case "hard": Hardness= data.ToFloat(); break;
            case "speed": speed= data.ToFloat(); break;
            case "smooth": Smooth= data.ToBool(); break;
            case "maskOff": maskOffset = data.ToVector2(); break;
        }
          

    }

    public const string storyTag = "brush";
    public override string getDefaultTagName() { return storyTag; }

    public bool GetMask(BrushMask flag)
    {
        return ((mask & flag) != 0);
    }

    public void MaskToggle(BrushMask flag)
    {
        mask ^= flag;
    }

    public void MaskSet(BrushMask flag, bool to)
    {
        if (to)
            mask |= flag;
        else
            mask &= ~flag;
    }

    public BrushMask mask;

	public int bliTMode;
    public int brushType_rt;

    public float repaintDelay = 0.016f;
    public int selectedSourceTexture = 0;
    public int selectedSourceMask = 0;
	public bool useMask = false;
    [NonSerialized]
    public Vector2 maskOffset;
    public bool randomMaskOffset;

    public int selectedDecal = 0;
    public float maskTiling = 1;
    public float Hardness = 256;
	public float blurAmount = 1;
    public float decalAngle = 0;
    public bool decalRandomRotation = true;
    public bool flipMaskAlpha = false;
    public bool decalContinious = false;
    public bool IndependentCPUblit = false;

    public float Brush3D_Radius = 16;
    public float Brush2D_Radius = 16;

    public float Size (bool worldSpace) {
        return (worldSpace ? Brush3D_Radius : Brush2D_Radius);
    }

   

    public float speed = 10;
    public bool MB1ToLinkPositions;
    public bool Smooth;
    //public bool LazyBrush;
    //public bool sphereBrush;
    public bool DontRedoMipmaps;

    public linearColor color;

    [NonSerialized]
    public Vector2 SampledUV;

    public BrushConfig()
    {
        Smooth = true;
        color = new linearColor(Color.black);
        mask = new BrushMask();
        mask |= BrushMask.R | BrushMask.G | BrushMask.B;
    }

	public bool ColorSlider(BrushMask m, ref float chanel)	{

      

		pegi.write (m.getIcon (), 25);
		bool changed = pegi.edit(ref chanel, 0, 1);

		pegi.newLine();

		return changed;
	}

	public bool ColorSlider(BrushMask m, ref float chanel, Texture icon, bool slider)
    {
        if (icon == null)
            icon = m.getIcon();

       

        bool changed = false;
        bool maskVal = GetMask(m);
        if (maskVal ? pegi.Click(icon, 25) : pegi.Click(m.ToString() + " disabled"))
        {
            MaskToggle(m);
            changed = true;
        }

		if ((slider) && (GetMask(m)))
            changed |= pegi.edit(ref chanel, 0, 1);
		
        pegi.newLine();

        return changed;
    }

    public override void PEGI() {
        base.PEGI();
    }

	public bool ColorSliders_PEGI()  {
		bool changed = false;

		changed |= ColorSlider (BrushMask.R, ref color.r, null,true);
		changed |= ColorSlider (BrushMask.G, ref color.g, null,true);
		changed |= ColorSlider (BrushMask.B, ref color.b, null,true);
		changed |= ColorSlider (BrushMask.A, ref color.a, null,true);
			
		return changed;
	}

}

