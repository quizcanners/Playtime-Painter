﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;
using UnityEngine.UI;

namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class PixelPerfectUVupdate : MonoBehaviour
    {
        public Camera mainCamera = null;

        public RectTransform rect;

        public Material mat;

        private void OnEnable() {
            if (!rect)
                rect = GetComponent<RectTransform>();

            if (!mat) {
                var graphic = GetComponent<MaskableGraphic>();
                if (graphic)
                    mat = graphic.material;
            }

        }

        void LateUpdate() {

            if (rect && mat) {
                var pos = RectTransformUtility.WorldToScreenPoint(mainCamera, rect.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
                mat.SetVector("_ProjTexPos", pos.ToVector4(rect.rect.size));
            }
        }
    }
}