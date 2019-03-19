﻿using Antura.Profile;
using System;
using System.Collections.Generic;

namespace Antura.Core
{
    /// <summary>
    /// Defines app settings that must be saved locally.
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        public AppLanguages AppLanguage = AppLanguages.English;

        // not used anymore.. but could be useful in the future
        public bool HighQualityGfx = false;

        // the uuid of currently active player
        public string LastActivePlayerUUID;

        // to enable english subtitles (especially in the Book)
        public bool EnglishSubtitles = true;

        // to enable arabic subtitles / Keeper Widget that shows what he's saying
        public bool SubtitlesEnabled = false;

        public bool MusicEnabled = true;

        public bool ShareAnalyticsEnabled = true;

        // if set the app starts is special scene mode, used in museums and demo installations
        public bool KioskMode = false;

        // we save the current AppVersion maybe we should compare when installing updated versions
        public string AppVersion;

        // the list of saved players
        public List<PlayerIconData> SavedPlayers = new List<PlayerIconData>();

        public void SetAppVersion(Version _version)
        {
            AppVersion = _version.ToString();
        }

        public void DeletePlayers()
        {
            SavedPlayers = new List<PlayerIconData>();
            LastActivePlayerUUID = "";
        }
    }
}