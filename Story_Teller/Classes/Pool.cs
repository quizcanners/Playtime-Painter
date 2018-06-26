using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace StoryTriggerData {

    public static class PoolExtensions  {
        public static List<T> ToListOfStoryPoolablesOfTag<T>(this string data, string prefabTag) where T : STD_Poolable
        {

            StdDecoder cody = new StdDecoder(data);

            List<T> l = new List<T>();

            while (cody.GotData)
            {
                cody.GetTag();
                T tmp = (T)STD_Pool.getOne(prefabTag);
                l.Add(cody.GetData().DecodeInto(tmp));
            }

            return l;
        }


    }

    public class StoryTagName : Attribute {

		string tag;

		public string Tag
		{
			get
			{
				return tag;
			}
		}

		public StoryTagName( string tagname)
		{
			tag = tagname;
		}
	}

    public abstract class STD_Pool {

        public static bool DestroyingAll = false;
        public static STD_Pool[] all;

        public static IEnumerable<STD_Poolable> allEnabledObjects() {
            for (int i = 0; i < all.Length; i++) {
                
                PoolControllerBase pcb = all[i].pool;

                for (int o = 0; o < pcb.initializedCount; o++)
                    if (pcb.activeSelf(o)) 
                        yield return (STD_Poolable)pcb.getScript(o);
            }
        }



        public static Dictionary<string, STD_Pool> stdPoolsDictionary;

        public static void DestroyAll() {
            DestroyingAll = true;
            foreach (STD_Pool cmp in all)
                cmp.pool.DestroyAll();
            DestroyingAll = false;
        }

        public static STD_Poolable getOne(string tag) {
            STD_Pool cp;
            if (stdPoolsDictionary.TryGetValue(tag, out cp))
                return (STD_Poolable)cp.pool.getScript();
            return null;
        }

        static int counter = 0;

        public int poolIndex;
        public string storyTag;
        public PoolControllerBase pool;

        public static void InitStoryPoolsIfNull() {
            if (all == null) {
                List<Type> derrs = CsharpFuncs.GetAllChildTypesOf<STD_Poolable>();
                all = new STD_Pool[derrs.Count];
                stdPoolsDictionary = new Dictionary<string, STD_Pool>();

                for (int i = 0; i < derrs.Count; i++) {
                    Type genericClass = typeof(STD_PoolGeneric<>);
                    Type constructedClass = genericClass.MakeGenericType(derrs[i]);
                    STD_Pool cp = (STD_Pool)Activator.CreateInstance(constructedClass);
                    all[i] = cp;
                    stdPoolsDictionary.Add(cp.storyTag, cp);
                }
            }
        }
        #if PEGI
        public void PEGI() {
            pool.PEGI();
        }
#endif
        public STD_Pool() {
            poolIndex = counter;
            counter++;
        }
    }

	public class STD_PoolGeneric<T> : STD_Pool where T: STD_Poolable, new() {

		public string getCodeTag() {
			var dnAttribute = typeof(T).GetCustomAttributes(
				typeof(StoryTagName), true
			).FirstOrDefault() as StoryTagName;
			if (dnAttribute != null) 
				return dnAttribute.Tag;
			
			return null;
		}

		public STD_PoolGeneric() {
            storyTag = getCodeTag();
            pool = new PoolController<T> (8, Resources.Load(Book.PrefabsResourceFolder + "/" + storyTag) as GameObject);
            try {
                pool.prefab.GetComponent<STD_Poolable>().SetStaticPoolController(this);
            } catch {

                Debug.Log( "Place a prefab for your "+ typeof(T) + " object and place it in some Resource folder, in "
                          +Book.PrefabsResourceFolder+" subfolder and call it "+storyTag);

            }
		}
	}
}
