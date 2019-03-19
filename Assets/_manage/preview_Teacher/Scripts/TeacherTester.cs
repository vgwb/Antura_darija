using System;
using System.Collections;
using System.Collections.Generic;
using Antura.Assessment;
using Antura.UI;
using DG.DeInspektor.Attributes;
using UnityEngine;
using UnityEngine.UI;
using Antura.Core;

namespace Antura.Teacher.Test
{
    public enum QuestionBuilderType
    {
        Empty,

        RandomLetters,
        RandomLetterForms,
        Alphabet,
        LettersBySunMoon,
        LettersByType,

        RandomWords,
        OrderedWords,
        WordsByArticle,
        WordsByForm,
        WordsBySunMoon,

        LettersInWord,
        LetterFormsInWords,
        LetterAlterationsInWords,
        CommonLettersInWords,
        WordsWithLetter,

        WordsInPhrase,
        PhraseQuestions,

        MAX
    }

    /// <summary>
    /// Helper class to test Teacher functionality regardless of minigames.
    /// </summary>
    public class TeacherTester : MonoBehaviour
    {
        [DeBeginGroup]
        [Header("Reporting")]
        [DeToggleButton(DePosition.HHalfLeft)]
        public bool verboseQuestionPacks = false;
        [DeToggleButton(DePosition.HHalfRight)]
        public bool verboseDataSelection = false;
        [DeToggleButton(DePosition.HHalfLeft)]
        public bool verboseDataFiltering = false;
        [DeEndGroup]
        [DeToggleButton(DePosition.HHalfRight)]
        public bool verbosePlaySessionInitialisation = false;

        [DeBeginGroup]
        [Header("Simulation")]
        public int numberOfSimulations = 50;
        [DeEndGroup]
        public int yieldEverySimulations = 20;

        // Current options
        [DeBeginGroup]
        [Header("Journey")]
        [DeToggleButton()]
        public bool ignoreJourneyPlaySessionSelection = false;
        [Range(1, 6)]
        public int currentJourneyStage = 1;
        [Range(1, 15)]
        public int currentJourneyLB = 1;
        [DeToggleButton()]
        [DeEndGroup]
        public bool isAssessment = false;
        //int currentJourneyPS = 1;

        [DeToggleButton()]
        public bool testWholeJourneyAtButtonClick = false;

        [DeBeginGroup]
        [Header("Selection Parameters")]
        [Range(1, 10)]
        public int nPacks = 5;
        [Range(1, 10)]
        public int nPacksPerRound = 2;
        [DeToggleButton()]
        public bool sortPacksByDifficulty = true;

        [Range(1, 10)]
        public int nCorrectAnswers = 1;
        public SelectionSeverity correctSeverity = SelectionSeverity.MayRepeatIfNotEnough;
        public PackListHistory correctHistory = PackListHistory.RepeatWhenFull;
        [DeToggleButton()]
        public bool journeyEnabledForBase = true;

        [Range(0, 10)]
        public int nWrongAnswers = 1;
        public SelectionSeverity wrongSeverity = SelectionSeverity.MayRepeatIfNotEnough;
        public PackListHistory wrongHistory = PackListHistory.RepeatWhenFull;
        [DeEndGroup]
        [DeToggleButton()]
        public bool journeyEnabledForWrong = true;

        [HideInInspector]
        public InputField journey_stage_in;
        [HideInInspector]
        public InputField journey_learningblock_in;
        [HideInInspector]
        public InputField journey_playsession_in;
        [HideInInspector]
        public InputField npacks_in;
        [HideInInspector]
        public InputField ncorrect_in;
        [HideInInspector]
        public InputField nwrong_in;
        [HideInInspector]
        public Dropdown severity_in;
        [HideInInspector]
        public Dropdown severitywrong_in;
        [HideInInspector]
        public Dropdown history_in;
        [HideInInspector]
        public Dropdown historywrong_in;
        [HideInInspector]
        public Toggle journeybase_in;
        [HideInInspector]
        public Toggle journeywrong_in;

        [HideInInspector]
        public Dictionary<MiniGameCode, Button> minigamesButtonsDict = new Dictionary<MiniGameCode, Button>();
        [HideInInspector]
        public Dictionary<QuestionBuilderType, Button> qbButtonsDict = new Dictionary<QuestionBuilderType, Button>();

        void Start()
        {
            // Setup for testing
            Application.runInBackground = true;
            SetVerboseAI(true);
            ConfigAI.ForceJourneyIgnore = false;

            /*
            journey_stage_in.onValueChanged.AddListener(x => { currentJourneyStage = int.Parse(x); });
            journey_learningblock_in.onValueChanged.AddListener(x => { currentJourneyLB = int.Parse(x); });
            journey_playsession_in.onValueChanged.AddListener(x => { currentJourneyPS = int.Parse(x); });

            npacks_in.onValueChanged.AddListener(x => { nPacks = int.Parse(x); });
            ncorrect_in.onValueChanged.AddListener(x => { nCorrectAnswers = int.Parse(x); });
            nwrong_in.onValueChanged.AddListener(x => { nWrongAnswers = int.Parse(x); });

            severity_in.onValueChanged.AddListener(x => { correctSeverity = (SelectionSeverity)x; });
            severitywrong_in.onValueChanged.AddListener(x => { wrongSeverity = (SelectionSeverity)x; });

            history_in.onValueChanged.AddListener(x => { correctHistory = (PackListHistory)x; });
            historywrong_in.onValueChanged.AddListener(x => { wrongHistory = (PackListHistory)x; });

            journeybase_in.onValueChanged.AddListener(x => { journeyEnabledForBase = x; });
            journeywrong_in.onValueChanged.AddListener(x => { journeyEnabledForWrong = x; });
            */

            GlobalUI.ShowPauseMenu(false);
        }

        private void InitialisePlaySession(Core.JourneyPosition jp = null)
        {
            if (jp == null)
            {
                jp = new Core.JourneyPosition(currentJourneyStage, currentJourneyLB, isAssessment ? 100 : 1);
            }
            AppManager.I.Player.CurrentJourneyPosition.SetPosition(jp.Stage, jp.LearningBlock, jp.PlaySession);
            AppManager.I.Teacher.InitNewPlaySession();
        }

        void SetVerboseAI(bool choice)
        {
            ConfigAI.VerboseTeacher = choice;
        }

        #region Testing API

        void ApplyParameters()
        {
            ConfigAI.VerboseQuestionPacks = verboseQuestionPacks;
            ConfigAI.VerboseDataFiltering = verboseDataFiltering;
            ConfigAI.VerboseDataSelection = verboseDataSelection;
            ConfigAI.VerbosePlaySessionInitialisation = verbosePlaySessionInitialisation;
        }

        private bool IsCodeValid(MiniGameCode code)
        {
            bool isValid = true;
            switch (code)
            {
                case MiniGameCode.Invalid:
                case MiniGameCode.Assessment_VowelOrConsonant:
                    isValid = false;
                    break;
            }
            return isValid;
        }

        private bool IsCodeValid(QuestionBuilderType code)
        {
            bool isValid = true;
            switch (code)
            {
                case QuestionBuilderType.Empty:
                case QuestionBuilderType.MAX:
                    isValid = false;
                    break;
            }
            return isValid;
        }

        [DeMethodButton("Cleanup")]
        public void DoCleanup()
        {
            foreach (var code in Helpers.GenericHelper.SortEnums<QuestionBuilderType>())
            {
                if (!IsCodeValid(code)) continue;
                SetButtonStatus(qbButtonsDict[code], Color.white);
            }

            foreach (var code in Helpers.GenericHelper.SortEnums<MiniGameCode>())
            {
                if (!IsCodeValid(code)) continue;
                SetButtonStatus(minigamesButtonsDict[code], Color.white);
            }
        }

        [DeMethodButton("Test Minimum Journey")]
        public void DoTestMinimumJourney()
        {
            StartCoroutine(DoTest(() => DoTestMinimumJourneyCO()));
        }
        private IEnumerator DoTestMinimumJourneyCO()
        {
            // Test all minigames at their minimum journey
            foreach (var code in Helpers.GenericHelper.SortEnums<MiniGameCode>())
            {
                if (!IsCodeValid(code)) continue;
                var jp = AppManager.I.JourneyHelper.GetMinimumJourneyPositionForMiniGame(code);
                if (jp == null) jp = AppManager.I.JourneyHelper.GetFinalJourneyPosition();
                InitialisePlaySession(jp);
                yield return StartCoroutine(DoTestMinigameCO(code));
            }
        }

        [DeMethodButton("Test Complete Journey")]
        public void DoTestCompleteJourney()
        {
            StartCoroutine(DoTest(() => DoTestCompleteJourneyCO()));
        }
        private IEnumerator DoTestCompleteJourneyCO()
        {
            // Test all minigames at all their available journeys. Stop when we find a wrong one.
            foreach (var code in Helpers.GenericHelper.SortEnums<MiniGameCode>())
            {
                if (!IsCodeValid(code)) continue;
                yield return StartCoroutine(DoTestMinigameWholeJourneyCO(code));
            }
        }

        [DeMethodButton("Test Everything (current PS)")]
        public void DoTestEverything()
        {
            StartCoroutine(DoTest(() => DoTestEverythingCO()));
        }
        private IEnumerator DoTestEverythingCO()
        { 
            yield return StartCoroutine(DoTestAllMiniGamesCO());
            yield return StartCoroutine(DoTestAllQuestionBuildersCO());
        }

        [DeMethodButton("Test Minigames (current PS)")]
        public void DoTestAllMiniGames()
        {
            StartCoroutine(DoTest(() => DoTestAllMiniGamesCO()));
        }
        private IEnumerator DoTestAllMiniGamesCO()
        {
            foreach (var code in Helpers.GenericHelper.SortEnums<MiniGameCode>())
            {
                if (!IsCodeValid(code)) continue;
                yield return StartCoroutine(DoTestMinigameCO(code));
            }
        }

        [DeMethodButton("Test QuestionBuilders (current PS)")]
        public void DoTestAllQuestionBuilders()
        {
            StartCoroutine(DoTest(() => DoTestAllQuestionBuildersCO()));
        }
        private IEnumerator DoTestAllQuestionBuildersCO()
        {
            foreach (var type in Helpers.GenericHelper.SortEnums<QuestionBuilderType>())
            {
                if (!IsCodeValid(type)) continue;
                yield return StartCoroutine(DoTestQuestionBuilderCO(type));
            }
        }

        public void DoTestQuestionBuilder(QuestionBuilderType type)
        {
            StartCoroutine(DoTest(() => DoTestQuestionBuilderCO(type)));
        }
        private IEnumerator DoTestQuestionBuilderCO(QuestionBuilderType type)
        {
            SetButtonStatus(qbButtonsDict[type], Color.yellow);
            yield return new WaitForSeconds(0.1f);
            var statusColor = Color.green;
            try
            {
                SimulateQuestionBuilder(type);
            }
            catch (Exception e)
            {
                Debug.LogError("!! " + type + "\n " + e.Message);
                statusColor = Color.red;
            }
            SetButtonStatus(qbButtonsDict[type], statusColor);
            yield return null;
        }

        public void DoTestMinigame(MiniGameCode code)
        {
            if (testWholeJourneyAtButtonClick)
            {
                StartCoroutine(DoTest(() => DoTestMinigameWholeJourneyCO(code)));
            }
            else
            {
                StartCoroutine(DoTest(() => DoTestMinigameCO(code)));
            }
        }

        private IEnumerator DoTestMinigameWholeJourneyCO(MiniGameCode code)
        {
            int lastStage = 0;
            bool isCorrect = true;
            foreach (var psData in AppManager.I.DB.GetAllPlaySessionData())
            {
                if (!AppManager.I.Teacher.CanMiniGameBePlayedAtPlaySession(psData.GetJourneyPosition(), code)) continue;

                InitialisePlaySession(psData.GetJourneyPosition());

                // Log
                Debug.Log("Testing " + code + " at ps " + psData.GetJourneyPosition());
                if (psData.Stage != lastStage)
                {
                    lastStage = psData.Stage;
                }

                // Skip minigames that found errors
                yield return StartCoroutine(DoTestMinigameCO(code, 0.01f));
                if (minigamesButtonsDict[code].colors.normalColor == Color.red)
                {
                    Debug.LogError("Minigame " + code + " first wrong at ps " + psData.GetJourneyPosition());
                    isCorrect = false;
                    break;
                }
            }
            if (isCorrect)
            {
                Debug.Log("Minigame " + code + " is always fine");
            }
        }


        private IEnumerator DoTestMinigameCO(MiniGameCode code, float delay = 0.1f)
        {
            SetButtonStatus(minigamesButtonsDict[code], Color.yellow);
            yield return new WaitForSeconds(delay);
            var statusColor = Color.green; 

            if (!ignoreJourneyPlaySessionSelection && !AppManager.I.Teacher.CanMiniGameBePlayedAtAnyPlaySession(code))
            {
                Debug.LogError("Cannot select " + code + " for any journey position!");
                statusColor = Color.magenta;
            }
            else
            {
                if (ignoreJourneyPlaySessionSelection || AppManager.I.Teacher.CanMiniGameBePlayedAfterMinPlaySession(AppManager.I.Player.CurrentJourneyPosition, code))
                {
                    try
                    {
                        SimulateMiniGame(code);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("!! " + code + " at PS(" + AppManager.I.Player.CurrentJourneyPosition + ")\n " + e.Message);
                        statusColor = Color.red;
                    }
                }
                else
                {
                    Debug.LogError("Cannot select " + code + " for position " + AppManager.I.Player.CurrentJourneyPosition);
                    statusColor = Color.gray;
                }
            }

            SetButtonStatus(minigamesButtonsDict[code], statusColor);
            yield return null;
        }

        private void SetButtonStatus(Button button, Color statusColor)
        {
            var colors = button.colors;
            colors.normalColor = statusColor;
            colors.highlightedColor = statusColor * 1.2f;
            button.colors = colors;
        }
        private IEnumerator DoTest(Func<IEnumerator> CoroutineFunc)
        {
            ConfigAI.StartTeacherReport();
            ApplyParameters();
            InitialisePlaySession();
            for (int i = 1; i <= numberOfSimulations; i++)
            {
                Debug.Log("************ Simulation " + i + " ************");
                ConfigAI.AppendToTeacherReport("************ Simulation " + i + " ************");
                yield return StartCoroutine(CoroutineFunc());
            }
            ConfigAI.PrintTeacherReport();
        }

        #endregion


        #region Minigames Simulation

        private void SimulateMiniGame(MiniGameCode code)
        {
            var config = AppManager.I.GameLauncher.ConfigureMiniGameScene(code, System.DateTime.Now.Ticks.ToString());
            if (config is IAssessmentConfiguration)
            {
                (config as IAssessmentConfiguration).NumberOfRounds = nPacks;
            }

            var builder = config.SetupBuilder();
            ConfigAI.AppendToTeacherReport("** Minigame " + code + " - " + builder.GetType().Name);

            var questionPacksGenerator = new QuestionPacksGenerator();
            questionPacksGenerator.GenerateQuestionPacks(builder);
        }

        #endregion

        #region  QuestionBuilder Simulation

        public int lettersVariationChoice = 0;

        private void SimulateQuestionBuilder(QuestionBuilderType builderType)
        {

            LetterAlterationFilters letterAlterationFilters = null;
            switch (lettersVariationChoice)
            {
                case 0:
                    letterAlterationFilters = LetterAlterationFilters.FormsOfSingleLetter;
                    break;
                case 1:
                    letterAlterationFilters = LetterAlterationFilters.FormsOfMultipleLetters;
                    break;
                case 2:
                    letterAlterationFilters = LetterAlterationFilters.MultipleLetters;
                    break;
                case 3:
                    letterAlterationFilters = LetterAlterationFilters.PhonemesOfSingleLetter;
                    break;
                case 4:
                    letterAlterationFilters = LetterAlterationFilters.PhonemesOfMultipleLetters;
                    break;
                case 5:
                    letterAlterationFilters = LetterAlterationFilters.FormsAndPhonemesOfMultipleLetters;
                    break;
            }


            var builderParams = SetupBuilderParameters();
            IQuestionBuilder builder = null;
            switch (builderType)
            {
                case QuestionBuilderType.RandomLetters:
                    builder = new RandomLettersQuestionBuilder(nPacks: nPacks, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, firstCorrectIsQuestion: true, parameters: builderParams);
                    break;
                case QuestionBuilderType.RandomLetterForms:
                    builder = new RandomLetterAlterationsQuestionBuilder(nPacks: nPacks, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, letterAlterationFilters: letterAlterationFilters, parameters: builderParams);
                    break;
                case QuestionBuilderType.Alphabet:
                    builder = new AlphabetQuestionBuilder(parameters: builderParams);
                    break;
                case QuestionBuilderType.LettersBySunMoon:
                    builder = new LettersBySunMoonQuestionBuilder(nPacks: nPacks, parameters: builderParams);
                    break;
                case QuestionBuilderType.LettersByType:
                    builder = new LettersByTypeQuestionBuilder(nPacks: nPacks, parameters: builderParams);
                    break;
                case QuestionBuilderType.LettersInWord:
                    builder = new LettersInWordQuestionBuilder(nRounds: nPacks, nPacksPerRound: nPacksPerRound, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, useAllCorrectLetters: true, parameters: builderParams);
                    break;
                case QuestionBuilderType.LetterFormsInWords:
                    builder = new LetterFormsInWordsQuestionBuilder(nPacks, nPacksPerRound, parameters: builderParams);
                    break;
                case QuestionBuilderType.LetterAlterationsInWords:
                    builder = new LetterAlterationsInWordsQuestionBuilder(nPacks, nPacksPerRound, parameters: builderParams, letterAlterationFilters: letterAlterationFilters);
                    break;
                case QuestionBuilderType.CommonLettersInWords:
                    builder = new CommonLetterInWordQuestionBuilder(nPacks: nPacks, nWrong: nWrongAnswers, parameters: builderParams);
                    break;
                case QuestionBuilderType.RandomWords:
                    builder = new RandomWordsQuestionBuilder(nPacks: nPacks, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, firstCorrectIsQuestion: true, parameters: builderParams);
                    break;
                case QuestionBuilderType.OrderedWords:
                    builder = new OrderedWordsQuestionBuilder(Database.WordDataCategory.Number, parameters: builderParams);
                    break;
                case QuestionBuilderType.WordsWithLetter:
                    builder = new WordsWithLetterQuestionBuilder(nRounds: nPacks, nPacksPerRound: nPacksPerRound, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, parameters: builderParams);
                    break;
                case QuestionBuilderType.WordsByForm:
                    builder = new WordsByFormQuestionBuilder(nPacks: nPacks, parameters: builderParams);
                    break;
                case QuestionBuilderType.WordsByArticle:
                    builder = new WordsByArticleQuestionBuilder(nPacks: nPacks, parameters: builderParams);
                    break;
                case QuestionBuilderType.WordsBySunMoon:
                    builder = new WordsBySunMoonQuestionBuilder(nPacks: nPacks, parameters: builderParams);
                    break;
                case QuestionBuilderType.WordsInPhrase:
                    builder = new WordsInPhraseQuestionBuilder(nPacks: nPacks, nCorrect: nCorrectAnswers, nWrong: nWrongAnswers, useAllCorrectWords: false, usePhraseAnswersIfFound: true, parameters: builderParams);
                    break;
                case QuestionBuilderType.PhraseQuestions:
                    builder = new PhraseQuestionsQuestionBuilder(nPacks: nPacks, nWrong: nWrongAnswers, parameters: builderParams);
                    break;
            }

            var questionPacksGenerator = new QuestionPacksGenerator();
            questionPacksGenerator.GenerateQuestionPacks(builder);
        }

        QuestionBuilderParameters SetupBuilderParameters()
        {
            var builderParams = new QuestionBuilderParameters();
            builderParams.correctChoicesHistory = correctHistory;
            builderParams.wrongChoicesHistory = wrongHistory;
            builderParams.correctSeverity = correctSeverity;
            builderParams.wrongSeverity = wrongSeverity;
            builderParams.useJourneyForCorrect = journeyEnabledForBase;
            builderParams.useJourneyForWrong = journeyEnabledForWrong;
            builderParams.sortPacksByDifficulty = sortPacksByDifficulty;
            return builderParams;
        }

        #endregion

    }

}
