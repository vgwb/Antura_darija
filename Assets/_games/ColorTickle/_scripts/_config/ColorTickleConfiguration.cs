using Antura.Database;
using Antura.Teacher;
using System;

namespace Antura.Minigames.ColorTickle
{
    public enum ColorTickleVariation
    {
        LetterName = MiniGameCode.ColorTickle_lettername,
    }

    public class ColorTickleConfiguration : AbstractGameConfiguration
    {
        private ColorTickleVariation Variation { get; set; }

        public override void SetMiniGameCode(MiniGameCode code)
        {
            Variation = (ColorTickleVariation)code;
        }

        // Singleton Pattern
        static ColorTickleConfiguration instance;
        public static ColorTickleConfiguration Instance
        {
            get {
                if (instance == null) {
                    instance = new ColorTickleConfiguration();
                }
                return instance;
            }
        }

        private ColorTickleConfiguration()
        {
            // Default values
            Questions = new ColorTickleLetterProvider();
            Context = new MinigamesGameContext(MiniGameCode.ColorTickle_lettername, System.DateTime.Now.Ticks.ToString());
            Difficulty = 0.5f;
            TutorialEnabled = true;
            Variation = ColorTickleVariation.LetterName;
        }

        public override IQuestionBuilder SetupBuilder()
        {
            IQuestionBuilder builder = null;

            int nPacks = 10;
            int nCorrect = 1;

            var builderParams = new QuestionBuilderParameters();
            switch (Variation) {
                case ColorTickleVariation.LetterName:
                    builderParams.letterFilters.excludeDiacritics = LetterFilters.ExcludeDiacritics.All;
                    builderParams.letterFilters.excludeLetterVariations = LetterFilters.ExcludeLetterVariations.AllButAlefHamza;
                    builderParams.letterFilters.excludeDiphthongs = true;
                    builderParams.wordFilters.excludeDiacritics = true;
                    builder = new RandomLettersQuestionBuilder(nPacks, nCorrect, parameters: builderParams);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return builder;
        }

        public override MiniGameLearnRules SetupLearnRules()
        {
            var rules = new MiniGameLearnRules();
            // example: a.minigameVoteSkewOffset = 1f;
            return rules;
        }

        public override LetterDataSoundType GetVocabularySoundType()
        {
            LetterDataSoundType soundType;
            switch (Variation) {
                case ColorTickleVariation.LetterName:
                    soundType = LetterDataSoundType.Name;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return soundType;
        }
    }
}
