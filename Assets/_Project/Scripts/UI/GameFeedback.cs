using ArrowMaze.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArrowMaze.UI
{
    /// <summary>
    /// One lightweight, persistent SFX source for the small set of high-value game events.
    /// It also installs consistent press feedback on scene buttons after each scene load.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameFeedback : MonoBehaviour
    {
        private const string ResourceRoot = "Audio/Feedback/";

        private static GameFeedback instance;

        private AudioSource source;
        private AudioClip buttonClip;
        private AudioClip carMoveClip;
        private AudioClip blockedClip;
        private AudioClip successClip;
        private AudioClip exitClip;
        private AudioClip toggleClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureInstance();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
        {
            EnsureInstance().InstallButtonFeedback(scene);
        }

        private static GameFeedback EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            var root = new GameObject("Game Feedback");
            instance = root.AddComponent<GameFeedback>();
            DontDestroyOnLoad(root);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 0.72f;
            source.ignoreListenerPause = true;

            // Resources are loaded once for the full session, not on each interaction.
            buttonClip = Resources.Load<AudioClip>(ResourceRoot + "click_002");
            carMoveClip = Resources.Load<AudioClip>(ResourceRoot + "select_003");
            blockedClip = Resources.Load<AudioClip>(ResourceRoot + "error_005");
            successClip = Resources.Load<AudioClip>(ResourceRoot + "confirmation_002");
            exitClip = Resources.Load<AudioClip>(ResourceRoot + "back_001");
            toggleClip = Resources.Load<AudioClip>(ResourceRoot + "toggle_001");
        }

        public static void PlayButton() => Play(instance != null ? instance.buttonClip : null, 0.58f);
        public static void PlayToggle() => Play(instance != null ? instance.toggleClip : null, 0.55f);
        public static void PlayCarMove() => Play(instance != null ? instance.carMoveClip : null, 0.52f);
        public static void PlayBlocked() => Play(instance != null ? instance.blockedClip : null, 0.72f);
        public static void PlayExit() => Play(instance != null ? instance.exitClip : null, 0.48f);
        public static void PlaySuccess() => Play(instance != null ? instance.successClip : null, 0.72f);

        private static void Play(AudioClip clip, float volume)
        {
            var active = instance != null ? instance : EnsureInstance();
            if (!PlayerProgress.SoundEffectsEnabled || clip == null || active.source == null)
            {
                return;
            }

            active.source.PlayOneShot(clip, volume);
        }

        private void InstallButtonFeedback(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var feedback = button.GetComponent<ButtonPressFeedback>();
                    if (feedback == null)
                    {
                        feedback = button.gameObject.AddComponent<ButtonPressFeedback>();
                    }

                    feedback.SetToggleStyle(button.name.Contains("Toggle"));
                }
            }
        }
    }
}
