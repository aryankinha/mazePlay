using ArrowMaze.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    public sealed class LevelNode : MonoBehaviour
    {
        private static readonly Color ColorCompleted = new Color32(255, 255, 255, 255);
        private static readonly Color ColorCurrent = new Color32(47, 128, 237, 255);
        private static readonly Color ColorLocked = new Color32(226, 232, 240, 255);

        private static readonly Color TextNavy = new Color32(23, 35, 61, 255);
        private static readonly Color TextWhite = Color.white;
        private static readonly Color TextLocked = new Color32(148, 163, 184, 255);

        private static readonly Color StarGold = new Color32(242, 201, 78, 255);
        private static readonly Color StarEmpty = new Color32(208, 215, 227, 255);

        [Header("Components")]
        [SerializeField] private Button button;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Outline nodeOutline;
        [SerializeField] private TMP_Text levelNumberText;
        [SerializeField] private GameObject carMarker;
        [SerializeField] private GameObject currentGlow;
        [SerializeField] private GameObject starsContainer;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFullSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private GameObject currentBadge;
        [SerializeField] private GameObject lockIcon;

        public void Setup(int id, int currentUnlockedLevel)
        {
            var levelId = id;

            var unlocked = PlayerProgress.IsUnlocked(levelId);
            var isCurrent = levelId == currentUnlockedLevel;
            var stars = PlayerProgress.GetStars(levelId);
            var completed = stars > 0;

            if (levelNumberText != null)
            {
                levelNumberText.text = $"{levelId}";
            }

            if (isCurrent)
            {
                // Current level state
                if (nodeBackground != null) nodeBackground.color = ColorCurrent;
                if (nodeOutline != null) nodeOutline.effectColor = Color.white;
                if (levelNumberText != null) levelNumberText.color = TextWhite;
                if (carMarker != null) carMarker.SetActive(true);
                if (currentGlow != null) currentGlow.SetActive(true);
                if (currentBadge != null) currentBadge.SetActive(true);
                if (lockIcon != null) lockIcon.SetActive(false);
                if (starsContainer != null) starsContainer.SetActive(false);
                if (button != null) button.interactable = true;
            }
            else if (completed || unlocked)
            {
                // Completed / Unlocked level state
                if (nodeBackground != null) nodeBackground.color = ColorCompleted;
                if (nodeOutline != null) nodeOutline.effectColor = StarGold;
                if (levelNumberText != null) levelNumberText.color = TextNavy;
                if (carMarker != null) carMarker.SetActive(false);
                if (currentGlow != null) currentGlow.SetActive(false);
                if (currentBadge != null) currentBadge.SetActive(false);
                if (lockIcon != null) lockIcon.SetActive(false);
                if (button != null) button.interactable = true;

                if (starsContainer != null)
                {
                    starsContainer.SetActive(true);
                    if (starImages != null && starImages.Length >= 3)
                    {
                        starImages[0].sprite = stars >= 1 ? starFullSprite : starEmptySprite;
                        starImages[1].sprite = stars >= 2 ? starFullSprite : starEmptySprite;
                        starImages[2].sprite = stars >= 3 ? starFullSprite : starEmptySprite;
                    }
                }
            }
            else
            {
                // Locked level state
                if (nodeBackground != null) nodeBackground.color = ColorLocked;
                if (nodeOutline != null) nodeOutline.effectColor = Color.white;
                if (levelNumberText != null) levelNumberText.color = TextLocked;
                if (carMarker != null) carMarker.SetActive(false);
                if (currentGlow != null) currentGlow.SetActive(false);
                if (currentBadge != null) currentBadge.SetActive(false);
                if (lockIcon != null) lockIcon.SetActive(true);
                if (starsContainer != null) starsContainer.SetActive(false);
                if (button != null) button.interactable = false;
            }
        }

    }
}
