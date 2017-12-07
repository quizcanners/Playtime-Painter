using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


namespace StoryTriggerData {

    public static class PoolExtensions  {
        public static List<T> ToListOfStoryPoolablesOfTag<T>(this string data, string prefabTag) where T : STD_Object
        {

            stdDecoder cody = new stdDecoder(data);

            List<T> l = new List<T>();

            while (cody.gotData)
            {
                cody.getTag();
                T tmp = (T)STD_Pool.getOne(prefabTag);
                tmp.Reboot(cody.getData());
                l.Add(tmp);
            }

            return l;
        }


    }

    public class TagName : Attribute {

		string tag;

		public string Tag
		{
			get
			{
				return tag;
			}
		}

		public TagName( string tagname)
		{
			tag = tagname;
		}
	}

    public abstract class STD_Pool {

        public static STD_Pool[] all;

        public static IEnumerable<STD_Object> allEnabledObjects() {
            for (int i = 0; i < all.Length; i++) {
                
                PoolControllerBase pcb = all[i].pool;

                for (int o = 0; o < pcb.Max; o++)
                    if (pcb.activeSelf(o)) 
                        yield return (STD_Object)pcb.getScript(o);
            }
        }



        public static Dictionary<string, STD_Pool> stdPoolsDictionary;

        public static void DestroyAll() {
            foreach (STD_Pool cmp in all)
                cmp.pool.DestroyAll();
        }

        public static STD_Object getOne(string tag) {
            STD_Pool cp;
            if (stdPoolsDictionary.TryGetValue(tag, out cp))
                return (STD_Object)cp.pool.getScript();
            return null;
        }

        static int counter = 0;

        public int poolIndex;
        public string storyTag;
        public PoolControllerBase pool;

        public static void InitStoryPoolsIfNull() {
            if (all == null) {
                List<Type> derrs = CsharpFuncs.GetAllChildTypesOf<STD_Object>();
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

        public void PEGI() {
            pool.PEGI();
        }

        public STD_Pool() {
            poolIndex = counter;
            counter++;
        }
    }

	public class STD_PoolGeneric<T> : STD_Pool where T: STD_Object, new() {

		public string getCodeTag() {
			var dnAttribute = typeof(T).GetCustomAttributes(
				typeof(TagName), true
			).FirstOrDefault() as TagName;
			if (dnAttribute != null) 
				return dnAttribute.Tag;
			
			return null;
		}

		public STD_PoolGeneric() {
            storyTag = getCodeTag();
            pool = new PoolController<T> (8, Resources.Load(Book.PrefabsResourceFolder + "/" + storyTag) as GameObject);
            try {
                pool.prefab.GetComponent<STD_Object>().SetStaticPoolController(this);
            } catch {

                Debug.Log( "Place a prefab for your "+ typeof(T) + " object and place it in some Resource folder, in "
                          +Book.PrefabsResourceFolder+" subfolder and call it "+storyTag);

            }
		}
	}
}
