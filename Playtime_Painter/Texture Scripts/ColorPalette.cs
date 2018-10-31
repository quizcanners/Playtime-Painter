using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using UnityEngine;

namespace Playtime_Painter
{

    [Serializable]
    public class ColorScheme : Abstract_STD, IPEGI, IGotName, IPEGI_ListInspect
    {

        protected static PainterDataAndConfig Cfg => PainterCamera.Data;
        protected static BrushConfig GlobalBrush => Cfg.brushConfig;

        public int lastPicked = -1;
        public string PaletteName;
        public List<Color> colors = new List<Color>();


        #region Inspector
#if PEGI

        public string NameForPEGI { get { return PaletteName; } set { PaletteName = value; } }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {

            bool changed = this.inspect_Name();

            if (icon.Enter.BGColor(colors.TryGet(0).ToOpaque()).Click().RestoreBGColor())
                edited = ind;

            return changed;
        }

        Color EditColor(Color col)
        {

            pegi.edit(ref col);

            if (icon.Save.Click("From brush"))
                col = GlobalBrush.colorLinear.ToGamma();

            if (icon.Load.Click("To brush"))
                GlobalBrush.colorLinear.From(col);

           

            return col;
        }

        public bool Inspect() {
            bool changed = false;

            changed |= PaletteName.edit_List(ref colors, EditColor);

            return changed;
        }

        public void PickerPEGI() {

      

            int rowLimit = pegi.paintingPlayAreaGUI ? 6 : (int)((Screen.width-55) / 32f);

            int rowCount = 0;
            for (int i = 0; i < colors.Count; i++) {

                var col = colors[i];

                if (lastPicked == i) {
                    if (icon.Save.BGColor(col.ToOpaque()).Click("Save changes").RestoreBGColor())
                        colors[i] = GlobalBrush.colorLinear.ToGamma();
                }
                else
                if (col.ToOpaque().Click()) {
                    lastPicked = i;
                    GlobalBrush.colorLinear.From(col);
                }

                rowCount++;
                if (rowCount > rowLimit)
                {
                    rowCount = 0;
                    pegi.nl();
                }
            }

            var curColor = GlobalBrush.colorLinear.ToGamma();

            if (icon.SaveAsNew.BGColor(GlobalBrush.colorLinear.ToGamma().ToOpaque()).Click().RestoreBGColor())
                colors.Add(curColor);

            pegi.nl();

        }


#endif
        #endregion

        #region Encode/Decode

        public override StdEncoder Encode() => new StdEncoder()
            .Add_String("n", PaletteName)
            .Add("cols", colors);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": PaletteName = data; break;
                case "cols": data.DecodeInto(out colors); break;
                default: return false;
            }
            return true;
        }

        #endregion

    }

}
