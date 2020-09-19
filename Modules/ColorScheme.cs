using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter
{

    public class ColorScheme : ICfg, IPEGI, IGotName, IPEGI_ListInspect
    {
        private static PainterDataAndConfig Cfg => PainterCamera.Data;
        private static Brush GlobalBrush => Cfg.Brush;
        
        public string paletteName;
        private List<Color> _colors = new List<Color>();
        
        #region Inspector

         private int _lastPicked = -1;

        public string NameForPEGI { get { return paletteName; } set { paletteName = value; } }

        public bool InspectInList(IList list, int ind, ref int edited)
        {

            var changed = this.inspect_Name();

            if (icon.Enter.BgColor(_colors.TryGet(0).ToOpaque()).Click().RestoreBGColor())
                edited = ind;

            return changed;
        }

        Color EditColor(Color col)
        {

            pegi.edit(ref col);

            if (icon.Save.Click("From brush"))
                col = GlobalBrush.Color;

            if (icon.Load.Click("To brush"))
                GlobalBrush.Color = col;

            return col;
        }

        public bool Inspect() {

            var changed = false;

            paletteName.edit_List(ref _colors, EditColor).changes(ref changed);

            return changed;

        }

        public void PickerPEGI() {

            var rowLimit = pegi.PaintingGameViewUI ? 6 : (int)((Screen.width-55) / 32f);

            var rowCount = 0;
            for (var i = 0; i < _colors.Count; i++) {

                var col = _colors[i];

                if (_lastPicked == i) {
                    if (icon.Save.BgColor(col.ToOpaque()).Click("Save changes").RestoreBGColor())
                        _colors[i] = GlobalBrush.Color;
                }
                else
                if (col.ToOpaque().Click()) {
                    _lastPicked = i;
                    GlobalBrush.Color = col;
                }

                rowCount++;
                if (rowCount > rowLimit)
                {
                    rowCount = 0;
                    pegi.nl();
                }
            }

            var curColor = GlobalBrush.Color;

            if (icon.SaveAsNew.BgColor(curColor.ToOpaque()).Click().RestoreBGColor())
                _colors.Add(curColor);

            pegi.nl();

        }
        
        #endregion

        #region Encode/Decode

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", paletteName)
            .Add("cols", _colors);

        public void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "n": paletteName = data.ToString(); break;
                case "cols": data.ToList(out _colors); break;
            }
        }
        

        #endregion

    }

}
