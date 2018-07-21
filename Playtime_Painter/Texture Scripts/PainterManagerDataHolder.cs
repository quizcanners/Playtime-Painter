using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class PainterManagerDataHolder : STD_ReferancesHolder
    {
        public static PainterManagerDataHolder dataHolder;

        [SerializeField]public PainterManager scenePainterManager;

        public Shader br_Blit = null;
        public Shader br_Add = null;
        public Shader br_Copy = null;
        public Shader pixPerfectCopy = null;
        public Shader brushRendy_bufferCopy = null;
        public Shader Blit_Smoothed = null;
        public Shader br_Multishade = null;
        public Shader br_BlurN_SmudgeBrush = null;
        public Shader br_ColorFill = null;

        public Shader mesh_Preview = null;
        public Shader br_Preview = null;
        public Shader TerrainPreview = null;

        public override bool PEGI()
        {
            bool changed =  base.PEGI();

            "Painter Data".nl();
            return changed;
        }

        public void OnEnable()
        {
            dataHolder = this;


#if BUILD_WITH_PAINTER || UNITY_EDITOR
            if (pixPerfectCopy == null) pixPerfectCopy = Shader.Find("Editor/PixPerfectCopy");

            if (Blit_Smoothed == null) Blit_Smoothed = Shader.Find("Editor/BufferBlit_Smooth");

            if (brushRendy_bufferCopy == null) brushRendy_bufferCopy = Shader.Find("Editor/BufferCopier");

            if (br_Blit == null) br_Blit = Shader.Find("Editor/br_Blit");

            if (br_Add == null) br_Add = Shader.Find("Editor/br_Add");

            if (br_Copy == null) br_Copy = Shader.Find("Editor/br_Copy");

            if (br_Multishade == null) br_Multishade = Shader.Find("Editor/br_Multishade");

            if (br_BlurN_SmudgeBrush == null) br_BlurN_SmudgeBrush = Shader.Find("Editor/BlurN_SmudgeBrush");

            if (br_ColorFill == null) br_ColorFill = Shader.Find("Editor/br_ColorFill");

            if (br_Preview == null) br_Preview = Shader.Find("Editor/br_Preview");

            if (mesh_Preview == null) mesh_Preview = Shader.Find("Editor/MeshEditorAssist");
            
            TerrainPreview = Shader.Find("Editor/TerrainPreview");
#endif

           // Debug.Log("Painter Manager Enabled");

        }
    }
}