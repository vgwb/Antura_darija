using System.Collections.Generic;
using System.Linq;
using Antura.Core;
using Antura.Database;
using Antura.Helpers;
using UnityEngine;

namespace Antura.Teacher
{
    /// <summary>
    /// Selects alterations of letters at random
    /// * Question: The alteration to find
    /// * Correct answers: The correct alteration
    /// * Wrong answers: Wrong alteration
    /// </summary>
    public class RandomLetterAlterationsQuestionBuilder : IQuestionBuilder
    {
        // focus: Letters, either different or alterations of the same letter
        // pack history filter: parameterized
        // journey: enabled

        private int nPacks;
        private int nCorrect;
        private int nWrong;
        private QuestionBuilderParameters parameters;
        private LetterAlterationFilters letterAlterationFilters;
        private bool avoidWrongLettersWithSameSound;

        public QuestionBuilderParameters Parameters
        {
            get { return this.parameters; }
        }
       
        public RandomLetterAlterationsQuestionBuilder(int nPacks, int nCorrect = 1, int nWrong = 0, 
            LetterAlterationFilters letterAlterationFilters = null,
            bool avoidWrongLettersWithSameSound = false,
            QuestionBuilderParameters parameters = null
            )
        {
            if (letterAlterationFilters == null) letterAlterationFilters = new LetterAlterationFilters();

            if (parameters == null)
            {
                parameters = new QuestionBuilderParameters();
            }

            this.nPacks = nPacks;
            this.nCorrect = nCorrect;
            this.nWrong = nWrong;
            this.parameters = parameters;
            this.letterAlterationFilters = letterAlterationFilters;
            this.avoidWrongLettersWithSameSound = avoidWrongLettersWithSameSound;

            // Forced filters
            // We need only base letters as the basis here
            this.parameters.letterFilters.excludeDiacritics = LetterFilters.ExcludeDiacritics.All;
            this.parameters.letterFilters.excludeLetterVariations = LetterFilters.ExcludeLetterVariations.All;
            this.parameters.letterFilters.excludeDiphthongs = true;
        }

        private List<string> previousPacksIDs = new List<string>();

        public List<QuestionPackData> CreateAllQuestionPacks()
        {
            previousPacksIDs.Clear();

            var packs = new List<QuestionPackData>();
            for (int pack_i = 0; pack_i < nPacks; pack_i++)
            {
                var pack = CreateSingleQuestionPackData();
                packs.Add(pack);
            }
            return packs;
        }

        private QuestionPackData CreateSingleQuestionPackData()
        {
            var teacher = AppManager.I.Teacher;
            var vocabularyHelper = AppManager.I.VocabularyHelper;

            // First, get all letters (only base letters, tho, due to forced letter filters)
            int nBaseLettersRequired = 1;
            if (letterAlterationFilters.differentBaseLetters) nBaseLettersRequired = nCorrect + nWrong;
            var chosenLetters = teacher.VocabularyAi.SelectData(
                () => vocabularyHelper.GetAllLetters(parameters.letterFilters),
                    new SelectionParameters(parameters.correctSeverity, nRequired: nBaseLettersRequired, useJourney: parameters.useJourneyForCorrect,
                        packListHistory: parameters.correctChoicesHistory, filteringIds: previousPacksIDs)
            );
            var baseLetters = chosenLetters;

            // Then, find all the different variations and add them to a pool
            var letterPool = vocabularyHelper.GetAllLetterAlterations(baseLetters, letterAlterationFilters);

            // Choose randomly from that pool
            var correctAnswers = letterPool.RandomSelect(nCorrect);
            var wrongAnswers = letterPool;
            foreach (LetterData data in correctAnswers)
                wrongAnswers.Remove(data);

            if (avoidWrongLettersWithSameSound)
            {
                wrongAnswers.RemoveAll(wrongLetter => correctAnswers.Any(correctLetter => correctLetter.PhonemeSound.Equals(wrongLetter.PhonemeSound)));
            }

            wrongAnswers = wrongAnswers.RandomSelect(Mathf.Min(nWrong,wrongAnswers.Count));

            var question = correctAnswers[0];

            if (ConfigAI.VerboseQuestionPacks)
            {
                string debugString = "--------- TEACHER: question pack result ---------";
                debugString += "\nQuestion: " + question;
                debugString += "\nCorrect Answers: " + correctAnswers.Count;
                foreach (var l in correctAnswers) debugString += " " + l;
                debugString += "\nWrong Answers: " + wrongAnswers.Count;
                foreach (var l in wrongAnswers) debugString += " " + l;
                ConfigAI.AppendToTeacherReport(debugString);
            }

            return QuestionPackData.Create(question, correctAnswers, wrongAnswers);
        }

    }
}