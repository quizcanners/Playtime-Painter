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
1. Move the Resources Folder outside of Editor Flder (Needs to be quizcanners->Resources->icons instead of quizcanners->Editor->Resources->icons);
	To include icons in in a build.
2. In MonoBehaviour that needs to have playtime interface create the following code.

   private pegi.WindowPositionData_PEGI_GUI playtimeWindow = new pegi.WindowPositionData_PEGI_GUI();
   public void OnGUI() => playtimeWindow.Render(this);

3. All set.