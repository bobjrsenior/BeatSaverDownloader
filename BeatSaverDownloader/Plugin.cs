﻿using BeatSaverDownloader.Misc;
using BeatSaverDownloader.UI;
using BS_Utils.Gameplay;
using IPA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;

namespace BeatSaverDownloader
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        public static BeatSaverSharp.BeatSaver BeatSaver;
        public static System.Net.Security.RemoteCertificateValidationCallback certificateCallback;
        [Init]
        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            log = logger;
        }

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = certificateCallback;
        }
        [OnStart]
        public void OnApplicationStart()
        {
            certificateCallback = System.Net.ServicePointManager.ServerCertificateValidationCallback;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) => {
                if (System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors == sslPolicyErrors ||
                    System.Net.Security.SslPolicyErrors.None == sslPolicyErrors)
                    return true;
                return false;                
            };

            string steamDllPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "Beat Saber_Data", "Plugins", "steam_api64.dll");
            bool hasSteamDll = File.Exists(steamDllPath);
            string platform = hasSteamDll ? "steam" : "oculus";
            string gameVersionFull = $"{IPA.Utilities.UnityGame.GameVersion.ToString()}-{platform}";

            var httpOptions = new BeatSaverSharp.HttpOptions()
            {
                ApplicationName = "BeatSaverDownloader",
                Version = Assembly.GetExecutingAssembly().GetName().Version,

                Agents = new BeatSaverSharp.ApplicationAgent[1]
                {
                    new BeatSaverSharp.ApplicationAgent("BeatSaber", gameVersionFull),
                },
            };

            BeatSaver = new BeatSaverSharp.BeatSaver(httpOptions);

            instance = this;
            PluginConfig.LoadConfig();
            Sprites.ConvertToSprites();

            PluginUI.instance.Setup();

            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
        {
            try
            {
                PluginUI.SetupLevelDetailClone();
                Settings.SetupSettings();
                SongCore.Loader.SongsLoadedEvent += Loader_SongsLoadedEvent;
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        private void Loader_SongsLoadedEvent(SongCore.Loader arg1, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            if (!PluginUI.instance.moreSongsButton.Interactable)
                PluginUI.instance.moreSongsButton.Interactable = true;
        }

        public void OnUpdate()
        {
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
