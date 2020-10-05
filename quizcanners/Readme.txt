This are my Utilities for making Custom Playtime & Editor Inspector, also classes for manual serialization of Configs. 

Latest Version is always at:
https://github.com/quizcanners/Tools
(Part of the Playtime Painter)

There is currently no documentation but the Painter asset as well as 
https://github.com/quizcanners/NodeBook
and 
https://github.com/quizcanners/GravityRunner

can serve as examples since they use this utilities a lot.

HOW TO USE IT IN PLAYTIME:
   In MonoBehaviour that needs to have playtime interface create the following code.

	pegi.GameView.Window window = new pegi.GameView.Window();
	
	public void OnGUI() =>	window.Render(this, "TITLE");
	
