using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    /// <summary>Consistent tactile response for every native uGUI button.</summary>
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler, ISubmitHandler
    {
        private const float PressScale = 0.96f;
        private const float PressDuration = 0.055f;
        private const float ReleaseDuration = 0.09f;

        private Button button;
        private Vector3 restingScale;
        private Coroutine scaleRoutine;
        private bool useToggleSound;

        private void Awake()
        {
            button = GetComponent<Button>();
            restingScale = transform.localScale;
        }

        private void OnDisable()
        {
            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
                scaleRoutine = null;
            }

            transform.localScale = restingScale;
        }

        public void SetToggleStyle(bool enabled)
        {
            useToggleSound = enabled;
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (button != null && button.interactable)
            {
                AnimateTo(restingScale * PressScale, PressDuration);
            }
        }

        public void OnPointerUp(PointerEventData _)
        {
            AnimateTo(restingScale, ReleaseDuration);
        }

        public void OnPointerExit(PointerEventData _)
        {
            AnimateTo(restingScale, ReleaseDuration);
        }

        public void OnPointerClick(PointerEventData _)
        {
            PlaySound();
        }

        public void OnSubmit(BaseEventData _)
        {
            PlaySound();
            AnimateTo(restingScale, ReleaseDuration);
        }

        private void PlaySound()
        {
            if (button == null || !button.interactable)
            {
                return;
            }

            if (useToggleSound)
            {
                GameFeedback.PlayToggle();
            }
            else
            {
                GameFeedback.PlayButton();
            }
        }

        private void AnimateTo(Vector3 target, float duration)
        {
            if (!isActiveAndEnabled)
            {
                transform.localScale = target;
                return;
            }

            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
            }

            scaleRoutine = StartCoroutine(ScaleRoutine(target, duration));
        }

        private IEnumerator ScaleRoutine(Vector3 target, float duration)
        {
            var start = transform.localScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.LerpUnclamped(start, target, progress);
                yield return null;
            }

            transform.localScale = target;
            scaleRoutine = null;
        }
    }
}
