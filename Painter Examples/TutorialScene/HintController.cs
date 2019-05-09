using System;
using UnityEngine;

namespace PlaytimePainter.Examples
{

    [ExecuteInEditMode]
    public class HintController : MonoBehaviour
    {
        private string _text = "";

        private HintStage _stage;

        enum HintStage { EnableTool, UnlockTexture, Draw, AddTool, AddTexture, RenderTexture, WellDone }

        public GameObject picture;
        public GameObject pill;
        public GameObject cube;
        public float timer = 5f;

        private void SetStage(HintStage st)  {
            _stage = st;
           
            var mb = (Application.isPlaying) ? "RIGHT MOUSE BUTTON" : "LEFT MOUSE BUTTON";

            string newText;

            switch (_stage) {
                case HintStage.EnableTool:
                    newText = "Select the central cube with " + mb + " and click 'On/Off' to start using painter. Unlock texture if locked."; break;
                case HintStage.UnlockTexture:
                    newText = "Unlock texture (lock icon next to it)"; break;
                case HintStage.Draw:
                    newText = "Draw on the cube. \n You can LOCK EDITING for selected texture."; break;
                case HintStage.AddTool:
                    newText = "Picture to the right has no tool attached. \n Select it and \n Click 'Add Component'->'Mesh'->'Playtime Painter'"; break;
                case HintStage.AddTexture:
                    newText = "Pill on the left has no texture. Select it with " + mb + " and click 'Create Texture' icon"; break;
                case HintStage.RenderTexture:
                    int size = RenderTextureBuffersManager.renderBuffersSize;
                    newText = "Change MODE to GPU Blit. \n This will enable different option and will use two " + size + "*" + size + " Render Texture buffers for editing. \n"; break;
                case HintStage.WellDone: goto default;
                default:
                    newText = "Well Done! Remember to save your textures before entering/exiting Play Mode.";
                    break;
            }

            _text = newText;

        }

        private void OnEnable() {
            SetStage(HintStage.EnableTool);
            style.wordWrap = true;
        }

        public GUIStyle style = new GUIStyle();

        PlaytimePainter pp;

        private PlaytimePainter PillPainter {
            get
            {
                if (!pp)
                    pp = pill.GetComponent<PlaytimePainter>();

                return pp;
            }
        }

        private void OnGUI() {
            var cont = new GUIContent(_text);
            GUI.Box(new Rect(Screen.width - 400, 10, 390, 100), cont, style);
        }

        private void Update()
        {

            timer -= Time.deltaTime;

            switch (_stage)
            {
                case HintStage.EnableTool:
                    if (PlaytimePainter.IsCurrentTool) { SetStage(HintStage.UnlockTexture); timer = 3f; }
                    break;
                case HintStage.UnlockTexture:
                    if (cube) {
                        var painter = cube.GetComponent<PlaytimePainter>();
                        if (painter && painter.ImgMeta != null && !painter.ImgMeta.lockEditing)
                            SetStage(HintStage.Draw);
                    }
                    break;
                case HintStage.Draw:
                    if (!PlaytimePainter.IsCurrentTool) { SetStage(HintStage.EnableTool); break; }
                    if (timer < 0) { SetStage(HintStage.AddTool); }
                    break;
                case HintStage.AddTool:
                    if (picture.GetComponent<PlaytimePainter>()) { SetStage(HintStage.AddTexture); }
                    break;
                case HintStage.AddTexture:
                    if (PillPainter && PillPainter.ImgMeta != null) SetStage(HintStage.RenderTexture); break;
                case HintStage.RenderTexture:
                    if (PillPainter && PillPainter.ImgMeta != null && PillPainter.ImgMeta.TargetIsRenderTexture()) SetStage(HintStage.WellDone); break;
            }

        }

    }

}