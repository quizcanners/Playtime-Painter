using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using UnityEngine.SceneManagement;
using StoryTriggerData;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    [Serializable]
    public class TileableAtlasingControllerPlugin : PainterManagerPluginBase
    {

        public static TileableAtlasingControllerPlugin inst;

        public override string ToString()
        {
            return "Tilable Atlasing";
        }

        public List<AtlasTextureCreator> atlases;

        public List<MaterialAtlases> atlasedMaterials;

        [SerializeField]
        protected bool showAtlasedMaterial;
        [SerializeField]
        protected bool showAtlases;

        [SerializeField]
        protected int browsedAtlas;


        public override void OnEnable()
        {
            inst = this;
            if (atlases == null)
                atlases = new List<AtlasTextureCreator>();

            if (atlasedMaterials == null)
                atlasedMaterials = new List<MaterialAtlases>();
        }

        public override bool ConfigTab_PEGI()
        {
            bool changed = false;

            var painter = PlaytimePainter.inspectedPainter;

            if (painter.isAtlased())
            {

                "***** Selected Material Atlased *****".nl();
#if UNITY_EDITOR

                var m = painter.getMesh();
                if (m != null && AssetDatabase.GetAssetPath(m).Length == 0)
                {
                    "Atlased Mesh is not saved".nl();
                    var n = m.name;
                    if ("Mesh Name".edit(80, ref n))
                        m.name = n;
                    if (icon.save.Click().nl())
                        painter.SaveMesh();
                }

#endif


                var atlPlug = painter.getPlugin<TileableAtlasingPainterPlugin>();

                if ("Undo Atlasing".Click())
                {
                    painter.getRenderer().sharedMaterials = atlPlug.preAtlasingMaterials;

                    if (atlPlug.preAtlasingMesh != null)
                        painter.meshFilter.mesh = atlPlug.preAtlasingMesh;
                    painter.savedEditableMesh = atlPlug.preAtlasingSavedMesh;

                    atlPlug.preAtlasingMaterials = null;
                    atlPlug.preAtlasingMesh = null;
                    painter.getRenderer().sharedMaterial.DisableKeyword(PainterConfig.UV_ATLASED);
                }

                if ("Not Atlased".Click().nl())
                {
                    atlPlug.preAtlasingMaterials = null;
                    painter.getRenderer().sharedMaterial.DisableKeyword(PainterConfig.UV_ATLASED);
                }

                pegi.newLine();

            }
            else if ("Atlased Materials".foldout(ref showAtlasedMaterial).nl())
            {

                showAtlases = false;

        

                changed |= atlasedMaterials.PEGI(ref painter.selectedAtlasedMaterial, true).nl();

               


            }

            if ("Atlases".foldout(ref showAtlases))
            {

                if ((browsedAtlas > -1) && (browsedAtlas >= atlases.Count))
                    browsedAtlas = -1;

                pegi.newLine();

                if (browsedAtlas > -1)
                {
                    if (icon.Back.Click(25))
                        browsedAtlas = -1;
                    else
                        atlases[browsedAtlas].PEGI();
                }
                else
                {
                    pegi.newLine();
                    for (int i = 0; i < atlases.Count; i++)
                    {
                        if (icon.Delete.Click(25))
                            atlases.RemoveAt(i);
                        else
                        {
                            pegi.edit(ref atlases[i].name);
                            if (icon.Edit.Click(25).nl())
                                browsedAtlas = i;
                        }
                    }

                    if (icon.Add.Click(30))
                        atlases.Add(new AtlasTextureCreator("new"));

                }




              
            }

          

            return changed;

        }

    }
}