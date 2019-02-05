using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public abstract class CoordinatePickerBase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler {

        static CoordinatePickerBase currentPicker;
        
        [NonSerialized] public bool mouseDown = false;

        bool Down { get { return mouseDown; }
            set {

                if (value) {
                    if (currentPicker && currentPicker != this)
                        currentPicker.mouseDown = false;

                    currentPicker = this;
                    mouseDown = true;
                }
                
                mouseDown = value;
      
            }
        }

        [NonSerialized] Camera clickCamera;
        public RectTransform rectTransform;

        public Vector2 uvClick;
        public abstract bool UpdateFromUV(Vector2 clickUV);
        
        public void OnPointerDown(PointerEventData eventData) => Down = DataUpdate(eventData);

        public void OnPointerUp(PointerEventData eventData) => Down = false;

        public void OnPointerEnter(PointerEventData eventData) {  }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Down)
                DataUpdate(eventData);
        }
        
        bool DataUpdate(PointerEventData eventData)
        {

            if (DataUpdate(eventData.position, eventData.pressEventCamera))
                clickCamera = eventData.pressEventCamera;
            else return false;

            return true;
        }

        bool DataUpdate(Vector2 position, Camera cam)
        {
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, cam, out localCursor))
                return false;

            pegi.MouseOverUI = true;

            uvClick = (localCursor / rectTransform.rect.size) + Vector2.one * 0.5f;

            return UpdateFromUV(uvClick);
        }

        protected virtual void Update() {
            if (!Input.GetMouseButton(0) || (Down && !DataUpdate(Input.mousePosition, clickCamera)))
                Down = false;
        }

        protected virtual void OnEnable() {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
            mouseDown = false;
        }

 
    }
}
