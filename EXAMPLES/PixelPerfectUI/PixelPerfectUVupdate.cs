using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;


namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class PixelPerfectUVupdate : MonoBehaviour
    {

        public RectTransform rect;

        public Canvas canvas;

        public Material mat;

        private void OnEnable()
        {
            if (!rect)
                rect = GetComponent<RectTransform>();
        }

        [ExecuteInEditMode]
        // Update is called once per frame
        void Update()
        {

            if (rect && canvas && mat)
            {

                var pos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));

                mat.SetVector("_ProjTexPos", pos.ToVector4());
            }



        }
    }
}