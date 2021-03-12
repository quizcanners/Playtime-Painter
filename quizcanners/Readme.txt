This are my Utilities for making Custom Playtime & Editor Inspector, also classes for manual serialization of Configs. 

Latest Version is always at:
https://github.com/quizcanners/Playtime-Painter
(Part of the Playtime Painter)

HOW TO USE IT IN PLAYTIME:
   In MonoBehaviour that needs to have playtime interface create the following code:

	pegi.GameView.Window window = new pegi.GameView.Window();
	public void OnGUI() =>	window.Render(this, "TITLE");
	
