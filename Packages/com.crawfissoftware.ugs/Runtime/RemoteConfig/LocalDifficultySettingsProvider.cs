using System;
using UnityEngine;

//namespace CrawfisSoftware.GameConfig
//{
//    [CreateAssetMenu(fileName = "LocalDifficultyProvider", menuName = "CrawfisSoftware/GameConfig/LocalDifficultyProvider")]
//    public class LocalDifficultySettingsProvider : ScriptableObject, IDifficultySettingsProvider
//    {
//        [SerializeField] private DifficultySettings _difficultySettings;

//        public bool IsLoaded => _difficultySettings != null;
//        public event Action<DifficultySettings> OnDifficultySettingsUpdated;

//        public DifficultyConfig GetDifficultyConfig(DifficultyLevel level)
//        {
//            if (_difficultySettings == null)
//            {
//                return CreateFallbackConfig(level);
//            }
            
//            return _difficultySettings.GetDifficulty(level);
//        }

//        private DifficultyConfig CreateFallbackConfig(DifficultyLevel level)
//        {
//            return level switch
//            {
//                DifficultyLevel.Easy => new DifficultyConfig { InitialSpeed = 3f, MaxSpeed = 6f, Acceleration = 0.15f, StartRunway = 15, NumberOfLives = 5 },
//                DifficultyLevel.Medium => new DifficultyConfig { InitialSpeed = 5f, MaxSpeed = 8f, Acceleration = 0.2f, StartRunway = 10, NumberOfLives = 3 },
//                DifficultyLevel.Hard => new DifficultyConfig { InitialSpeed = 6f, MaxSpeed = 10f, Acceleration = 0.25f, StartRunway = 8, NumberOfLives = 2 },
//                DifficultyLevel.Insane => new DifficultyConfig { InitialSpeed = 8f, MaxSpeed = 15f, Acceleration = 0.35f, StartRunway = 5, NumberOfLives = 1 },
//                DifficultyLevel.Exergame => new DifficultyConfig { InitialSpeed = 4f, MaxSpeed = 7f, Acceleration = 0.18f, StartRunway = 12, NumberOfLives = 10 },
//                _ => new DifficultyConfig()
//            };
//        }

//        public void SetDifficultySettings(DifficultySettings settings)
//        {
//            _difficultySettings = settings;
//            OnDifficultySettingsUpdated?.Invoke(_difficultySettings);
//        }
//    }
//}