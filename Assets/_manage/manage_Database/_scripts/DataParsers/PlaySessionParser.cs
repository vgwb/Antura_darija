﻿using System.Collections.Generic;
using UnityEngine;

namespace Antura.Database.Management
{
    /// <summary>
    /// Custom JSON parser for PlaySessionData
    /// </summary>
    public class PlaySessionParser : DataParser<PlaySessionData, PlaySessionTable>
    {
        override protected PlaySessionData CreateData(Dictionary<string, object> dict, DatabaseObject db)
        {
            var data = new PlaySessionData();

            data.Type = ToString(dict["Type"]);

            data.Stage = ToInt(dict["Stage"]);
            data.LearningBlock = ToInt(dict["LearningBlock"]);
            data.PlaySession = ToInt(dict["PlaySession"]);
            data.Id = data.Stage + "." + data.LearningBlock + "." + data.PlaySession;

            data.Letters = ParseIDArray<LetterData, LetterTable>(data, (string)dict["Letters"], db.GetLetterTable());
            //CustomAddDiacritics(data, db);

            data.Words = ParseIDArray<WordData, WordTable>(data, (string)dict["Words"], db.GetWordTable());
            data.Words_previous = ParseIDArray<WordData, WordTable>(data, (string)dict["Words_previous"], db.GetWordTable());
            data.Phrases = ParseIDArray<PhraseData, PhraseTable>(data, (string)dict["Phrases"], db.GetPhraseTable());
            data.Phrases_previous = ParseIDArray<PhraseData, PhraseTable>(data, (string)dict["Phrases_previous"], db.GetPhraseTable());

            data.Order = ParseEnum<PlaySessionDataOrder>(data, dict["Order"]);
            data.NumberOfMinigames = ToInt(dict["NumberOfMinigames"]);
            data.Minigames = CustomParseMinigames(data, dict, db.GetMiniGameTable());
            data.NumberOfRoundsPerMinigame = ToInt(dict["NumberOfRounds"]);

            return data;
        }

        private void CustomAddDiacritics(PlaySessionData psData, DatabaseObject db)
        {
            // Make sure to also add all combos, if a symbol is found
            HashSet<string> newLetters = new HashSet<string>();
            newLetters.UnionWith(psData.Letters);
            foreach (var letterId in psData.Letters) {
                var letterData = db.GetById(db.GetLetterTable(), letterId);
                if (letterData.Kind == LetterDataKind.Symbol && letterData.Type == LetterDataType.DiacriticSymbol) {
                    // this is a symbol
                    var symbolId = letterId;
                    var allDiacriticCombos = db.FindAll(db.GetLetterTable(), x => x.Symbol == symbolId);
                    newLetters.UnionWith(allDiacriticCombos.ConvertAll(x => x.Id));
                }
            }
            psData.Letters = new string[newLetters.Count];
            newLetters.CopyTo(psData.Letters);
        }

        private List<MiniGameCode> notFoundCodes = new List<MiniGameCode>();
        public MiniGameInPlaySession[] CustomParseMinigames(PlaySessionData PSdata, Dictionary<string, object> dict, MiniGameTable table)
        {
            var list = new List<MiniGameInPlaySession>();

            if (PSdata.Type == "Assessment") {
                // Assessments have AssessmentType as their minigame
                var minigameStruct = new MiniGameInPlaySession();
                var assessmentType = ToString(dict["AssessmentType"]);
                if (assessmentType == "") {
                    Debug.LogWarning(PSdata.GetType() + " could not find AssessmentType for assessment " + PSdata.Id);
                    return list.ToArray(); // this means that no assessment type has been selected
                }
                minigameStruct.MiniGameCode = (MiniGameCode)System.Enum.Parse(typeof(MiniGameCode), assessmentType);
                minigameStruct.Weight = 1;  // weight is forced to be 1

                list.Add(minigameStruct);
            } else {
                // Non-Assessments (i.e. Minigames) must be checked through columns
                for (int enum_i = 0; enum_i < System.Enum.GetValues(typeof(MiniGameCode)).Length; enum_i++) {
                    if ((MiniGameCode)enum_i == MiniGameCode.Invalid) {
                        continue;
                    }

                    var enum_string = ((MiniGameCode)enum_i).ToString();
                    int result = 0;

                    if (enum_string == "") {
                        // this means that the enum does not exist
                        continue;
                    }
                    if (int.TryParse(enum_string, out result)) {
                        // this means that the enum does not exist among the ones we want
                        continue;
                    }

                    // this checks if a minigame isn't used in the PlaySession table
                    if (!dict.ContainsKey(enum_string)) {
                        if (!notFoundCodes.Contains((MiniGameCode)enum_i)) {
                            Debug.LogWarning(PSdata.GetType() + " could not find minigame column for " + enum_string);
                            notFoundCodes.Add((MiniGameCode)enum_i);
                        }
                        continue;
                    }

                    var minigameStruct = new MiniGameInPlaySession();
                    minigameStruct.MiniGameCode = (MiniGameCode)enum_i;
                    // Debug.Log("mingame: " + enum_string);
                    minigameStruct.Weight = ToInt(dict[enum_string]);
                    if (minigameStruct.Weight == 0) {
                        // Skip adding if the weight is zero
                        continue;
                    }

                    list.Add(minigameStruct);
                }

            }

            return list.ToArray();
        }

        protected override void RegenerateEnums(List<Dictionary<string, object>> rowdicts_list)
        {
            ExtractEnum(rowdicts_list, "Order");
        }

    }
}
