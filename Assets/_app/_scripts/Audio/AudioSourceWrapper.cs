﻿using Antura.Minigames;
using DG.DeAudio;
using UnityEngine;

namespace Antura.Audio
{
    public class AudioSourceWrapper : IAudioSource
    {
        public DeAudioGroup Group { get; private set; }
        public DeAudioSource CurrentSource;

        AudioClip clip;
        AudioManager manager;

        bool paused = false;

        public bool IsPlaying
        {
            get
            {
                if (CurrentSource == null || CurrentSource.audioSource == null) {
                    return false;
                }

                return CurrentSource.isPlaying;
            }
        }

        bool loop;

        public bool Loop
        {
            get { return loop; }

            set
            {
                loop = value;

                if (CurrentSource != null) {
                    CurrentSource.loop = value;
                }
            }
        }

        float pitch;

        public float Pitch
        {
            get { return pitch; }

            set
            {
                pitch = value;

                if (CurrentSource != null) {
                    CurrentSource.pitch = value;
                }
            }
        }

        float volume;

        public float Volume
        {
            get { return volume; }

            set
            {
                volume = value;

                if (CurrentSource != null) {
                    CurrentSource.volume = value;
                }
            }
        }

        float duration;

        public float Duration
        {
            get { return duration; }
        }

        public float Position
        {
            get
            {
                if (CurrentSource == null || CurrentSource.audioSource == null) {
                    return 0;
                }

                return CurrentSource.time;
            }
            set
            {
                if (CurrentSource != null) {
                    CurrentSource.time = value;
                }
            }
        }

        public void Stop()
        {
            if (CurrentSource != null && CurrentSource.audioSource != null) {
                CurrentSource.loop = false;
                CurrentSource.Stop();
            }

            CurrentSource = null;
            paused = false;
        }

        public void Play()
        {
            if (paused && CurrentSource != null) {
                CurrentSource.Resume();
            } else {
                paused = false;

                if (CurrentSource != null) {
                    CurrentSource.Stop();
                }

                CurrentSource = Group.Play(clip);
                CurrentSource.locked = true;

                CurrentSource.pitch = pitch;
                CurrentSource.volume = volume;
                CurrentSource.loop = loop;
                manager.OnAudioStarted(this);
            }
        }

        public void Pause()
        {
            if (CurrentSource != null && CurrentSource.Pause()) {
                paused = true;
            }
        }

        public bool Update()
        {
            if (CurrentSource != null) {
                if (!CurrentSource.isPlaying && !CurrentSource.isPaused && CurrentSource.time == 0 && !manager.IsAppPaused) {
                    CurrentSource.Stop();
                    CurrentSource.locked = false;
                    CurrentSource = null;
                    return true;
                }
            }
            return false;
        }

        public AudioSourceWrapper(DeAudioSource source, DeAudioGroup group, AudioManager manager)
        {
            this.CurrentSource = source;
            this.Group = group;
            this.manager = manager;
            this.clip = source.clip;
            duration = source.duration;
            loop = source.loop;
            volume = source.volume;
            pitch = source.pitch;

            manager.OnAudioStarted(this);
        }
    }
}