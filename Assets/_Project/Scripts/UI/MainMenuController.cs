using ArrowMaze.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowMaze.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            var canvas = MenuUiBuilder.CreateCanvas("Main Menu Canvas");
            MenuUiBuilder.Panel(canvas, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(250, 252, 255, 255));
            MenuUiBuilder.Text(canvas, "Title", "TAP AWAY\nCARS", new Vector2(.5f, .78f), new Vector2(.5f, .78f), Vector2.zero, new Vector2(860, 250), 105f, MenuUiBuilder.Navy);
            MenuUiBuilder.Text(canvas, "Subtitle", "Your traffic journey", new Vector2(.5f, .66f), new Vector2(.5f, .66f), Vector2.zero, new Vector2(640, 60), 34f, new Color32(92, 112, 144, 255));

            var continueButton = MenuUiBuilder.Button(canvas, "Continue Button", "CONTINUE", new Vector2(.5f, .5f), new Vector2(0, 50), new Vector2(650, 130), MenuUiBuilder.Blue);
            continueButton.onClick.AddListener(() => { LevelSession.SelectedLevel = PlayerProgress.GetContinueLevel(); SceneManager.LoadScene("Gameplay"); });
            var mapButton = MenuUiBuilder.Button(canvas, "Level Map Button", "LEVEL MAP", new Vector2(.5f, .5f), new Vector2(0, -120), new Vector2(650, 115), MenuUiBuilder.Navy);
            mapButton.onClick.AddListener(() => SceneManager.LoadScene("LevelMap"));
            var settingsButton = MenuUiBuilder.Button(canvas, "Settings Button", "SETTINGS", new Vector2(.5f, .5f), new Vector2(0, -270), new Vector2(420, 92), new Color32(129, 151, 184, 255));
            var settingsPanel = MenuUiBuilder.Panel(canvas, "Settings Panel", new Vector2(.13f, .32f), new Vector2(.87f, .67f), Vector2.zero, Vector2.zero, MenuUiBuilder.Navy);
            MenuUiBuilder.Text(settingsPanel.transform, "Settings Title", "SETTINGS", new Vector2(.5f, .72f), new Vector2(.5f, .72f), Vector2.zero, new Vector2(600, 70), 46f, Color.white);
            MenuUiBuilder.Text(settingsPanel.transform, "Settings Message", "Sound and accessibility controls\nwill appear in the next polish pass.", new Vector2(.5f, .48f), new Vector2(.5f, .48f), Vector2.zero, new Vector2(620, 110), 27f, MenuUiBuilder.PaleBlue);
            var closeSettings = MenuUiBuilder.Button(settingsPanel.transform, "Close Settings", "CLOSE", new Vector2(.5f, .20f), Vector2.zero, new Vector2(260, 72), MenuUiBuilder.Blue);
            closeSettings.onClick.AddListener(() => settingsPanel.gameObject.SetActive(false));
            settingsPanel.gameObject.SetActive(false);
            settingsButton.onClick.AddListener(() => settingsPanel.gameObject.SetActive(true));
            MenuUiBuilder.Text(canvas, "Progress", $"ROAD PROGRESS  •  LEVEL {PlayerProgress.GetContinueLevel()}", new Vector2(.5f, .32f), new Vector2(.5f, .32f), Vector2.zero, new Vector2(800, 55), 28f, MenuUiBuilder.Green);
        }
    }
}
