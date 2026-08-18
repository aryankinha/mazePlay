using System;
using UnityEngine;

namespace ArrowMaze.Gameplay
{
    public sealed class LivesManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxLives = 3;

        public event Action<int> OnLivesChanged;
        public event Action OnGameOver;

        public int CurrentLives { get; private set; }
        public int MaxLives => maxLives;

        private void Awake()
        {
            ResetLives();
        }

        public void ResetLives()
        {
            CurrentLives = maxLives;
            OnLivesChanged?.Invoke(CurrentLives);
        }

        public void LoseLife()
        {
            if (CurrentLives <= 0)
            {
                return;
            }

            CurrentLives--;
            OnLivesChanged?.Invoke(CurrentLives);

            if (CurrentLives == 0)
            {
                OnGameOver?.Invoke();
            }
        }
    }
}
