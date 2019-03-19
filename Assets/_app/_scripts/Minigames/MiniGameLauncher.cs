using Antura.Core;
using Antura.Database;
using Antura.Helpers;
using Antura.LivingLetters;
using Antura.Teacher;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Antura.Minigames
{
    /// <summary>
    /// Handles the logic to launch minigames with the correct configuration.
    /// </summary>
    public class MiniGameLauncher
    {
        private QuestionPacksGenerator questionPacksGenerator;
        private TeacherAI teacher;

        // Last used data
        private IGameConfiguration currentGameConfig;
        private IQuestionBuilder currentQuestionBuilder;
        private List<IQuestionPack> currentQuestionPacks;

        public IGameConfiguration GetCurrentMiniGameConfig()
        {
            return currentGameConfig;
        }

        public MiniGameLauncher(TeacherAI _teacher)
        {
            teacher = _teacher;
            questionPacksGenerator = new QuestionPacksGenerator();
        }

        /// <summary>
        /// Prepare the context and start a minigame.
        /// </summary>
        /// <param name="_gameCode">The minigame code.</param>
        /// <param name="_launchConfiguration">The launch configuration. If null, the Teacher will generate a new one.</param>
        /// <param name="forceNewPlaySession">Is this a new play session?</param>
        public void LaunchGame(MiniGameCode _gameCode, MinigameLaunchConfiguration _launchConfiguration = null,
            bool forceNewPlaySession = false)
        {
            ConfigAI.StartTeacherReport();
            if (_launchConfiguration == null) {
                var difficulty = teacher.GetCurrentDifficulty(_gameCode);
                var numberOfRounds = teacher.GetCurrentNumberOfRounds(_gameCode);
                var tutorialEnabled = teacher.GetTutorialEnabled(_gameCode);
                _launchConfiguration = new MinigameLaunchConfiguration(difficulty, numberOfRounds, tutorialEnabled);
            }

            var miniGameData = AppManager.I.DB.GetMiniGameDataByCode(_gameCode);

            if (forceNewPlaySession) {
                AppManager.I.NavigationManager.InitNewPlaySession(miniGameData);
            }

            if (AppConfig.DebugLogEnabled) { Debug.Log("StartGame " + _gameCode.ToString()); }

            // Assign the configuration for the given minigame
            var minigameSession = System.DateTime.Now.Ticks.ToString();
            currentGameConfig = ConfigureMiniGameScene(_gameCode, minigameSession);
            currentGameConfig.Difficulty = _launchConfiguration.Difficulty;
            currentGameConfig.TutorialEnabled = _launchConfiguration.TutorialEnabled;

            // Set also the number of rounds
            // @note: only for assessment, for now
            if (currentGameConfig is Assessment.IAssessmentConfiguration) {
                var assessmentConfig = currentGameConfig as Assessment.IAssessmentConfiguration;
                assessmentConfig.NumberOfRounds = _launchConfiguration.NumberOfRounds;
            }

            // Retrieve the packs for the current minigame configuration
            currentQuestionBuilder = currentGameConfig.SetupBuilder();
            currentQuestionPacks = questionPacksGenerator.GenerateQuestionPacks(currentQuestionBuilder);
            currentGameConfig.Questions = new SequentialQuestionPackProvider(currentQuestionPacks);

            // Communicate to LogManager the start of a new single minigame play session.
            if (AppConfig.DebugLogDbInserts) { Debug.Log("InitGameplayLogSession " + _gameCode.ToString()); }
            LogManager.I.LogInfo(InfoEvent.GameStart, "{\"minigame\":\"" + _gameCode.ToString() + "\"}");
            LogManager.I.StartMiniGame();

            // Print the teacher's report now
            ConfigAI.PrintTeacherReport();

            // Play the title dialog for the game
            //AudioManager.I.PlayDialogue(_gameCode.ToString()+"_Title");

            // Launch the game
            AppManager.I.NavigationManager.GoToMiniGameScene();
        }

        public string GetCurrentMiniGameConfigSummary()
        {
            var output = "";
            output += "Difficulty: " + currentGameConfig.Difficulty;
            output += "\nQuestion builder: " + currentQuestionBuilder.GetType().Name;

            // LB Focus
            var contents = AppManager.I.Teacher.VocabularyAi.GetContentsAtLearningBlock(AppManager.I.Player.CurrentJourneyPosition);
            var focusLetters = contents.GetHashSet<LetterData>();
            output += "\nFocus letters: " + focusLetters.ToDebugString();

            return output;
        }

        /// <summary>
        /// Prepare the configuration for a given minigame.
        /// </summary>
        public IGameConfiguration ConfigureMiniGameScene(MiniGameCode code, string sessionName)
        {
            var miniGameData = AppManager.I.DB.GetMiniGameDataByCode(code);
            var defaultContext = new MinigamesGameContext(code, sessionName);

            // We use reflection to get the correct configuration class given a minigame code
            // This is needed so the Core is not directly dependent on the minigame classes
            const string configurationKey = "Configuration";
            const string assessmentNamespaceKey = "Assessment";
            const string minigamesNamespaceKey = "Minigames";
            const string baseNamespaceKey = "Antura";
            const string instanceFieldName = "Instance";

            string miniGameSceneKey = miniGameData.Scene.Split('_')[1];
            string configurationClassName = miniGameSceneKey + "." + miniGameSceneKey + configurationKey;
            if (miniGameSceneKey != assessmentNamespaceKey) {
                configurationClassName = minigamesNamespaceKey + "." + configurationClassName;
            }
            configurationClassName = baseNamespaceKey + "." + configurationClassName;

            var configurationClassType = Type.GetType(configurationClassName);
            if (configurationClassType == null) {
                throw new Exception("Type " + configurationClassName + " not found. Are the minigame scene and Configuration class ready?");
            }

            var property = configurationClassType.GetProperty(instanceFieldName, BindingFlags.Public | BindingFlags.Static);
            if (property == null) {
                throw new Exception("Public static property named " + instanceFieldName +
                                    " not found. This should be present in the minigame's Configuration class.");
            }

            var currentGameConfig = (IGameConfiguration)property.GetValue(null, null);

            if (currentGameConfig != null) {
                currentGameConfig.Context = defaultContext;
                currentGameConfig.SetMiniGameCode(code);
            }

            return currentGameConfig;
        }
    }
}