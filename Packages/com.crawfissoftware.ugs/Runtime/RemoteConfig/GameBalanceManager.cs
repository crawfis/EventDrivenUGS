using System;

using Unity.Services.RemoteConfig;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    public class GameBalanceManager
    {
        private GameBalance _gameBalance;
        private bool _logValues;

        public event Action<GameBalance> OnGameBalanceUpdated;
        public GameBalance GameBalance => _gameBalance;

        public void Initialize(bool logValues = true)
        {
            _logValues = logValues;
            _gameBalance = CreateDefaultGameBalance();
        }

        public void UpdateFromRemoteConfig()
        {
            if (RemoteConfigService.Instance.appConfig.HasKey("game_balance"))
            {
                string balance = RemoteConfigService.Instance.appConfig.GetJson("game_balance");
                JsonUtility.FromJsonOverwrite(balance, _gameBalance);
                OnGameBalanceUpdated?.Invoke(_gameBalance);
                
                if (_logValues)
                    Debug.Log("Remote game balance settings loaded successfully");
            }
        }

        private GameBalance CreateDefaultGameBalance()
        {
            return new GameBalance
            {
                GlobalSpeedMultiplier = 1.0f,
                GlobalScoreMultiplier = 1.0f,
                CoinsPerRun = 10,
                CoinsPerVideoAd = 50,
                LivesRefillTimeMinutes = 30,
                MaxLives = 5
            };
        }

        public void LogValues()
        {
            Debug.Log($"Game Balance - Speed Multiplier: {_gameBalance.GlobalSpeedMultiplier}, Score Multiplier: {_gameBalance.GlobalScoreMultiplier}");
        }
    }
}