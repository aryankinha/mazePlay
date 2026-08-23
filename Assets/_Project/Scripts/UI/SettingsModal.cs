using System;
using ArrowMaze.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class SettingsModal : MonoBehaviour
    {
        private static readonly Color ColorActive = new Color32(47, 128, 237, 255);
        private static readonly Color ColorInactive = new Color32(208, 215, 227, 255);
        private static readonly Color ColorTextNavy = new Color32(23, 35, 61, 255);

        [Header("UI Bindings")]
        [SerializeField] private GameObject modalCard;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button soundEffectsToggleButton;
        [SerializeField] private TMP_Text soundEffectsToggleText;
        [SerializeField] private Image soundEffectsToggleImage;
        [SerializeField] private RectTransform soundEffectsToggleKnob;
        [SerializeField] private Button hapticsToggleButton;
        [SerializeField] private TMP_Text hapticsToggleText;
        [SerializeField] private Image hapticsToggleImage;
        [SerializeField] private RectTransform hapticsToggleKnob;

        [SerializeField] private Button resetProgressButton;
        [SerializeField] private GameObject resetConfirmDialog;
        [SerializeField] private Button resetConfirmCancelButton;
        [SerializeField] private Button resetConfirmOkButton;

        public event Action OnProgressReset;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (soundEffectsToggleButton != null)
            {
                soundEffectsToggleButton.onClick.AddListener(ToggleSoundEffects);
            }

            if (hapticsToggleButton != null)
            {
                hapticsToggleButton.onClick.AddListener(ToggleHaptics);
            }

            if (resetProgressButton != null)
            {
                resetProgressButton.onClick.AddListener(() =>
                {
                    if (resetConfirmDialog != null)
                    {
                        resetConfirmDialog.SetActive(true);
                    }
                });
            }

            if (resetConfirmCancelButton != null)
            {
                resetConfirmCancelButton.onClick.AddListener(() =>
                {
                    if (resetConfirmDialog != null)
                    {
                        resetConfirmDialog.SetActive(false);
                    }
                });
            }

            if (resetConfirmOkButton != null)
            {
                resetConfirmOkButton.onClick.AddListener(ConfirmReset);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (resetConfirmDialog != null)
            {
                resetConfirmDialog.SetActive(false);
            }
            RefreshToggles();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ToggleHaptics()
        {
            PlayerProgress.HapticsEnabled = !PlayerProgress.HapticsEnabled;
            RefreshToggles();
        }

        private void ToggleSoundEffects()
        {
            PlayerProgress.SoundEffectsEnabled = !PlayerProgress.SoundEffectsEnabled;
            RefreshToggles();
        }

        private void ConfirmReset()
        {
            PlayerProgress.ResetAllProgress();
            if (resetConfirmDialog != null)
            {
                resetConfirmDialog.SetActive(false);
            }
            OnProgressReset?.Invoke();
            RefreshToggles();
            Hide();
        }

        private void RefreshToggles()
        {
            UpdateToggleUI(soundEffectsToggleImage, soundEffectsToggleText, soundEffectsToggleKnob, PlayerProgress.SoundEffectsEnabled);
            UpdateToggleUI(hapticsToggleImage, hapticsToggleText, hapticsToggleKnob, PlayerProgress.HapticsEnabled);
        }

        private static void UpdateToggleUI(Image img, TMP_Text text, RectTransform knob, bool enabled)
        {
            if (img != null)
            {
                img.color = enabled ? ColorActive : ColorInactive;
            }

            if (text != null)
            {
                text.text = enabled ? "ON" : "OFF";
                text.color = enabled ? Color.white : ColorTextNavy;
                text.rectTransform.anchoredPosition = enabled ? new Vector2(-27f, 0f) : new Vector2(27f, 0f);
            }

            if (knob != null)
            {
                knob.anchoredPosition = enabled ? new Vector2(44f, 0f) : new Vector2(-44f, 0f);
            }
        }
    }
}
