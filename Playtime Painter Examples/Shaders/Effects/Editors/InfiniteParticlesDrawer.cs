using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

public class InfiniteParticlesDrawerGUI : PEGI_Inspector_Material {

    public const string FadeOutTag = "_FADEOUT";
     
    public override bool Inspect(Material mat) {

        var changed = pegi.toggleDefaultInspector(mat);

        mat.toggle("SCREENSPACE").nl(ref changed);
        mat.toggle("DYNAMIC_SPEED").nl(ref changed);
        mat.toggle(FadeOutTag).nl(ref changed);

        var fo = mat.HasTag(FadeOutTag, false);

        if (fo)
            "When alpha is one, the graphic will be invisible.".writeHint();

        pegi.nl();

        var dynamicSpeed = mat.GetKeyword("DYNAMIC_SPEED");

        pegi.nl();
        
        if (!dynamicSpeed)
            mat.edit(speed, "speed", 0, 60).nl(ref changed);
        else
        {
            mat.edit(time, "Time").nl(ref changed);
            "It is expected that time Float will be set via script. Parameter name is _CustomTime. ".writeHint();
            pegi.nl();
        }

        mat.edit(tiling, "Tiling", 0.1f, 20f).nl(ref changed);

        mat.edit(upscale, "Scale", 0.1f, 1).nl(ref changed);

        mat.editTexture("_MainTex").nl(ref changed);
        mat.editTexture("_MainTex2").nl(ref changed);


        return changed;
    }
    
    private static readonly ShaderProperty.FloatValue speed = new ShaderProperty.FloatValue("_Speed");
    private static readonly ShaderProperty.FloatValue time = new ShaderProperty.FloatValue("_CustomTime");
    private static readonly ShaderProperty.FloatValue tiling = new ShaderProperty.FloatValue("_Tiling");
    private static readonly ShaderProperty.FloatValue upscale = new ShaderProperty.FloatValue("_Upscale");
    
}

