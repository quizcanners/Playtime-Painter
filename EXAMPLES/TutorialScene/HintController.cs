using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Playtime_Painter;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class HintController : MonoBehaviour
    {

        string text = "";

        HintStage stage;

        enum HintStage { enableTool, unlockTexture, draw, addTool, addTexture, renderTexture, WellDone }

        public GameObject picture;
        public GameObject pill;
        public GameObject cube;
        public float timer = 5f;

        void SetStage(HintStage st)  {
            stage = st;
            string ntext = "Well Done! Remember to save your textures before entering/exiting playmode.";
            string mb = (Application.isPlaying) ? "RIGHT MOUSE BUTTON" : "LEFT MOUSE BUTTON";

            switch (stage) {
                case HintStage.enableTool:
                    ntext = "Select the central cube with " + mb + " and click 'On/Off' to start using painter. Unlock texture if locked."; break;
                case HintStage.unlockTexture:
                    ntext = "Unlock texture (lock icon next to it)"; break;
                case HintStage.draw:
                    ntext = "Draw on the cube. \n You can LOCK EDITING for selected texture."; break;
                case HintStage.addTool:
                    ntext = "Picture to the right has no tool attached. \n Select it and \n Click 'Add Component'->'Mesh'->'Playtime Painter'"; break;
                case HintStage.addTexture:
                    ntext = "Pill on the left has no texture. Select it with " + mb + " and click 'Create Texture' icon"; break;
                case HintStage.renderTexture:
                    int size = PainterCamera.renderTextureSize;
                    ntext = "Change MODE to GPU blit. \n This will enable different option and will use two " + size + "*" + size + " Render Texture buffers for editing. \n"; break;
            }

            text = ntext;

        }

        void OnEnable() {
            SetStage(HintStage.enableTool);
            style.wordWrap = true;
        }

        public GUIStyle style = new GUIStyle();
        PlaytimePainter pp;

        PlaytimePainter ShipPainter() {
            if (!pp)
                pp = pill.GetComponent<PlaytimePainter>();
            return pp;
        }

        private void OnGUI() {
            var cont = new GUIContent(text);
            GUI.Box(new Rect(Screen.width - 400, 10, 390, 100), cont, style);
        }

        void Update()
        {

            timer -= Time.deltaTime;

            switch (stage)
            {
                case HintStage.enableTool:
                    if (PlaytimePainter.IsCurrentTool) { SetStage(HintStage.unlockTexture); timer = 3f; }
                    break;
                case HintStage.unlockTexture:
                    if (cube) {
                        var pntr = cube.GetComponent<PlaytimePainter>();
                        if (pntr && pntr.ImgData != null && !pntr.ImgData.lockEditing)
                            SetStage(HintStage.draw);
                    }
                    break;
                case HintStage.draw:
                    if (!PlaytimePainter.IsCurrentTool) { SetStage(HintStage.enableTool); break; }
                    if (timer < 0) { SetStage(HintStage.addTool); }
                    break;
                case HintStage.addTool:
                    if (picture.GetComponent<PlaytimePainter>() != null) { SetStage(HintStage.addTexture); }
                    break;
                case HintStage.addTexture:
                    if ((ShipPainter() != null) && (ShipPainter().ImgData != null)) SetStage(HintStage.renderTexture); break;
                case HintStage.renderTexture:
                    if ((ShipPainter() != null) && (ShipPainter().ImgData != null) && (ShipPainter().ImgData.TargetIsRenderTexture())) SetStage(HintStage.WellDone); break;
            }

        }

    }

}