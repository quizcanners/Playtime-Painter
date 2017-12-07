using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FamilyMember<T> where T : Family<T> {
    public int index;
    public bool _is(Family<T> type) { return type == Family<T>.family[index]; }
    public Type get() {
        return Family<T>.family[index].GetType();
    }

public string[] names { get { return Family<T>.names; } }
}

[Serializable]
public class Family<T> where T : Family<T> {

    public static T[] family;

    public static string[] names;

    public T[] all { get { return family; } }

    public string[] allNames { get { return names; } }

    public int index;

    public override int GetHashCode() {
        return index;
    }


    public static implicit operator int (Family<T> instance) {
        if (instance == null) {
            return -1;
        }
        return instance.index;
    }

    static Family ()  {
        List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<T>();

        family = new T[allTypes.Count];
        names = new string[allTypes.Count];

      //  Debug.Log("Creating "+typeof(T).ToString());

        for (int i = 0; i < allTypes.Count; i++)  {
         //   Debug.Log("Adding " + allTypes[i].ToString());
            T tmp = (T)Activator.CreateInstance(allTypes[i]);
            family[i] = tmp;
            tmp.index = i;
            names[i] = tmp.ToString();
        }
    }

}

/*
public class newToolBase : Family<newToolBase> {

}

public class eraser : newToolBase {

}

public class pencil : newToolBase {

}


public abstract class secondToolBase : Family<secondToolBase>
{

}

public class dragger : secondToolBase
{

}

public class putter : secondToolBase {

    public override string ToString()
    {
        return "putter";
    }

}*/