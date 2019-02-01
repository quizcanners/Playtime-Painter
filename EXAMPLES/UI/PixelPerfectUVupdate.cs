using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System;

namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class PixelPerfectUVupdate : MonoBehaviour, IPEGI {

        public Camera mainCamera;

        public enum UpdateMode { PerFrame, EditorOnly, Disabled, OnEnable_Play }

        public UpdateMode updateMode = UpdateMode.PerFrame;

        public RectTransform rectTransform;

        public Material material;

        private void OnEnable()
        {
            SearchDependencies();

            if (updateMode == UpdateMode.OnEnable_Play && Application.isPlaying)
                UpdateProjectionPosition();

        }

        void SearchDependencies(bool refresh = false)
        {
            if (refresh || !rectTransform)
                rectTransform = GetComponent<RectTransform>();

            if (refresh || !material)
            {
                var graphic = GetComponent<MaskableGraphic>();
                if (graphic)
                    material = graphic.material;
            }
        }

        void Update()
        {
            switch (updateMode)
            {
                case UpdateMode.EditorOnly: if (!Application.isPlaying) UpdateProjectionPosition(); break;
                case UpdateMode.PerFrame: UpdateProjectionPosition(); break;
            }

        }

        void UpdateProjectionPosition()
        {
            if (rectTransform && material)
            {
                var pos = RectTransformUtility.WorldToScreenPoint(Application.isPlaying ? mainCamera : null, rectTransform.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
                
                Vector2 scale = rectTransform.rect.size;
                scale = new Vector2(Mathf.Max(0, (scale.x - scale.y) / scale.x), Mathf.Max(0, (scale.y - scale.x) / scale.y));

                material.SetVector("_ProjTexPos", pos.ToVector4(scale));//rectTransform.rect.size));
            }
        }

        #region Inspector
        #if PEGI

        [NonSerialized]bool showDependencies = false;
        public bool Inspect()
        {
            bool changed = false;
            "Update Mode".editEnum(80, ref updateMode).changes(ref changed);
            if (icon.Refresh.Click("Update Now").nl())
                UpdateProjectionPosition();

            if ("Dependencies".foldout(ref showDependencies) && icon.Search.Click("Try find on Component"))
                SearchDependencies(true);

                pegi.nl();

            if (showDependencies || !material)
                "Material".edit(60, ref material).nl(ref changed);

            if (showDependencies || !rectTransform)
                "Rect Trancform".edit(90, ref rectTransform).nl(ref changed);

            if (showDependencies || !mainCamera)
            {
                "* Camera".edit(60, ref mainCamera).nl(ref changed);

                "* If there is more then one camera or no mainCamera then camera needs to be set in order for position provision to work".writeHint();
            }

            return changed;
        }
        #endif
        #endregion

    }
}