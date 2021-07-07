My Utilities to make CustomEditor super-fast. This are just wrappers on top of Unity's Custom Editor functionality. Absolutely nothing special or new.

Latest Version is always at:
https://github.com/quizcanners/Playtime-Painter
(Part of the Playtime Painter)

Use IPEGI interface to make Inspect() fucntion (Look at some examples).


HOW TO USE IN EDITOR TIME:

	   Anywhere (I usually put it in the same .cs file, right under the class):
	  [PEGI_Inspector_Override(typeof(YourClass))] internal class YourClassInspector : PEGI_Inspector_Override { }


HOW TO USE IT IN PLAYTIME:

    In MonoBehaviour that that you want a playtime interface for, create the following code:
	pegi.GameView.Window window = new pegi.GameView.Window();
	public void OnGUI() =>	window.Render(this, "TITLE");



	
