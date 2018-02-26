using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif




// This class will be serialized and saved when you enter or exit play mode
[Serializable]
public class prsst  {
    public int test = 0;

     public void AfterLoad() {
        test += 1;
       // Debug.Log(test);
    }

     public void BeforeSave() {
        //make sure all important data is moved inside this class;
    }

}

[ExecuteInEditMode]
public class Persistant : MonoBehaviour {
    const string fileName = "prsst";

    static prsst _dta;
    static Persistant monoHolder;

    public static prsst dta()  {

        if (monoHolder == null)
            monoHolder = GameObject.FindObjectOfType<Persistant>();

        if (_dta == null)
            monoHolder.Load();

        return _dta;
    }

    void Awake () {
       
        Load();
#if UNITY_EDITOR
       // EditorApplication.playModeStateChanged -= Save;
       // EditorApplication.playModeStateChanged += Save;
#endif
    }

    private void Update() {
        if (_dta == null)
            Load();
    }

   /* public void Save(PlayModeStateChange state) {
        if (_dta != null) {
            _dta.BeforeSave();
            ResourceSaver.SaveToResources(fileName, _dta);
        }
    }*/

    void Load() {
        ResourceLoader<prsst> rl = new ResourceLoader<prsst>();
        if (!rl.LoadResource(fileName, ref _dta))
            _dta = new prsst();
        _dta.AfterLoad();
    }
}
