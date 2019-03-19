using Antura.Audio;
using System;
using UnityEngine;

namespace Antura.Core
{
    public class AppSettingsManager
    {
        private const string SETTINGS_PREFS_KEY = "OPTIONS";
        private AppSettings _settings = new AppSettings();

        public AppSettings Settings
        {
            get { return _settings; }
            set {
                if (value != _settings) {
                    _settings = value;
                    // Auto save at any change
                    SaveSettings();
                } else {
                    _settings = value;
                }
            }
        }

        private bool isFirstIstall;
        public Version AppVersionPrevious;

        /// <summary>
        /// Loads the settings. Creates new settings if none are found.
        /// </summary>
        public AppSettings LoadSettings()
        {
            if (PlayerPrefs.HasKey(SETTINGS_PREFS_KEY)) {
                var serializedObjs = PlayerPrefs.GetString(SETTINGS_PREFS_KEY);
                Settings = JsonUtility.FromJson<AppSettings>(serializedObjs);
                //Debug.Log("LoadSettings() " + serializedObjs);
            } else {
                // FIRST INSTALLATION
                isFirstIstall = true;
                Debug.Log("LoadSettings() FIRST INSTALLATION");
                Settings = new AppSettings();
                Settings.SetAppVersion(AppConfig.AppVersion);
            }

            AudioManager.I.MusicEnabled = Settings.MusicEnabled;
            // Debug.Log("Setting music to " + Settings.MusicOn);
            return _settings;
        }

        /// <summary>
        /// Save all settings. This also saves player profiles.
        /// </summary>
        public void SaveSettings()
        {
            var serializedObjs = JsonUtility.ToJson(Settings);
            PlayerPrefs.SetString(SETTINGS_PREFS_KEY, serializedObjs);
            PlayerPrefs.Save();
            // Debug.Log("AppSettingsManager SaveSettings() " + serializedObjs);
        }

        /// <summary>
        /// Delete all settings. This also deletes all player profiles.
        /// </summary>
        public void DeleteAllSettings()
        {
            PlayerPrefs.DeleteAll();
        }

        #region external API to save single settings
        public void SaveMusicSetting(bool musicOn)
        {
            Settings.MusicEnabled = musicOn;
            SaveSettings();
        }
        #endregion

        public bool IsAppJustUpdatedFromOldVersion()
        {
            if (!isFirstIstall && AppVersionPrevious != null && AppVersionPrevious <= new Version(1, 0, 0, 0)) {
                return true;
            } else {
                return false;
            }
        }

        public void UpdateAppVersion()
        {
            if (Settings.AppVersion != null && Settings.AppVersion == "") {
                AppVersionPrevious = new Version(0, 0, 0, 0);
            } else {
                AppVersionPrevious = new Version(Settings.AppVersion);
            }
            Debug.Log("UpdateAppVersion() previous: " + AppVersionPrevious + " current: " + AppConfig.AppVersion);
            Settings.SetAppVersion(AppConfig.AppVersion);
            SaveSettings();
        }

        public Action<bool> onEnableShareAnalytics;

        public void EnableShareAnalytics(bool status)
        {
            Settings.ShareAnalyticsEnabled = status;
            SaveSettings();
            if (onEnableShareAnalytics != null) onEnableShareAnalytics(status);
        }

        public void ToggleShareAnalytics()
        {
            EnableShareAnalytics(!Settings.ShareAnalyticsEnabled);
        }

        public void SetKioskMode(bool status)
        {
            Settings.KioskMode = status;
            SaveSettings();
        }

        public void DeleteAllPlayers()
        {
            Settings.DeletePlayers();
            SaveSettings();
        }

        public void ToggleQualitygfx()
        {
            Settings.HighQualityGfx = !Settings.HighQualityGfx;
            SaveSettings();
            // CameraGameplayController.I.EnableFX(Settings.HighQualityGfx);
        }

        public void ToggleEnglishSubtitles()
        {
            Settings.EnglishSubtitles = !Settings.EnglishSubtitles;
            SaveSettings();
        }

        public void ToggleSubtitles()
        {
            Settings.SubtitlesEnabled = !Settings.SubtitlesEnabled;
            SaveSettings();
        }

    }
}