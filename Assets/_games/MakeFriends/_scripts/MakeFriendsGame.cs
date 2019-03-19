﻿using Antura.Database;
using Antura.LivingLetters;
using Antura.Tutorial;
using Antura.UI;
using ArabicSupport;
using Antura.Core;
using Antura.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Antura.Minigames.MakeFriends
{
    public class MakeFriendsGame : MiniGameController
    {
        public LivingLetterArea leftArea;
        public LivingLetterArea rightArea;
        public LetterPickerController letterPicker;
        public Canvas endGameCanvas;
        public GameObject sceneCamera;
        public const int numberOfRounds = 6;
        //public float uiDelay;
        public float feedbackDuration;
        public float loseDuration;
        public Vector3 endCameraPosition;
        public Vector3 endCameraRotation;
        public GameObject letterBalloonPrefab;
        public GameObject letterBalloonContainer;
        public GameObject FxParticlesPoof;
        public DropZoneController dropZone;
        public RoundResultAnimator roundResultAnimator;
        public MakeFriendsAnturaController antura;
        public Transform tutorialDropZoneLocation;
        public Transform[] tutorialLetterLocations;
        public Vector3 correctChoiceIndicatorPosition;
        public Camera uiCamera;
        [Header("Difficulty Override")]
        public bool overrideDifficulty;
        public MakeFriendsDifficulty difficultySetting;
        public static MakeFriendsGame Instance { get { return I as MakeFriendsGame; } }

        public bool IsIntroducingLetter = false;
        public int SpokenWords = 0;

        [HideInInspector]
        public MakeFriendsConfiguration Configuration { get { return MakeFriendsConfiguration.Instance; } }

        private LL_WordData wordData1;
        private List<ILivingLetterData> wordLetters1 = new List<ILivingLetterData>();
        private LL_WordData wordData2;
        private List<ILivingLetterData> wordLetters2 = new List<ILivingLetterData>();
        private List<ILivingLetterData> commonLetters = new List<ILivingLetterData>();
        private List<ILivingLetterData> uncommonLetters = new List<ILivingLetterData>();
        private List<ILivingLetterData> choiceLetters = new List<ILivingLetterData>();
        private List<ILivingLetterData> correctChoices = new List<ILivingLetterData>();
        private List<ILivingLetterData> incorrectChoices = new List<ILivingLetterData>();
        private int currentRound = 0;
        private int _currentScore = 0;
        private bool isTutorialRound;

        MakeFriendsLivingLetter livingLetter1;
        MakeFriendsLivingLetter livingLetter2;

        public bool TutorialEnabled
        {
            get { return GetConfiguration().TutorialEnabled; }
        }

        public int CurrentScore
        {
            get { return _currentScore; }
            set {
                _currentScore = value;
                Context.GetOverlayWidget().SetStarsScore(CurrentScore);
            }
        }

        private IPopupWidget Popup { get { return GetConfiguration().Context.GetPopupWidget(); } }

        private IAudioManager AudioManager { get { return GetConfiguration().Context.GetAudioManager(); } }

        public MakeFriendsIntroductionState IntroductionState { get; private set; }

        public MakeFriendsQuestionState QuestionState { get; private set; }

        public MakeFriendsPlayState PlayState { get; private set; }

        public MakeFriendsResultState ResultState { get; private set; }

        private readonly int STARS_1_THRESHOLD = Mathf.CeilToInt(0.33f * numberOfRounds);
        private readonly int STARS_2_THRESHOLD = Mathf.CeilToInt(0.66f * numberOfRounds);
        private readonly int STARS_3_THRESHOLD = numberOfRounds;

        public int CurrentStars
        {
            get {
                if (CurrentScore < STARS_1_THRESHOLD)
                    return 0;
                if (CurrentScore < STARS_2_THRESHOLD)
                    return 1;
                if (CurrentScore < STARS_3_THRESHOLD)
                    return 2;
                return 3;
            }
        }

        protected override void OnInitialize(IGameContext context)
        {
            IntroductionState = new MakeFriendsIntroductionState(this);
            QuestionState = new MakeFriendsQuestionState(this);
            PlayState = new MakeFriendsPlayState(this);
            ResultState = new MakeFriendsResultState(this);
        }

        public void InitializeMinigameUI()
        {
            Context.GetOverlayWidget().Initialize(true, false, false);
            Context.GetOverlayWidget().SetStarsThresholds(STARS_1_THRESHOLD, STARS_2_THRESHOLD, STARS_3_THRESHOLD);
        }

        protected override FSM.IState GetInitialState()
        {
            return IntroductionState;
        }

        protected override IGameConfiguration GetConfiguration()
        {
            return MakeFriendsConfiguration.Instance;
        }

        protected override void Start()
        {
            base.Start();
            PlayIdleMusic();
        }

        public void PlayActiveMusic()
        {
            AudioManager.PlayMusic(Music.Theme9);
        }

        public void PlayIdleMusic()
        {
            AudioManager.PlayMusic(Music.Relax);
        }

        public void PlayTitleVoiceOver()
        {
            AudioManager.PlayDialogue(LocalizationDataId.MakeFriends_letterinword_Title);
        }

        public void PlayTutorialVoiceOver()
        {
            StartCoroutine(PlayDialog_Coroutine(LocalizationDataId.MakeFriends_letterinword_Tuto));
        }

        public void PlayIntroVoiceOver()
        {
            StartCoroutine(PlayDialog_Coroutine(LocalizationDataId.MakeFriends_letterinword_Intro));
        }

        private IEnumerator PlayDialog_Coroutine(LocalizationDataId dialog)
        {
            while (MakeFriendsGame.Instance.SpokenWords < 2)
                yield return null;
            
            AudioManager.PlayDialogue(dialog);
        }

        public void Play()
        {
            currentRound++;
            if (currentRound <= numberOfRounds) {
                StartNewRound();
            } else {
                EndGame();
            }
        }

        public void StartNewRound()
        {
            Reset();
            SetNewWords();
            SetLetterChoices();
            SpawnLivingLetters();
            ShowLetterPicker();
            ShowDropZone();
        }

        public void PlayTutorial()
        {
            Debug.Log("Playing Tutorial");

            isTutorialRound = true;
            PlayIdleMusic();
            Reset();
            SetNewWords();
            SetLetterChoices();
            SpawnLivingLetters();
            ShowLetterPicker();
            ShowDropZone();

            PlayTutorialVoiceOver();

            StartCoroutine("ShowTutorialUI_Coroutine");
        }

        private IEnumerator ShowTutorialUI_Coroutine()
        {
            while (MakeFriendsGame.Instance.SpokenWords < 2)
                yield return null;

            yield return new WaitForSeconds(0.5f);

            while (isTutorialRound) {
                for (int i = 0; i < letterPicker.CorrectLetterChoices.Count; i++) {
                    var choice = letterPicker.CorrectLetterChoices[i];

                    if (choice.isCorrectChoice && !choice.IsDisabled && !letterPicker.IsBlocked) {
                        var from = choice.transform.position;
                        var to = dropZone.transform.position;
                        TutorialUI.SetCamera(uiCamera);
                        TutorialUI.DrawLine(from, to, TutorialUI.DrawLineMode.Finger, false, false);
                    }
                }
                yield return new WaitForSeconds(2f);
            }
        }

        private void HideTutorialUI()
        {
            StopCoroutine("ShowTutorialUI_Coroutine");
            TutorialUI.Clear(false);
        }

        private void SetNewWords()
        {
            wordData1 = null;
            wordData2 = null;
            wordLetters1.Clear();
            wordLetters2.Clear();
            commonLetters.Clear();
            uncommonLetters.Clear();

            var question = GetConfiguration().Questions.GetNextQuestion();

            List<ILivingLetterData> words = question.GetQuestions().ToList();
            wordData1 = words[0] as LL_WordData;
            wordData2 = words[1] as LL_WordData;
            commonLetters = question.GetCorrectAnswers().ToList();
            uncommonLetters = question.GetWrongAnswers().ToList();

            Debug.Log("[New Round] Word 1: " + ArabicFixer.Fix(wordData1.Data.Arabic) + ", Word 2: " + ArabicFixer.Fix(wordData2.Data.Arabic)
                + "\nCommon: " + string.Join(" / ", commonLetters.Select(x => x.TextForLivingLetter.ToString()).Reverse().ToArray())
                + ", Uncommon: " + string.Join(" / ", uncommonLetters.Select(x => x.TextForLivingLetter.ToString()).Reverse().ToArray()));
        }

        private void SetLetterChoices()
        {
            choiceLetters.AddRange(commonLetters);
            if (choiceLetters.Count > letterPicker.letterChoices.Length) {
                choiceLetters = choiceLetters.GetRange(0, letterPicker.letterChoices.Length);
            }
            //Debug.Log("Added " + choiceLetters.Count + " common letters to choices");

            int vacantChoiceLettersCount = letterPicker.letterChoices.Length - choiceLetters.Count;

            // Get other random letters (without repetition)
            for (int i = 0; i < vacantChoiceLettersCount; i++) {
                LL_LetterData letter;
                do {
                    if (i < uncommonLetters.Count) {
                        letter = uncommonLetters[i] as LL_LetterData;
                        //Debug.Log("Considering as choice: " + letter.TextForLivingLetter);
                        if (choiceLetters.Exists(x => x.Id == letter.Id)) {
                            letter = AppManager.I.Teacher.GetAllTestLetterDataLL().GetRandom();
                            //Debug.Log("Using random choice instead: " + letter);
                        }
                    } else {
                        letter = AppManager.I.Teacher.GetAllTestLetterDataLL().GetRandom();
                        //Debug.Log("No more word letters, using random: " + letter.TextForLivingLetter);
                    }
                } while (choiceLetters.Exists(x => x.Id == letter.Id));
                choiceLetters.Add(letter);
                //Debug.Log("Added " + letter.TextForLivingLetter + " to choices");
            }
            choiceLetters.Shuffle();

            letterPicker.DisplayLetters(choiceLetters);
            if (isTutorialRound) {
                letterPicker.SetCorrectChoices(commonLetters);
            }
        }

        private void SpawnLivingLetters()
        {
            livingLetter1 = leftArea.SpawnLivingLetter(wordData1);
            livingLetter2 = rightArea.SpawnLivingLetter(wordData2);

            leftArea.MakeEntrance();
            rightArea.MakeEntrance();
        }

        private void ShowDropZone()
        {
            dropZone.Appear();
        }

        private void HideDropZone()
        {
            dropZone.Disappear();
        }

        private void ShowLetterPicker(float delay = 0)
        {
            letterPicker.Block();
            letterPicker.ShowAndUnblockDelayed(delay);
        }

        private void HideLetterPicker()
        {
            letterPicker.StopAllCoroutines();
            letterPicker.Block();
            letterPicker.Hide();
        }

        public void NextRound(float delay = 0f)
        {
            StartCoroutine(NextRound_Coroutine(delay));
        }

        private IEnumerator NextRound_Coroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            Play();
        }

        public void OnLetterChoiceSelected(LetterChoiceController letterChoice)
        {
            HideLetterPicker();
            ShowLetterPicker(feedbackDuration + 0.75f);

            if (commonLetters.Exists(x => x.Id == letterChoice.letterData.Id)) {
                letterChoice.State = LetterChoiceController.ChoiceState.CORRECT;
                //letterChoice.SpawnBalloon(true);
                GetConfiguration().Context.GetAudioManager().PlaySound(Sfx.LetterHappy);
                dropZone.AnimateCorrect();
                TutorialUI.MarkYes(correctChoiceIndicatorPosition, TutorialUI.MarkSize.Normal);
                MakeFriendsConfiguration.Instance.Context.GetAudioManager().PlaySound(Sfx.StampOK);

                if (!correctChoices.Exists(x => x.Id == letterChoice.letterData.Id)) {
                    correctChoices.Add(letterChoice.letterData);
                }

                if (correctChoices.Count >= commonLetters.Count) {
                    EndRound(true);
                } else {
                    dropZone.ResetLetter(feedbackDuration);
                }
                antura.ReactPositively();
            } else {
                letterChoice.State = LetterChoiceController.ChoiceState.WRONG;
                //letterChoice.SpawnBalloon(false);
                GetConfiguration().Context.GetAudioManager().PlaySound(Sfx.LetterSad);
                dropZone.AnimateWrong();
                TutorialUI.MarkNo(correctChoiceIndicatorPosition, TutorialUI.MarkSize.Normal);
                MakeFriendsConfiguration.Instance.Context.GetAudioManager().PlaySound(Sfx.KO);
                dropZone.ResetLetter(feedbackDuration);
                incorrectChoices.Add(letterChoice.letterData);
                antura.ReactNegatively();
                if (!isTutorialRound) {
                    leftArea.MoveAwayAngrily();
                    rightArea.MoveAwayAngrily();
                    if (incorrectChoices.Count >= 3) {
                        EndRound(false);
                    }
                } else {
                    leftArea.livingLetter.LookAngry();
                    rightArea.livingLetter.LookAngry();
                }
            }
        }

        private void EndRound(bool win)
        {
            StartCoroutine(EndRound_Coroutine(win));
        }

        private IEnumerator EndRound_Coroutine(bool win)
        {
            var winDelay1 = leftArea.celebrationDuration + 1.5f;
            var winDelay2 = 1.5f;
            var friendlyExitDelay = leftArea.friendlyExitDuration;
            var loseDelay = 1.5f;

            HideLetterPicker();

            if (win) {
                Debug.Log("Win");

                if (isTutorialRound) {
                    Debug.Log("Cleared tutorial");
                    HideTutorialUI();
                }

                List<LL_LetterData> letters = new List<LL_LetterData>();

                foreach (var l in commonLetters)
                {
                    LL_LetterData data = l as LL_LetterData;

                    if (data != null)
                        letters.Add(data);
                }

                livingLetter1.MarkLetters(letters, Color.green);
                livingLetter2.MarkLetters(letters, Color.green);

                GetConfiguration().Context.GetAudioManager().PlaySound(Sfx.Win);
                leftArea.Celebrate();
                rightArea.Celebrate();
                leftArea.HighFive(leftArea.celebrationDuration);
                rightArea.HighFive(rightArea.celebrationDuration);
                roundResultAnimator.ShowWin();
                if (!isTutorialRound) {
                    CurrentScore++;
                    Context.GetLogManager().OnAnswered(wordData1, true);
                    Context.GetLogManager().OnAnswered(wordData2, true);
                }

                // Exit
                yield return new WaitForSeconds(winDelay1);
                roundResultAnimator.Hide();
                leftArea.MakeFriendlyExit();
                rightArea.MakeFriendlyExit();

                // Go to Friends Zone
                if (!isTutorialRound) {
                    yield return new WaitForSeconds(friendlyExitDelay);
                    leftArea.GoToFriendsZone(FriendsZonesManager.instance.currentZone);
                    rightArea.GoToFriendsZone(FriendsZonesManager.instance.currentZone);
                    FriendsZonesManager.instance.IncrementCurrentZone();
                }

                // Hide answer Drop Zone
                yield return new WaitForSeconds(winDelay2);
                HideDropZone();

                if (isTutorialRound) {
                    isTutorialRound = false;
                    IntroductionState.OnFinishedTutorial();
                } else {
                    NextRound();
                }
            } else {
                Debug.Log("Lose");

                Context.GetLogManager().OnAnswered(wordData1, false);
                Context.GetLogManager().OnAnswered(wordData2, false);

                GetConfiguration().Context.GetAudioManager().PlaySound(Sfx.Lose);
                yield return new WaitForSeconds(loseDelay);
                HideDropZone();
                roundResultAnimator.ShowLose();
                //TutorialUI.MarkNo(Vector3.zero, TutorialUI.MarkSize.Huge);
                NextRound(loseDuration);
            }
        }

        public void Reset()
        {
            IsIntroducingLetter = false;
            SpokenWords = 0;

            commonLetters.Clear();
            choiceLetters.Clear();
            correctChoices.Clear();
            incorrectChoices.Clear();
            wordData1 = null;
            wordData2 = null;
            wordLetters1.Clear();
            wordLetters2.Clear();

            letterPicker.Reset();
            dropZone.Reset();
            leftArea.Reset();
            rightArea.Reset();
        }

        private void EndGame()
        {
            StartCoroutine(EndGame_Coroutine());
        }

        private IEnumerator EndGame_Coroutine()
        {
            var delay1 = 0.25f;
            yield return new WaitForSeconds(delay1);

            PlayIdleMusic();
            Reset();

            // Everybody dance!
            FriendsZonesManager.instance.EverybodyDance();
            antura.ReactToEndGame();

            yield return new WaitForSeconds(1.0f);

            antura.animationController.DoShout();
            Context.GetAudioManager().PlaySound(Sfx.DogBarking);

            /*
            // Zoom out camera
            var fromPosition = sceneCamera.transform.localPosition;
            var toPosition = endCameraPosition;
            var fromRotation = sceneCamera.transform.localRotation.eulerAngles;
            var toRotation = endCameraRotation;
            var interpolant = 0f;
            var lerpProgress = 0f;
            var lerpLength = 2f;

            while (lerpProgress < lerpLength) {
                sceneCamera.transform.localPosition = Vector3.Lerp(fromPosition, toPosition, interpolant);
                sceneCamera.transform.localRotation = Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, interpolant));
                lerpProgress += Time.deltaTime;
                interpolant = lerpProgress / lerpLength;
                interpolant = Mathf.Sin(interpolant * Mathf.PI * 0.5f);
                yield return new WaitForFixedUpdate();
            }
            */

            //            endGameCanvas.gameObject.SetActive(true);
            //
            //            int numberOfStars = 0;
            //
            //            if (friendships <= 0)
            //            {
            //                numberOfStars = 0;
            //                WidgetSubtitles.I.DisplaySentence("game_result_retry");
            //            }
            //            else if ((float)friendships / numberOfRounds < 0.5f)
            //            {
            //                numberOfStars = 1;
            //                WidgetSubtitles.I.DisplaySentence("game_result_fair");
            //            }
            //            else if (friendships < numberOfRounds)
            //            {
            //                numberOfStars = 2;
            //                WidgetSubtitles.I.DisplaySentence("game_result_good");
            //            }
            //            else
            //            {
            //                numberOfStars = 3;
            //                WidgetSubtitles.I.DisplaySentence("game_result_great");
            //            }
            //            starFlowers.Show(numberOfStars);

            PlayState.OnResult();
        }
    }

}
