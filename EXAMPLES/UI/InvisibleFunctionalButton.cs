using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using PlayerAndEditorGUI;

namespace QuizCannersUtilities
{



    public class InvisibleFunctionalButton : Graphic,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPEGI
    {

        public UnityEvent OnClick;

        public enum ClickableElementTransition { Fade, Nothing }

        public ClickableElementTransition transition;

        public Graphic clickVisualFeedTarget;

        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        [NonSerialized] bool mouseDown = false;
        [NonSerialized] float mouseDownTime = 0;
        [NonSerialized] Vector2 mouseDownPosition;

        public float maxHoldForClick = 0.3f;
        public float maxMousePositionPixOffsetForClick = 20f;

        public void OnPointerDown(PointerEventData eventData)
        {
            mouseDownPosition = Input.mousePosition;
            mouseDown = true;
            mouseDownTime = Time.time;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {

        }

        public void OnPointerExit(PointerEventData eventData)
        {

            //   if (mouseDown)
            //    Debug.LogError("Mouse Exited");

            mouseDown = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {

            if (mouseDown)
            {

                if (Time.time - mouseDownTime < maxHoldForClick)
                {

                    var diff = mouseDownPosition - Input.mousePosition.ToVector2();

                    if ((diff.magnitude) < maxMousePositionPixOffsetForClick)
                        OnClick.Invoke();
                   // else Debug.Log("Too much displacement: {0} -> {1} : {2}:{3} > {4}".F(mouseDownPosition, eventData.position
                     //   , diff, diff.magnitude, maxMousePositionPixOffsetForClick));
                }
               // else Debug.LogError("Click to slow");
            }

            mouseDown = false;
        }

        bool targetDirty;

        void LateUpdate()
        {

            if (clickVisualFeedTarget && (targetDirty || mouseDown))
            {

                if ((Time.time - mouseDownTime) > maxHoldForClick
                   || ((Input.mousePosition.ToVector2() - mouseDownPosition).magnitude > maxMousePositionPixOffsetForClick))
                    mouseDown = false;

                float a = clickVisualFeedTarget.color.a;

                float target = mouseDown ? 0.75f : 1;

                a = MyMath.Lerp_bySpeed(a, target, 5);

                clickVisualFeedTarget.TrySetAlpha(a);

                targetDirty = (a != 1);
            }

        }

#if PEGI

        public bool Inspect()
        {
            var changed = false;

            if (mouseDown)
                icon.Active.write("Is pressed");

            "Max Click Duration".edit("How much time can pass between Mouse Button Down and Up events", 90, ref maxHoldForClick).nl(ref changed);
            "Max Position Change (pix)".edit("How much mouse can move between Down and Up", 110, ref maxMousePositionPixOffsetForClick).nl(ref changed);

            "On Click".edit_Property(() => OnClick, this).nl(ref changed);

            "Target".edit(50, ref clickVisualFeedTarget).nl(ref changed);

            if (clickVisualFeedTarget)
                "On Mouse Down".editEnum(60, ref transition).nl(ref changed);

            if (mouseDownPosition.magnitude > 0)
                "Last mouse down position: {0}".F(mouseDownPosition).nl();

            return changed;
        }
#endif

    }
}