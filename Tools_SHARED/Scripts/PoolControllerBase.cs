using UnityEngine;
using System.Collections;
using System;
using PlayerAndEditorGUI;



public class PoolableBase : MonoBehaviour {
    public PoolControllerBase poolController;
    public int poolIndex;

    public virtual void Reboot(PoolControllerBase deactivator, int my_ind) {
        poolController = deactivator;
        poolIndex = my_ind;
    }

	public virtual void PEGI (){
	
	}

    public void OnDestroy() {
       
        if (poolController!= null)
            poolController.OnDuringDestroy(poolIndex);

    }


    public virtual void Deactivate() {
        poolController.Deactivate(poolIndex);

    }



}

public abstract class PoolControllerBase {
	public int Max;
	public int firstFree;
	protected int ExpandBy;
	protected int _BufferSize;
	public GameObject prefab;
    public int browsedObject = -1; // For Component Browsing

	protected abstract void ExpandArrays ();
	protected abstract void init (int bufferSize);
	public abstract void DeactivateAll ();
	public abstract void DestroyAll();
    public abstract void OnDuringDestroy(int ind);
	public abstract void Deactivate (int i);
	public abstract void PEGI ();
	public abstract bool activeSelf (int i);
	public abstract GameObject getFreeGO ();
    public abstract void AddToPool(GameObject go);
	public abstract Component getScript();
    public abstract Component getScript(int i);

	public PoolControllerBase(int lim, GameObject pref) {
		prefab = pref;
		init(lim);
	}

}

public class PoolController<T> : PoolControllerBase where T : PoolableBase,  new() {
    public T[] scripts;


    public ArrayManager<T> ScrptArray = new ArrayManager<T>();


    public override void PEGI() {
        
        if (browsedObject != -1) {
            T tmp = scripts[browsedObject];
            if (((!tmp.gameObject.activeSelf)) || (pegi.Click("< Objects")))
                browsedObject = -1;
            else
                tmp.PEGI();
        }

        pegi.newLine();

        if (browsedObject == -1)
        for (int i = 0; i < Max; i++) {
            T tmp = scripts[i];
            if (tmp.gameObject.activeSelf) {
                    
                string name = tmp.gameObject.name;
                if (pegi.edit(ref name))
                    tmp.gameObject.name = name;

                if (pegi.Click(">"))
                    browsedObject = i;
                pegi.newLine();
            }
        }
    }

	public override bool activeSelf (int i) {
		return scripts [i].gameObject.activeSelf;
	}

	protected override void ExpandArrays() {
        ScrptArray.Expand(ref scripts, ExpandBy);
        _BufferSize += ExpandBy;
    }

 

    private T GetOrInstantiate()    {
        if (scripts[firstFree] == null)  {
                scripts[firstFree] = GameObject.Instantiate(prefab).GetComponent<T>();
            
            scripts[firstFree].Reboot(this, firstFree);
        }

        T tmp = scripts[firstFree];
       
        tmp.gameObject.SetActive(true);
        tmp.transform.parent = null;
        firstFree++;
        Max = Mathf.Max(firstFree, Max);

        return tmp;
    }

    public override void AddToPool(GameObject go) {
        
        int before = firstFree;
        while ((firstFree < Max) && (firstFree < _BufferSize)) firstFree++;
        if (firstFree >= _BufferSize) { ExpandArrays(); }

      
        scripts[firstFree] = go.GetComponent<T>();

        T tmp = scripts[firstFree];

        tmp.poolController = this;
        tmp.poolIndex = firstFree;


        tmp.gameObject.SetActive(true);
        tmp.transform.parent = null;
        firstFree++;
        Max = Mathf.Max(firstFree, Max);

        firstFree = before; // Because we skipped the inactive ones
    }

  

    public T getOne()  {
        while ((firstFree < Max) && (scripts[firstFree].gameObject.activeSelf) && (firstFree < _BufferSize)) firstFree++;
        if (firstFree >= _BufferSize) { ExpandArrays(); }

        return GetOrInstantiate();
    }

    public T getOne(Transform parent)
    {
        T tmp = getOne();
        tmp.transform.parent = parent;
        return tmp;
    }

    public override Component getScript(int i) {
        return scripts[i];
    }

	public override Component getScript () {
		return getOne();
	}

	public override GameObject getFreeGO () {
		return getOne ().gameObject;
	}

    protected override void init(int bufferSize) {
        Max = 0;
        firstFree = 0;
        _BufferSize = bufferSize;
        ExpandBy = bufferSize;
        scripts = new T[bufferSize];
    }

	public PoolController(int lim, GameObject pref) : base (lim, pref) {
    }

    public override void DeactivateAll() {
        for (int i = 0; i < Max; i++){
            scripts[i].gameObject.SetActive(false);
        }
		firstFree = 0;
        Max = 0;

       
    }

    public override void OnDuringDestroy(int ind) {
        //Debug.Log("Destroying "+ind+" Max: "+Max);
        for (int i = ind; i < Max-1; i++) {
            scripts[i] = scripts[i + 1];
            scripts[i].poolIndex--;
        }

        Max--;
        scripts[Max] = null;
        firstFree = 0;
    }

    public override void DestroyAll() {
        for (int i = Max - 1; i >= 0; i--) {
            scripts[i].gameObject.DestroyWhatever();
        }
	}

    public override void Deactivate(int i) {
        scripts[i].gameObject.SetActive(false);
        firstFree = Mathf.Min(i, firstFree);
        while ((Max > 0) && (!scripts[Max - 1].gameObject.activeSelf))
            Max--;
    }


}
