
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonPressEffect :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Animation")]
        [SerializeField, Range(0.8f, 1f)]
        private float pressedScale = 0.94f;

        [SerializeField, Min(0.01f)]
        private float animationDuration = 0.10f;

        private RectTransform rectTransform;
        private Button button;
        private Vector3 normalScale;
        private Coroutine animationRoutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            button = GetComponent<Button>();

            normalScale = rectTransform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.IsInteractable())
                return;

            AnimateTo(normalScale * pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(normalScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(normalScale);
        }

        private void AnimateTo(Vector3 targetScale)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(
                ScaleRoutine(targetScale)
            );
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale)
        {
            Vector3 startScale =
                rectTransform.localScale;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / animationDuration
                );

                float eased =
                    t * t * (3f - 2f * t);

                rectTransform.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        targetScale,
                        eased
                    );

                yield return null;
            }

            rectTransform.localScale = targetScale;
            animationRoutine = null;
        }

        private void OnDisable()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            if (rectTransform != null)
                rectTransform.localScale = normalScale;
        }
    }
}
