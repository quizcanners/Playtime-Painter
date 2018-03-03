using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Playtime_Painter;


[ExecuteInEditMode]
public class HintController : MonoBehaviour {

    //public TextMesh text;

    string text = "";

    hintStage stage;

    enum hintStage {enableTool, draw, addTool, addTexture, renderTexture, WellDone}

    public GameObject picture;
    public GameObject ship;
    public float timer = 5f;

    void setStage(hintStage st) {
        stage = st;
        string ntext = "Well Done! Remember to save your textures before entering/exiting playmode.";
        string mb = (Application.isPlaying) ? "RIGHT MOUSE BUTTON" : "LEFT MOUSE BUTTON";

            switch (stage) {
            case hintStage.enableTool:
                ntext = "Select the cube with " + mb +" and click 'On/Off' to start using painter."; break;
            case hintStage.draw:
                ntext = "Draw on the cube. \n You can LOCK EDITING for selected object."; break;
            case hintStage.addTool:
                ntext = "Picture to the right has no tool attached. \n Select it and \n Click 'Add Component'->'Mesh'->'TextureEditor'"; break;
            case hintStage.addTexture:
                ntext = "Ship on the left has no texture. Select him with " +mb+ " and click 'Create Texture'"; break;
            case hintStage.renderTexture:
                int size = PainterManager.renderTextureSize;
                ntext = "Change MODE to Render Texture. \n This will enable different option and will use two " + size + "*" + size + " Render Texture buffers for editing. \n" +
                    "When using Render Texture to edit different texture2D, \n pixels will be updated at previous one."; break;
            }

        text = ntext;

    }
	// Use this for initialization
	void OnEnable () {
     
       setStage(hintStage.enableTool);
        style.wordWrap = true;
        
    }

    public GUIStyle style;
    PlaytimePainter pp;

    PlaytimePainter shipPainter() {
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

		case hintStage.enableTool:  if (PlaytimeToolComponent.enabledTool == typeof(PlaytimePainter)) {  setStage(hintStage.draw); timer = 3f; } break;
		case hintStage.draw: if (PlaytimeToolComponent.enabledTool != typeof(PlaytimePainter)) { setStage(hintStage.enableTool); break; } if (timer < 0) { setStage(hintStage.addTool); } break;
               case hintStage.addTool: if (picture.GetComponent<PlaytimePainter>() != null) { setStage(hintStage.addTexture); } break;
                 case hintStage.addTexture:
                if ((shipPainter()!= null) && (shipPainter().curImgData != null)) setStage(hintStage.renderTexture); break;
                 case hintStage.renderTexture: if ((shipPainter() != null) && (shipPainter().curImgData != null)
                    && (shipPainter().curImgData.TargetIsRenderTexture())) setStage(hintStage.WellDone); break;
                       
                }

        }

    }

