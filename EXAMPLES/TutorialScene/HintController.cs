using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Playtime_Painter;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


[ExecuteInEditMode]
public class HintController : MonoBehaviour {

    //public TextMesh text;

    string text = "";

    HintStage stage;

    enum HintStage {enableTool, draw, addTool, addTexture, renderTexture, WellDone}

    public GameObject picture;
    public GameObject ship;
    public float timer = 5f;

    void SetStage(HintStage st) {
        stage = st;
        string ntext = "Well Done! Remember to save your textures before entering/exiting playmode.";
        string mb = (Application.isPlaying) ? "RIGHT MOUSE BUTTON" : "LEFT MOUSE BUTTON";

            switch (stage) {
            case HintStage.enableTool:
                ntext = "Select the cube with " + mb +" and click 'On/Off' to start using painter."; break;
            case HintStage.draw:
                ntext = "Draw on the cube. \n You can LOCK EDITING for selected object."; break;
            case HintStage.addTool:
                ntext = "Picture to the right has no tool attached. \n Select it and \n Click 'Add Component'->'Mesh'->'TextureEditor'"; break;
            case HintStage.addTexture:
                ntext = "Ship on the left has no texture. Select him with " +mb+ " and click 'Create Texture'"; break;
            case HintStage.renderTexture:
                int size = PainterCamera.renderTextureSize;
                ntext = "Change MODE to Render Texture. \n This will enable different option and will use two " + size + "*" + size + " Render Texture buffers for editing. \n" +
                    "When using Render Texture to edit different texture2D, \n pixels will be updated at previous one."; break;
            }

        text = ntext;

    }
	// Use this for initialization
	void OnEnable () {
     
       SetStage(HintStage.enableTool);
        style.wordWrap = true;
        
    }

    public GUIStyle style;
    PlaytimePainter pp;

    PlaytimePainter ShipPainter() {
        if (pp == null)
         pp = ship.GetComponent<PlaytimePainter>();
        return pp;
    }


    private void OnGUI()
    {
        var cont = new GUIContent(text);
      
        GUI.Box(new Rect(Screen.width - 400, 10, 390, 100), cont, style);
    }

    // Update is called once per frame
    void Update() {
        timer -= Time.deltaTime;



        switch (stage) {

		case HintStage.enableTool:  if (PlaytimeToolComponent.enabledTool == typeof(PlaytimePainter)) {  SetStage(HintStage.draw); timer = 3f; } break;
		case HintStage.draw: if (PlaytimeToolComponent.enabledTool != typeof(PlaytimePainter)) { SetStage(HintStage.enableTool); break; } if (timer < 0) { SetStage(HintStage.addTool); } break;
               case HintStage.addTool: if (picture.GetComponent<PlaytimePainter>() != null) { SetStage(HintStage.addTexture); } break;
                 case HintStage.addTexture:

                if ((ShipPainter()!= null) && (ShipPainter().ImgData != null)) SetStage(HintStage.renderTexture); break;

                 case HintStage.renderTexture: if ((ShipPainter() != null) && (ShipPainter().ImgData != null)
                    && (ShipPainter().ImgData.TargetIsRenderTexture())) SetStage(HintStage.WellDone); break;
                       
                }

        }

    }

