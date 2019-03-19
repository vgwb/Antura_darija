﻿using Antura.Core;
using Antura.Database;
using ArabicSupport;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antura.Helpers
{
    // TODO refactor: We should create an intermediate layer for accessing language-specific helpers, so that they can be removed easily.
    // TODO refactor: this class needs a large refactoring as it is used for several different purposes
    public static class ArabicAlphabetHelper
    {
        public struct ArabicStringPart
        {
            public LetterData letter;
            public int fromCharacterIndex;
            public int toCharacterIndex;
            public LetterForm letterForm;

            public ArabicStringPart(LetterData letter, int fromCharacterIndex, int toCharacterIndex, LetterForm letterForm)
            {
                this.letter = letter;
                this.fromCharacterIndex = fromCharacterIndex;
                this.toCharacterIndex = toCharacterIndex;
                this.letterForm = letterForm;
            }
        }

        struct UnicodeLookUpEntry
        {
            public LetterData data;
            public LetterForm form;

            public UnicodeLookUpEntry(Database.LetterData data, Database.LetterForm form)
            {
                this.data = data;
                this.form = form;
            }
        }

        struct DiacriticComboLookUpEntry
        {
            public string symbolID;
            public string LetterID;

            public DiacriticComboLookUpEntry(string symbolID, string LetterID)
            {
                this.symbolID = symbolID;
                this.LetterID = LetterID;
            }
        }

        static List<LetterData> allLetterData;
        static Dictionary<string, UnicodeLookUpEntry> unicodeLookUpCache = new Dictionary<string, UnicodeLookUpEntry>();

        static Dictionary<DiacriticComboLookUpEntry, LetterData> diacriticComboLookUpCache = new Dictionary<DiacriticComboLookUpEntry, LetterData>();

        /// <summary>
        /// Collapses diacritics and letters, collapses multiple words variations (e.g. lam + alef), selects correct forms unicodes, and reverses the string.
        /// </summary>
        /// <returns>The string, ready for display or further processing.</returns>
        public static string ProcessArabicString(string str)
        {
            return GenericHelper.ReverseText(ArabicFixer.Fix(str, true, true));
        }

        public static string GetStringUnicodes(string str)
        {
            char[] chars = str.ToCharArray();
            string output = "";
            for (int i = 0; i < chars.Length; i++) {
                char character = chars[i];
                output += GetHexUnicodeFromChar(character) + ", ";
            }
            return output;
        }

        /// <summary>
        /// Return single letter string start from unicode hex code.
        /// </summary>
        /// <param name="hexCode">string Hexadecimal number</param>
        /// <returns>string char</returns>
        public static string GetLetterFromUnicode(string hexCode)
        {
            if (hexCode == "") {
                Debug.LogError("Letter requested with an empty hexacode (data is probably missing from the DataBase). Returning - for now.");
                hexCode = "002D";
            }

            int unicode = int.Parse(hexCode, System.Globalization.NumberStyles.HexNumber);
            var character = (char)unicode;
            return character.ToString();
        }

        /// <summary>
        /// Get char hexa code.
        /// </summary>
        /// <param name="_char"></param>
        /// <param name="unicodePrefix"></param>
        /// <returns></returns>
        public static string GetHexUnicodeFromChar(char _char, bool unicodePrefix = false)
        {
            return string.Format("{1}{0:X4}", Convert.ToUInt16(_char), unicodePrefix ? "/U" : string.Empty);
        }

        /// <summary>
        /// Return a string of a word without a character. Warning: the word is already reversed and fixed for rendering.
        /// This is mandatory since PrepareArabicStringForDisplay should be called before adding removedLetterChar.
        /// </summary>
        public static string GetWordWithMissingLetterText(WordData arabicWord, ArabicStringPart partToRemove,
            string removedLetterChar = "_")
        {
            string text = ProcessArabicString(arabicWord.Arabic);

            int toCharacterIndex = partToRemove.toCharacterIndex + 1;
            text = text.Substring(0, partToRemove.fromCharacterIndex) + removedLetterChar +
                   (toCharacterIndex >= text.Length ? "" : text.Substring(toCharacterIndex));

            return text;
        }

        /// <summary>
        /// Find all the occurrences of "letterToFind" in "arabicWord"
        /// </summary>
        /// <returns>the list of occurrences</returns>
        public static List<ArabicStringPart> FindLetter(DatabaseManager database, WordData arabicWord, LetterData letterToFind, bool findSameForm)
        {
            var result = new List<ArabicStringPart>();

            var parts = SplitWord(database, arabicWord, false, letterToFind.Kind != LetterDataKind.LetterVariation);

            for (int i = 0, count = parts.Count; i < count; ++i) {
                if (parts[i].letter.Id == letterToFind.Id && (!findSameForm || (parts[i].letterForm == letterToFind.Form))) {
                    result.Add(parts[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the list of letters found in a word string
        /// </summary>
        public static List<ArabicStringPart> SplitWord(DatabaseManager database, WordData arabicWord, bool separateDiacritics = false, bool separateVariations = false)
        {
            // Use ArabicFixer to deal only with combined unicodes
            return AnalyzeArabicString(database.StaticDatabase, ProcessArabicString(arabicWord.Arabic), separateDiacritics, separateVariations);
        }

        public static List<ArabicStringPart> SplitWord(DatabaseObject staticDatabase, WordData arabicWord, bool separateDiacritics = false, bool separateVariations = false)
        {
            // Use ArabicFixer to deal only with combined unicodes
            return AnalyzeArabicString(staticDatabase, ProcessArabicString(arabicWord.Arabic), separateDiacritics, separateVariations);
        }

        /// <summary>
        /// Returns the list of letters found in a word string
        /// </summary>
        public static List<ArabicStringPart> SplitPhrase(DatabaseManager database, PhraseData phrase, bool separateDiacritics = false,
            bool separateVariations = true)
        {
            // Use ArabicFixer to deal only with combined unicodes
            return AnalyzeArabicString(database.StaticDatabase, ProcessArabicString(phrase.Arabic), separateDiacritics, separateVariations);
        }

        public static List<ArabicStringPart> SplitPhrase(DatabaseObject staticDatabase, PhraseData phrase, bool separateDiacritics = false,
            bool separateVariations = true)
        {
            // Use ArabicFixer to deal only with combined unicodes
            return AnalyzeArabicString(staticDatabase, ProcessArabicString(phrase.Arabic), separateDiacritics, separateVariations);
        }

        static List<ArabicStringPart> AnalyzeArabicString(DatabaseObject staticDatabase, string processedArabicString,
            bool separateDiacritics = false, bool separateVariations = true)
        {
            if (allLetterData == null) {
                allLetterData = new List<LetterData>(staticDatabase.GetLetterTable().GetValuesTyped());

                for (int l = 0; l < allLetterData.Count; ++l) {
                    var data = allLetterData[l];

                    foreach (var form in data.GetAvailableForms()) {
                        if (data.Kind == LetterDataKind.Letter) {
                            // Overwrite
                            unicodeLookUpCache[data.GetUnicode(form)] = new UnicodeLookUpEntry(data, form);
                        } else {
                            var unicode = data.GetUnicode(form);

                            if (!unicodeLookUpCache.ContainsKey(unicode)) {
                                unicodeLookUpCache.Add(unicode, new UnicodeLookUpEntry(data, form));
                            }
                        }
                    }

                    if (data.Kind == LetterDataKind.DiacriticCombo) {
                        diacriticComboLookUpCache.Add(new DiacriticComboLookUpEntry(data.Symbol, data.BaseLetter), data);
                    }
                }
            }

            var result = new List<ArabicStringPart>();

            // If we used ArabicFixer, this char array will contain only combined unicodes
            char[] chars = processedArabicString.ToCharArray();

            int stringIndex = 0;
            for (int i = 0; i < chars.Length; i++) {
                char character = chars[i];

                // Skip spaces and arabic "?"
                if (character == ' ' || character == '؟') {
                    ++stringIndex;
                    continue;
                }

                string unicodeString = GetHexUnicodeFromChar(character);

                if (unicodeString == "0640") {
                    // it's an arabic tatweel
                    // just extends previous character
                    for (int t = result.Count - 1; t >= 0; --t) {
                        var previous = result[t];

                        if (previous.toCharacterIndex == stringIndex - 1) {
                            ++previous.toCharacterIndex;
                            result[t] = previous;
                        } else {
                            break;
                        }
                    }

                    ++stringIndex;
                    continue;
                }

                // Find the letter, and check its form
                LetterForm letterForm = LetterForm.None;
                LetterData letterData = null;

                UnicodeLookUpEntry entry;
                if (unicodeLookUpCache.TryGetValue(unicodeString, out entry)) {
                    letterForm = entry.form;
                    letterData = entry.data;
                }

                if (letterData != null) {
                    if (letterData.Kind == LetterDataKind.DiacriticCombo && separateDiacritics) {
                        // It's a diacritic combo
                        // Separate Letter and Diacritic
                        result.Add(new ArabicStringPart(staticDatabase.GetById(staticDatabase.GetLetterTable(), letterData.BaseLetter),
                            stringIndex, stringIndex, letterForm));
                        result.Add(new ArabicStringPart(staticDatabase.GetById(staticDatabase.GetLetterTable(), letterData.Symbol),
                            stringIndex, stringIndex, letterForm));
                    } else if (letterData.Kind == LetterDataKind.Symbol &&
                               letterData.Type == LetterDataType.DiacriticSymbol && !separateDiacritics) {
                        // It's a diacritic
                        // Merge Letter and Diacritic

                        var symbolId = letterData.Id;
                        var lastLetterData = result[result.Count - 1];
                        var baseLetterId = lastLetterData.letter.Id;

                        LetterData diacriticLetterData = null;
                        if (AppConfig.DisableShaddah) {
                            if (symbolId == "shaddah") {
                                diacriticLetterData = lastLetterData.letter;
                            } else {
                                diacriticComboLookUpCache.TryGetValue(new DiacriticComboLookUpEntry(symbolId, baseLetterId), out diacriticLetterData);
                            }
                        } else {
                            diacriticComboLookUpCache.TryGetValue(new DiacriticComboLookUpEntry(symbolId, baseLetterId), out diacriticLetterData);
                        }

                        if (diacriticLetterData == null) {
                            Debug.LogError("Cannot find a single character for " + baseLetterId + " + " + symbolId +
                                           ". Diacritic removed in (" + processedArabicString + ").");
                        } else {
                            var previous = result[result.Count - 1];
                            previous.letter = diacriticLetterData;
                            ++previous.toCharacterIndex;
                            result[result.Count - 1] = previous;
                        }
                    } else if (letterData.Kind == LetterDataKind.LetterVariation && separateVariations &&
                               letterData.BaseLetter == "lam") {
                        // it's a lam-alef combo
                        // Separate Lam and Alef
                        result.Add(new ArabicStringPart(staticDatabase.GetById(staticDatabase.GetLetterTable(), letterData.BaseLetter),
                            stringIndex, stringIndex, letterForm));

                        var secondPart = staticDatabase.GetById(staticDatabase.GetLetterTable(), letterData.Symbol);

                        if (secondPart.Kind == LetterDataKind.DiacriticCombo && separateDiacritics) {
                            // It's a diacritic combo
                            // Separate Letter and Diacritic
                            result.Add(new ArabicStringPart(staticDatabase.GetById(staticDatabase.GetLetterTable(), secondPart.BaseLetter),
                                stringIndex, stringIndex, letterForm));
                            result.Add(new ArabicStringPart(staticDatabase.GetById(staticDatabase.GetLetterTable(), secondPart.Symbol),
                                stringIndex, stringIndex, letterForm));
                        } else {
                            result.Add(new ArabicStringPart(secondPart, stringIndex, stringIndex, letterForm));
                        }
                    } else {
                        result.Add(new ArabicStringPart(letterData, stringIndex, stringIndex, letterForm));
                    }
                } else {
                    Debug.Log("Cannot parse letter " + character + " (" + unicodeString + ") in " + processedArabicString);
                }

                ++stringIndex;
            }

            return result;
        }

        #region Diacritic Fix

        private struct DiacriticComboEntry
        {
            public string Unicode1;
            public string Unicode2;

            public DiacriticComboEntry(string _unicode1, string _unicode2)
            {
                Unicode1 = _unicode1;
                Unicode2 = _unicode2;
            }
        }

        private static Dictionary<DiacriticComboEntry, Vector2> DiacriticCombos2Fix = null;

        /// <summary>
        /// these are manually configured positions of diacritic symbols relative to the main letter
        /// since TextMesh Pro can't manage these automatically and some letters are too tall, with the symbol overlapping 
        /// </summary>
        private static void BuildDiacriticCombos2Fix()
        {
            DiacriticCombos2Fix = new Dictionary<DiacriticComboEntry, Vector2>();

            //////// LETTER alef
            // alef_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0627", "064E"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE8E", "064E"), new Vector2(-30, 80));
            // alef_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0627", "064F"), new Vector2(-30, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE8E", "064F"), new Vector2(-50, 80));
            // alef_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0627", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE8E", "0650"), new Vector2(0, 0));

            //////// LETTER beh
            // beh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0628", "0650"), new Vector2(160, -130));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE91", "0650"), new Vector2(0, -130));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE92", "0650"), new Vector2(0, -130));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE90", "0650"), new Vector2(160, -130));
            // beh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0628", "064E"), new Vector2(150, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE91", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE92", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE90", "064E"), new Vector2(150, 0));
            // beh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0628", "0652"), new Vector2(150, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE91", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE92", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE90", "0652"), new Vector2(150, 0));
            // beh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0628", "064F"), new Vector2(130, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE91", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE92", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE90", "064F"), new Vector2(130, 0));

            //////// LETTER teh
            // teh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062A", "0652"), new Vector2(150, 30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE97", "0652"), new Vector2(0, 30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE98", "0652"), new Vector2(0, 30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE96", "0652"), new Vector2(150, 30));
            // teh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062A", "0650"), new Vector2(160, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE97", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE98", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE96", "0650"), new Vector2(160, 0));
            // teh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062A", "064E"), new Vector2(90, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE97", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE98", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE96", "064E"), new Vector2(90, 0));
            // teh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062A", "064F"), new Vector2(60, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE97", "064F"), new Vector2(-10, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE98", "064F"), new Vector2(-10, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE96", "064F"), new Vector2(60, 0));

            // teh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062A", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE97", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE98", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE96", "0651"), new Vector2(0, 0));

            //////// LETTER theh
            // theh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062B", "0650"), new Vector2(160, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9B", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9C", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9A", "0650"), new Vector2(160, 0));
            // theh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062B", "064E"), new Vector2(100, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9B", "064E"), new Vector2(-20, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9C", "064E"), new Vector2(-20, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9A", "064E"), new Vector2(60, 0));
            // theh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062B", "0652"), new Vector2(80, 70));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9B", "0652"), new Vector2(0, 90));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9C", "0652"), new Vector2(0, 90));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9A", "0652"), new Vector2(80, 70));
            // theh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062B", "064F"), new Vector2(60, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9B", "064F"), new Vector2(-30, 70));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9C", "064F"), new Vector2(-30, 70));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9A", "064F"), new Vector2(60, 40));

            // theh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062B", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9B", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9C", "0651"), new Vector2(0, 120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9A", "0651"), new Vector2(0, 0));

            //////// LETTER jeem
            // jeem_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062C", "064E"), new Vector2(30, -30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9F", "064E"), new Vector2(0, -30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA0", "064E"), new Vector2(0, -30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9E", "064E"), new Vector2(40, -30));
            // jeem_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062C", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9F", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA0", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9E", "064F"), new Vector2(0, 0));
            // jeem_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062C", "0650"), new Vector2(90, -260));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9F", "0650"), new Vector2(30, -110));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA0", "0650"), new Vector2(30, -110));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9E", "0650"), new Vector2(90, -260));
            // jeem_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062C", "0652"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9F", "0652"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA0", "0652"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9E", "0652"), new Vector2(50, 0));
            // jeem_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062C", "0651"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9F", "0651"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA0", "0651"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE9E", "0651"), new Vector2(50, 0));

            //////// LETTER hah
            // hah_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062D", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA3", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA4", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA2", "064E"), new Vector2(0, 0));
            // hah_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062D", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA3", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA4", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA2", "064F"), new Vector2(0, 0));
            // hah_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062D", "0650"), new Vector2(50, -350));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA3", "0650"), new Vector2(40, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA4", "0650"), new Vector2(40, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA2", "0650"), new Vector2(50, -350));
            // hah_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062D", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA3", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA4", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA2", "0652"), new Vector2(0, 0));
            // hah_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062D", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA3", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA4", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA2", "0651"), new Vector2(0, 0));

            //////// LETTER 7 khah
            // khah_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062E", "0652"), new Vector2(40, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA7", "0652"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA8", "0652"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA6", "0652"), new Vector2(40, 40));
            // khah_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062E", "064F"), new Vector2(40, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA7", "064F"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA8", "064F"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA6", "064F"), new Vector2(40, 40));
            // khah_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062E", "064E"), new Vector2(40, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA7", "064E"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA8", "064E"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA6", "064E"), new Vector2(40, 40));
            // khah_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062E", "0651"), new Vector2(40, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA7", "0651"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA8", "0651"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA6", "0651"), new Vector2(40, 40));
            // khah_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062E", "0650"), new Vector2(80, -260));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA7", "0650"), new Vector2(5, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA8", "0650"), new Vector2(5, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEA6", "0650"), new Vector2(80, -260));

            //////// LETTER 8 dal
            // dal_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062F", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAA", "0652"), new Vector2(0, 0));
            // dal_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062F", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAA", "0651"), new Vector2(0, 0));
            // dal_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062F", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAA", "064F"), new Vector2(0, 0));
            // dal_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062F", "0650"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAA", "0650"), new Vector2(20, 0));
            // dal_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("062F", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAA", "064E"), new Vector2(0, 0));

            //////// LETTER thal
            // thal_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0630", "064E"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAC", "064E"), new Vector2(0, 80));
            // thal_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0630", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAC", "064F"), new Vector2(0, 80));
            // thal_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0630", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAC", "0650"), new Vector2(0, 0));
            // thal_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0630", "0652"), new Vector2(20, 110));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAC", "0652"), new Vector2(20, 110));
            // thal_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0630", "0651"), new Vector2(20, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAC", "0651"), new Vector2(20, 80));

            //////// LETTER reh
            // reh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0631", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAE", "0651"), new Vector2(0, 0));
            // reh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0631", "0650"), new Vector2(50, -200));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAE", "0650"), new Vector2(50, -200));
            // reh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0631", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAE", "0652"), new Vector2(0, 0));
            // reh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0631", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAE", "064E"), new Vector2(0, 0));
            // reh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0631", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEAE", "064F"), new Vector2(0, 0));

            //////// LETTER zain
            // zain_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0632", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB0", "064E"), new Vector2(0, 0));
            // zain_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0632", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB0", "064F"), new Vector2(0, 0));
            // zain_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0632", "0650"), new Vector2(70, -180));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB0", "0650"), new Vector2(70, -180));
            // zain_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0632", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB0", "0652"), new Vector2(0, 0));
            // zain_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0632", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB0", "0651"), new Vector2(0, 0));

            //////// LETTER 12 seen
            // seen_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0633", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB3", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB4", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB2", "064F"), new Vector2(0, 0));
            // seen_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0633", "0650"), new Vector2(80, -160));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB3", "0650"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB4", "0650"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB2", "0650"), new Vector2(80, -160));
            // seen_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0633", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB3", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB4", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB2", "064E"), new Vector2(0, 0));
            // seen_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0633", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB3", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB4", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB2", "0652"), new Vector2(0, 0));
            // seen_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0633", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB3", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB4", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB2", "0651"), new Vector2(0, 0));

            //////// LETTER 13 sheen
            // sheen_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0634", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB7", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB8", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB6", "0652"), new Vector2(0, 0));
            // sheen_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0634", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB7", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB8", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB6", "0651"), new Vector2(0, 0));
            // sheen_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0634", "0650"), new Vector2(80, -160));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB7", "0650"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB8", "0650"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB6", "0650"), new Vector2(80, -160));
            // sheen_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0634", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB7", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB8", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB6", "064E"), new Vector2(0, 0));
            // sheen_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0634", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB7", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB8", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEB6", "064F"), new Vector2(0, 0));

            //////// LETTER 14 sad
            // sad_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0635", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBB", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBC", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBA", "0652"), new Vector2(0, 0));
            // sad_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0635", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBB", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBC", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBA", "0651"), new Vector2(0, 0));
            // sad_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0635", "0650"), new Vector2(80, -160));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBB", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBC", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBA", "0650"), new Vector2(80, -160));
            // sad_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0635", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBB", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBC", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBA", "064E"), new Vector2(0, 0));
            // sad_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0635", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBB", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBC", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBA", "064F"), new Vector2(0, 0));

            //////// LETTER 15 dad
            // dad_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0636", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBF", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC0", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBE", "0652"), new Vector2(0, 0));
            // dad_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0636", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBF", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC0", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBE", "0651"), new Vector2(0, 0));
            // dad_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0636", "0650"), new Vector2(80, -160));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBF", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC0", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBE", "0650"), new Vector2(80, -160));
            // dad_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0636", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBF", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC0", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBE", "064E"), new Vector2(0, 0));
            // dad_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0636", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBF", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC0", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEBE", "064F"), new Vector2(0, 0));

            //////// LETTER 16 tah
            // tah_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0637", "064E"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC3", "064E"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC4", "064E"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC2", "064E"), new Vector2(0, 100));
            // tah_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0637", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC3", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC4", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC2", "064F"), new Vector2(0, 80));
            // tah_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0637", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC3", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC4", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC2", "0651"), new Vector2(0, 100));
            // tah_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0637", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC3", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC4", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC2", "0650"), new Vector2(0, 0));
            // tah_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0637", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC3", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC4", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC2", "0652"), new Vector2(0, 100));

            //////// LETTER 17 zah
            // zah_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0638", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC7", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC8", "0652"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC6", "0652"), new Vector2(0, 100));
            // zah_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0638", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC7", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC8", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC6", "064F"), new Vector2(0, 80));
            // zah_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0638", "064E"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC7", "064E"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC8", "064E"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC6", "064E"), new Vector2(0, 80));
            // zah_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0638", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC7", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC8", "0651"), new Vector2(0, 100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC6", "0651"), new Vector2(0, 100));
            // zah_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0638", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC7", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC8", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEC6", "0650"), new Vector2(0, 0));

            //////// LETTER 18ain
            // ain_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0639", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECB", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECC", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECA", "064E"), new Vector2(0, 0));
            // ain_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0639", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECB", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECC", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECA", "064F"), new Vector2(0, 0));
            // ain_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0639", "0650"), new Vector2(20, -300));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECB", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECC", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECA", "0650"), new Vector2(20, -300));
            // ain_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0639", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECB", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECC", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECA", "0652"), new Vector2(0, 0));
            // ain_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0639", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECB", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECC", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECA", "0651"), new Vector2(0, 0));

            //////// LETTER 19 ghain
            // ghain_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("063A", "0651"), new Vector2(40, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECF", "0651"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED0", "0651"), new Vector2(20, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECE", "0651"), new Vector2(40, 20));
            // ghain_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("063A", "0650"), new Vector2(70, -250));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECF", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED0", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECE", "0650"), new Vector2(50, -250));
            // ghain_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("063A", "0652"), new Vector2(40, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECF", "0652"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED0", "0652"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECE", "0652"), new Vector2(40, 40));
            // ghain_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("063A", "064E"), new Vector2(0, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECF", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED0", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECE", "064E"), new Vector2(0, 0));
            // ghain_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("063A", "064F"), new Vector2(0, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECF", "064F"), new Vector2(0, 10));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED0", "064F"), new Vector2(0, 10));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FECE", "064F"), new Vector2(20, 10));

            //////// LETTER 20 feh
            // feh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0641", "064F"), new Vector2(100, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED3", "064F"), new Vector2(0, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED4", "064F"), new Vector2(0, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED2", "064F"), new Vector2(100, 0));
            // feh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0641", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED3", "064E"), new Vector2(200, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED4", "064E"), new Vector2(200, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED2", "064E"), new Vector2(0, 0));
            // feh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0641", "0651"), new Vector2(80, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED3", "0651"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED4", "0651"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED2", "0651"), new Vector2(80, 80));
            // feh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0641", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED3", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED4", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED2", "0650"), new Vector2(0, 0));
            // feh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0641", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED3", "0652"), new Vector2(200, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED4", "0652"), new Vector2(200, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED2", "0652"), new Vector2(0, 0));

            //////// LETTER 21 qaf
            // qaf_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0642", "0651"), new Vector2(90, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED7", "0651"), new Vector2(0, 90));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED8", "0651"), new Vector2(0, 90));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED6", "0651"), new Vector2(90, 40));
            // qaf_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0642", "0650"), new Vector2(50, -140));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED7", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED8", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED6", "0650"), new Vector2(50, -140));
            // qaf_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0642", "0652"), new Vector2(80, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED7", "0652"), new Vector2(0, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED8", "0652"), new Vector2(0, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED6", "0652"), new Vector2(80, 60));
            // qaf_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0642", "064E"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED7", "064E"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED8", "064E"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED6", "064E"), new Vector2(50, 0));
            // qaf_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0642", "064F"), new Vector2(80, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED7", "064F"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED8", "064F"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FED6", "064F"), new Vector2(80, 20));

            //////// LETTER 22 kaf
            // kaf_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0643", "064F"), new Vector2(40, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDB", "064F"), new Vector2(0, 30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDC", "064F"), new Vector2(0, 30));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDA", "064F"), new Vector2(40, 0));
            // kaf_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0643", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDB", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDC", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDA", "064E"), new Vector2(0, 0));
            // kaf_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0643", "0650"), new Vector2(60, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDB", "0650"), new Vector2(20, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDC", "0650"), new Vector2(30, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDA", "0650"), new Vector2(60, 0));
            // kaf_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0643", "0651"), new Vector2(50, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDB", "0651"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDC", "0651"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDA", "0651"), new Vector2(50, 20));
            // kaf_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0643", "0652"), new Vector2(50, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDB", "0652"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDC", "0652"), new Vector2(20, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDA", "0652"), new Vector2(50, 20));

            //////// LETTER 23 lam
            // lam_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0644", "0650"), new Vector2(0, -100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDF", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE0", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDE", "0650"), new Vector2(0, -100));
            // lam_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0644", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDF", "064E"), new Vector2(40, 120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE0", "064E"), new Vector2(40, 120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDE", "064E"), new Vector2(0, 0));
            // lam_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0644", "0651"), new Vector2(40, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDF", "0651"), new Vector2(0, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE0", "0651"), new Vector2(0, 60));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDE", "0651"), new Vector2(40, 40));
            // lam_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0644", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDF", "0652"), new Vector2(60, 140));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE0", "0652"), new Vector2(60, 140));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDE", "0652"), new Vector2(0, 0));
            // lam_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0644", "064F"), new Vector2(80, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDF", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE0", "064F"), new Vector2(0, 80));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEDE", "064F"), new Vector2(80, 40));

            //////// LETTER 24 meem
            // meem_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0645", "0650"), new Vector2(60, -100));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE3", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE4", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE2", "0650"), new Vector2(60, -100));
            // meem_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0645", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE3", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE4", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE2", "064E"), new Vector2(0, 0));
            // meem_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0645", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE3", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE4", "0651"), new Vector2(0, 120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE2", "0651"), new Vector2(0, 0));
            // meem_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0645", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE3", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE4", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE2", "0652"), new Vector2(0, 0));
            // meem_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0645", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE3", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE4", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE2", "064F"), new Vector2(0, 0));

            //////// LETTER 25 noon
            // noon_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0646", "064F"), new Vector2(30, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE7", "064F"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE8", "064F"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE6", "064F"), new Vector2(40, 0));
            // noon_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0646", "0650"), new Vector2(60, -150));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE7", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE8", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE6", "0650"), new Vector2(60, -150));
            // noon_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0646", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE7", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE8", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE6", "064E"), new Vector2(0, 0));
            // noon_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0646", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE7", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE8", "0651"), new Vector2(0, 40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE6", "0651"), new Vector2(0, 0));
            // noon_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0646", "0652"), new Vector2(50, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE7", "0652"), new Vector2(30, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE8", "0652"), new Vector2(30, 50));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEE6", "0652"), new Vector2(50, 0));

            //////// LETTER heh
            // heh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0647", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEB", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEC", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEA", "064E"), new Vector2(0, 0));
            // heh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0647", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEB", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEC", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEA", "064F"), new Vector2(0, 0));
            // heh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0647", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEB", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEC", "0650"), new Vector2(0, -120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEA", "0650"), new Vector2(0, 0));
            // heh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0647", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEB", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEC", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEA", "0652"), new Vector2(0, 0));
            // heh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0647", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEB", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEC", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEA", "0651"), new Vector2(0, 0));

            //////// LETTER waw
            // waw_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0648", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEE", "064E"), new Vector2(0, 0));
            // waw_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0648", "0650"), new Vector2(60, -140));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEE", "0650"), new Vector2(60, -140));
            // waw_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0648", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEE", "0652"), new Vector2(0, 0));
            // waw_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0648", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEE", "0651"), new Vector2(0, 0));
            // waw_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0648", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEEE", "064F"), new Vector2(0, 0));

            //////// LETTER yeh
            // yeh_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064A", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF3", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF4", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF2", "064E"), new Vector2(0, 0));
            // yeh_shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064A", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF3", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF4", "0651"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF2", "0651"), new Vector2(0, 0));
            // yeh_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064A", "0650"), new Vector2(140, -230));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF3", "0650"), new Vector2(0, -120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF4", "0650"), new Vector2(0, -120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF2", "0650"), new Vector2(120, -260));
            // yeh_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064A", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF3", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF4", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF2", "064F"), new Vector2(0, 0));
            // yeh_sukun
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064A", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF3", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF4", "0652"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF2", "0652"), new Vector2(0, 0));

            //////// LETTER alef_hamza_hi
            // alef_hamza_hi_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0623", "064E"), new Vector2(0, 200));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE84", "064E"), new Vector2(-20, 200));
            // alef_hamza_hi_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0623", "064F"), new Vector2(-20, 130));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE84", "064F"), new Vector2(-20, 130));

            //////// LETTER alef_hamza_low
            // alef_hamza_low_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0625", "0650"), new Vector2(0, -120));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE88", "0650"), new Vector2(0, -120));

            //////// LETTER lam_alef_kasrah
            // lam_alef_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF5", "0650"), new Vector2(120, -40));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEF6", "0650"), new Vector2(100, 0));

            //////// LETTER teh_marbuta
            // teh_marbuta_dammah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0629", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("", "064F"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE94", "064F"), new Vector2(0, 0));
            // teh_marbuta_kasrah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0629", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("", "0650"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE94", "0650"), new Vector2(0, 0));
            // teh_marbuta_fathah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0629", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("", "064E"), new Vector2(0, 0));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FE94", "064E"), new Vector2(0, 0));

            //////// SYMBOL shaddah
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("0650", "0651"), new Vector2(0, 20));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("064E", "0651"), new Vector2(0, 160));
            DiacriticCombos2Fix.Add(new DiacriticComboEntry("FEFC", "0651"), new Vector2(20, 130));
        }

        private static Vector2 FindDiacriticCombo2Fix(string Unicode1, string Unicode2)
        {
            if (DiacriticCombos2Fix == null) { BuildDiacriticCombos2Fix(); }

            Vector2 newDelta = new Vector2(0, 0);
            DiacriticCombos2Fix.TryGetValue(new DiacriticComboEntry(Unicode1, Unicode2), out newDelta);
            return newDelta;
        }

        /// <summary>
        /// Adjusts the diacritic positions.
        /// </summary>
        /// <returns><c>true</c>, if diacritic positions was adjusted, <c>false</c> otherwise.</returns>
        /// <param name="textInfo">Text info.</param>
        public static bool FixTMProDiacriticPositions(TMPro.TMP_TextInfo textInfo)
        {
            //Debug.Log("FixDiacriticPositions " + textInfo.characterCount);
            int characterCount = textInfo.characterCount;
            bool changed = false;

            if (characterCount > 1) {
                // output unicodes for DiacriticCombos2Fix
                //string combo = "";
                //for (int i = 0; i < characterCount; i++) {
                //    combo += '"' + ArabicAlphabetHelper.GetHexUnicodeFromChar(textInfo.characterInfo[i].character) + '"' + ',';
                //}
                //Debug.Log("DiacriticCombos2Fix.Add(new DiacriticComboEntry(" + combo + " 0, 0));");

                Vector2 modificationDelta = new Vector2(0, 0);
                for (int charPosition = 0; charPosition < characterCount - 1; charPosition++) {
                    modificationDelta = FindDiacriticCombo2Fix(
                        GetHexUnicodeFromChar(textInfo.characterInfo[charPosition].character),
                        GetHexUnicodeFromChar(textInfo.characterInfo[charPosition + 1].character)
                    );

                    if (modificationDelta.sqrMagnitude > 0f) {
                        changed = true;
                        int materialIndex = textInfo.characterInfo[charPosition + 1].materialReferenceIndex;
                        int vertexIndex = textInfo.characterInfo[charPosition + 1].vertexIndex;
                        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                        float charsize = (sourceVertices[vertexIndex + 2].y - sourceVertices[vertexIndex + 0].y);
                        float dx = charsize * modificationDelta.x / 100f;
                        float dy = charsize * modificationDelta.y / 100f;
                        Vector3 offset = new Vector3(dx, dy, 0f);

                        Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
                        destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] + offset;
                        destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] + offset;
                        destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] + offset;
                        destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] + offset;

                        //Debug.Log("DIACRITIC FIX: "
                        //          + ArabicAlphabetHelper.GetHexUnicodeFromChar(textInfo.characterInfo[charPosition].character)
                        //          + " + "
                        //          + ArabicAlphabetHelper.GetHexUnicodeFromChar(textInfo.characterInfo[charPosition + 1].character)
                        //          + " by " + modificationDelta);
                    }
                }
            }
            return changed;
        }

        public static string DebugShowDiacriticFix(string unicode1, string unicode2)
        {
            var delta = FindDiacriticCombo2Fix(unicode1, unicode2);
            return string.Format("DiacriticCombos2Fix.Add(new DiacriticComboEntry(\"{0}\", \"{1}\"), new Vector2({2}, {3}));", unicode1, unicode2, delta.x, delta.y);
        }
        #endregion
    }
}