using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class BinaryTree_simple<T>   where T : new() {




    BinaryTree_Branch root;

    public T Get(int ind) {
        if (root == null)
            return default(T);

        return root._Get(ind);
    }


    public T GetOrInit(int ind) {

        if (root == null) {
            root = new BinaryTree_Branch(ind);
            return root.data;
        }

        return root._GetOrInit(ind);
    }


    private class BinaryTree_Branch {

        public T data;
        protected int myindex;

        public BinaryTree_Branch left;
        public BinaryTree_Branch right;

        public BinaryTree_Branch(int index)
        {
            myindex = index;
            data = new T();
        }

        public T _Get(int ind)
        {
            BinaryTree_Branch current = this;

            while (current.myindex != ind) {
                current = ind > current.myindex ? current.right : current.left;
                if (current == null)
                    return default(T);
            }

            return current.data;
        }

        public T _GetOrInit(int ind) {
            BinaryTree_Branch current = this;

            while (current.myindex != ind)
            {
                BinaryTree_Branch next = ind > current.myindex ? current.right : current.left;

                if (next == null){
                    next = new BinaryTree_Branch(ind);
                    if (ind > current.myindex)
                        current.right = next;
                    else current.left = next;
                    return next.data;
                }

                current = next;
            }

            return current.data;

        }

    }

}
