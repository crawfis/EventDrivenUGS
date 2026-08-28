using System;
using System.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;

using CrawfisSoftware.Config;
using CrawfisSoftware.UGS.Events;

using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.RemoteConfig
{
    class DifficultyObserver : IDisposable
    {
        ServiceObserver<IAuthenticationService> m_AuthenticationServiceObserver;

        IAuthenticationService m_AuthenticationService;

        static DifficultyObserver s_Instance;

        /// <summary>
        /// The runtime difficulty data
        /// </summary>
        public List<DifficultyConfig> RuntimeDifficultySettings { get; private set; }

        /// <summary>
        /// Accessor for the singleton instance
        /// </summary>
        public static DifficultyObserver Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new DifficultyObserver();
                }

                return s_Instance;
            }
        }

        DifficultyObserver()
        {
            RuntimeDifficultySettings = new List<DifficultyConfig>();

            m_AuthenticationServiceObserver = ServiceObserverHelpers.InitializeServiceObserver<IAuthenticationService>(OnAuthenticationServiceInitialized);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// This exists to fix an issue of entering play mode when the UI Builder is open.
        /// </summary>
        /// <param name="state"></param>
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                s_Instance.Dispose();
                s_Instance = null;
            }
        }

#endif

        void OnAuthenticationServiceInitialized(IAuthenticationService authenticationService)
        {
            m_AuthenticationService = authenticationService;
            ServiceObserverHelpers.CleanupServiceObserver(ref m_AuthenticationServiceObserver);

            if (IsSignedIn())
            {
                OnSignedIn();
            }
            else
            {
                Action signedIn = null;
                signedIn = () =>
                {
                    OnSignedIn();
                    m_AuthenticationService.SignedIn -= signedIn;
                };
                m_AuthenticationService.SignedIn += signedIn;
            }
        }

        bool IsSignedIn()
        {
            return RemoteConfigService.Instance != null
                && m_AuthenticationService != null
                && m_AuthenticationService.IsSignedIn;
        }

        void OnSignedIn()
        {
            if (IsSignedIn())
            {
                _ = GetDifficultySettingsAsync();
            }
        }

        async Task<List<DifficultyConfig>> GetDefinitions()
        {
            var configs = await RemoteConfigService.Instance.FetchConfigsAsync(new EmptyStruct(), new EmptyStruct());
            var difficultiesJobject = configs.config[RemoteConfigConstants.difficultySettingsKey];
            //return difficultiesJobject.ToObject<List<DifficultyConfig>>();
            if (difficultiesJobject == null)
            {
                return new List<DifficultyConfig>();
            }
            var difficulties = difficultiesJobject.ToObject<List<DifficultyConfig>>();
            return difficulties ?? new List<DifficultyConfig>();
        }

        async Task GetDifficultySettingsAsync()
        {
            //var difficulties = await GetAchievementsAsync(m_AuthenticationService.PlayerId);
            var difficulties = await GetDefinitions();
            RuntimeDifficultySettings = difficulties;
            UGSBus.Publish(UGS_EventsEnum.DifficultySettingsFetched, this, difficulties);

        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ServiceObserverHelpers.CleanupServiceObserver(ref m_AuthenticationServiceObserver);
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }
        struct EmptyStruct { }
    }
}