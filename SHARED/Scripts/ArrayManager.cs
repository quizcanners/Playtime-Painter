using UnityEngine;
using System;

namespace SharedTools_Stuff {
    public static class Array_Extensions {

        public static T[] GetCopy<T>(this T[] args) {
            T[] temp = new T[args.Length];
            args.CopyTo(temp, 0);
            return temp;
        }

        public static void Resize<T>(ref T[] args, int To)
        {
            T[] temp;
            temp = new T[To];
            if (args != null)
                Array.Copy(args, 0, temp, 0, Mathf.Min(To, args.Length));
            else
                args = temp;
        }

        public static void Expand<T>(ref T[] args, int add)
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
        }

        public static void Remove<T>(ref T[] args, int ind)
        {
            T[] temp = new T[args.Length - 1];
            Array.Copy(args, 0, temp, 0, ind);
            int count = args.Length - ind - 1;
            Array.Copy(args, ind + 1, temp, ind, count);
            args = temp;
        }

        public static void AddAndInit<T>(ref T[] args, int add) where T:new()
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            for (int i = args.Length - add; i < args.Length; i++)
                args[i] = new T();
        }

        public static T AddAndInit<T>(ref T[] args) where T : new()
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + 1];
                args.CopyTo(temp, 0);
            }
            else temp = new T[1];
            args = temp;
            T tmp = new T();
            args[temp.Length - 1] = tmp;
            return tmp;
        }

        public static void InsertAfterAndInit<T>(ref T[] args, int ind) where T : new()
        {
            if ((args != null) && (args.Length > 0))
            {
                T[] temp = new T[args.Length + 1];
                Array.Copy(args, 0, temp, 0, ind + 1);
                if (ind < args.Length - 1)
                {
                    int count = args.Length - ind - 1;
                    Array.Copy(args, ind + 1, temp, ind + 2, count);
                }
                args = temp;
                args[ind + 1] = new T();
            } else {

                args = new T[ind + 1];
                for (int i = 0; i < ind + 1; i++)
                    args[i] = new T();
            }


        }
    }
}