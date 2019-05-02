using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    public class InvisibleButton : Graphic, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPEGI {

        public UnityEvent OnClick;

        public enum ClickableElementTransition { Fade, Nothing }

        public ClickableElementTransition transition;

        public Graphic clickVisualFeedTarget;

        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        [NonSerialized] private bool _mouseDown;
        [NonSerialized] private float _mouseDownTime;
        [NonSerialized] private Vector2 _mouseDownPosition;

        public float maxHoldForClick = 0.3f;
        public float maxMousePositionPixOffsetForClick = 20f;

        public void OnPointerDown(PointerEventData eventData)
        {
            _mouseDownPosition = Input.mousePosition;
            _mouseDown = true;
            _mouseDownTime = Time.time;
        }

        public void OnPointerEnter(PointerEventData eventData) { }

        public void OnPointerExit(PointerEventData eventData) =>_mouseDown = false;
        

        public void OnPointerUp(PointerEventData eventData)
        {

            if (_mouseDown && Time.time - _mouseDownTime < maxHoldForClick)
            {

                var diff = _mouseDownPosition - Input.mousePosition.ToVector2();

                if ((diff.magnitude) < maxMousePositionPixOffsetForClick)
                    OnClick.Invoke();
            }
            
            _mouseDown = false;
        }

        private bool _targetDirty;

        private void LateUpdate()
        {
            if (!clickVisualFeedTarget || (!_targetDirty && !_mouseDown)) return;

            if ((Time.time - _mouseDownTime) > maxHoldForClick || ((Input.mousePosition.ToVector2() - _mouseDownPosition).magnitude > maxMousePositionPixOffsetForClick))
                _mouseDown = false;

            if (transition == ClickableElementTransition.Fade) {
                var a = clickVisualFeedTarget.color.a;
                
                var target = _mouseDown ? 0.75f : 1;

                a = QcMath.LerpBySpeed(a, target, 5);

                clickVisualFeedTarget.TrySetAlpha(a);

                _targetDirty = (Math.Abs(a - 1) > float.Epsilon);
            }

        }

#if PEGI

        public bool Inspect()
        {
            var changed = false;

            if (_mouseDown)
                icon.Active.write("Is pressed");

            "Max Click Duration".edit("How much time can pass between Mouse Button Down and Up events", 90, ref maxHoldForClick).nl(ref changed);
            "Max Position Change (pix)".edit("How much mouse can move between Down and Up", 110, ref maxMousePositionPixOffsetForClick).nl(ref changed);

            "On Click".edit_Property(() => OnClick, this).nl(ref changed);

            "Target".edit(50, ref clickVisualFeedTarget).nl(ref changed);

            if (clickVisualFeedTarget)
                "On Mouse Down".editEnum(60, ref transition).nl(ref changed);

            if (_mouseDownPosition.magnitude > 0)
                "Last mouse down position: {0}".F(_mouseDownPosition).nl();

            return changed;
        }
#endif

    }
}