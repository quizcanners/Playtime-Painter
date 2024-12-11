using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{
    public class ColorPicker : ICfg, IPEGI, IGotStringId, IPEGI_ListInspect
    {
        private static Brush GlobalBrush => Painter.Data.Brush;
        
        public string paletteName;
        private List<Color> _colors = new();
        
        #region Inspector

         private int _lastPicked = -1;

        public override string ToString() => paletteName;

        public string StringId { get { return paletteName; } set { paletteName = value; } }

        public void InspectInList(ref int edited, int ind)
        {
            this.inspect_Name();

            using (pegi.SetBgColorDisposable(_colors.TryGet(0).Alpha(1)))
            {
                if (Icon.Enter.Click())
                    edited = ind;
            }
        }

        private Color EditColor(Color col)
        {
            pegi.Edit(ref col);

            if (Icon.Save.Click("From brush"))
                col = GlobalBrush.Color;

            if (Icon.Load.Click("To brush"))
                GlobalBrush.Color = col;

            return col;
        }

        void IPEGI.Inspect() 
        {
            paletteName.PL().Edit_List(_colors, EditColor);
        }

        public void PickerPEGI() {

            var rowLimit = pegi.PaintingGameViewUI ? 6 : (int)((Screen.width-55) / 32f);

            var rowCount = 0;
            for (var i = 0; i < _colors.Count; i++) {

                var col = _colors[i];

                if (_lastPicked == i)
                {
                    using (pegi.SetBgColorDisposable(col.Alpha(1)))
                    {
                        if (Icon.Save.Click("Save changes"))
                            _colors[i] = GlobalBrush.Color;
                    }
                }
                else
                    pegi.Click(col.Alpha(1)).OnChanged(() =>
                    {
                        _lastPicked = i;
                        GlobalBrush.Color = col;
                    });

                rowCount++;
                if (rowCount > rowLimit)
                {
                    rowCount = 0;
                    pegi.Nl();
                }
            }

            var curColor = GlobalBrush.Color;

            using (pegi.SetBgColorDisposable(curColor.Alpha(1)))
            {
                Icon.SaveAsNew.Click(() => _colors.Add(curColor));
            }

            pegi.Nl();

        }
        
        #endregion

        #region Encode/Decode

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", paletteName)
            .Add("cols", _colors);

        public void DecodeTag(string key, CfgData data)
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
