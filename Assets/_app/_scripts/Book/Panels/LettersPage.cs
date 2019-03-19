﻿using Antura.Audio;
using Antura.Core;
using Antura.Database;
using Antura.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Antura.Book
{
    public class LettersPage : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject LetterItemPrefab;
        public GameObject DiacriticSymbolItemPrefab;

        [Header("References")]
        public GameObject DetailPanel;
        public GameObject MainLetterPanel;
        public GameObject ListPanel;
        public GameObject ListContainer;

        public LetterAllForms MainLetterDisplay;
        public GameObject DiacriticsContainer;

        private LetterInfo myLetterInfo;
        private LetterData myLetterData;
        private GameObject btnGO;

        #region Letters

        private void OnEnable()
        {
            LettersPanel();
        }

        private void LettersPanel()
        {
            ListPanel.SetActive(true);
            DetailPanel.SetActive(false);
            emptyContainer(ListContainer);

            List<LetterData> letters = AppManager.I.DB.FindLetterData((x) => (x.Kind == LetterDataKind.Letter && x.InBook));
            letters.Sort((x, y) => x.Number.CompareTo(y.Number));

            //adds Letter VAriations
            List<LetterData> lettersVariations = AppManager.I.DB.FindLetterData((x) => (x.Kind == LetterDataKind.LetterVariation && x.InBook));
            lettersVariations.Sort((x, y) => x.Id.CompareTo(y.Id));
            letters.AddRange(lettersVariations);

            //adds Symbols
            List<LetterData> symbols = AppManager.I.DB.FindLetterData((x) => (x.Kind == LetterDataKind.Symbol && x.InBook));
            symbols.Sort((x, y) => x.Id.CompareTo(y.Id));
            letters.AddRange(symbols);

            List<LetterInfo> allLetterInfos = AppManager.I.ScoreHelper.GetAllLetterInfo();
            foreach (var letter in letters) {
                LetterInfo myLetterinfo = allLetterInfos.Find(value => value.data == letter);

                btnGO = Instantiate(LetterItemPrefab);
                btnGO.transform.SetParent(ListContainer.transform, false);
                btnGO.transform.SetAsFirstSibling();
                btnGO.GetComponent<ItemLetter>().Init(this, myLetterinfo, false);
            }

            MainLetterPanel.GetComponent<Button>().onClick.AddListener(BtnClickMainLetterPanel);
        }

        #endregion
        public void DetailLetter(LetterInfo letterInfo)
        {
            DetailPanel.SetActive(true);
            myLetterInfo = letterInfo;
            myLetterData = letterInfo.data;
            //string debug_output = "//////// LETTER " + myLetterData.Number + " " + myLetterData.Id + "\n";
            HighlightLetterItem(myLetterInfo.data.Id);

            emptyContainer(DiacriticsContainer);

            var letterbase = myLetterInfo.data.Id;
            var variationsletters = AppManager.I.DB.FindLetterData(
                (x) => (x.BaseLetter == letterbase && (x.Kind == LetterDataKind.DiacriticCombo && x.Active))
            );

            // diacritics box
            var letterGO = Instantiate(DiacriticSymbolItemPrefab);
            letterGO.transform.SetParent(DiacriticsContainer.transform, false);
            letterGO.GetComponent<ItemDiacriticSymbol>().Init(this, myLetterInfo, true);

            List<LetterInfo> info_list = AppManager.I.ScoreHelper.GetAllLetterInfo();
            info_list.Sort((x, y) => x.data.Id.CompareTo(y.data.Id));
            foreach (var info_item in info_list) {
                if (variationsletters.Contains(info_item.data)) {
                    if (AppConfig.DisableShaddah && info_item.data.Symbol == "shaddah") {
                        continue;
                    }
                    btnGO = Instantiate(DiacriticSymbolItemPrefab);
                    btnGO.transform.SetParent(DiacriticsContainer.transform, false);
                    btnGO.GetComponent<ItemDiacriticSymbol>().Init(this, info_item, false);
                    //debug_output += info_item.data.GetDebugDiacriticFix();
                }
            }
            //Debug.Log(debug_output);
            ShowLetter(myLetterInfo);
        }

        private void ShowLetter(LetterInfo letterInfo)
        {
            myLetterInfo = letterInfo;
            myLetterData = letterInfo.data;

            // Debug.Log("ShowLetter " + myLetterData.Id);

            string positionsString = "";
            foreach (var p in letterInfo.data.GetAvailableForms()) {
                positionsString = positionsString + " " + p;
            }
            MainLetterDisplay.Init(myLetterData);
            //LetterScoreText.text = "Score: " + myLetterInfo.score;

            HighlightDiacriticItem(myLetterData.Id);
            playSound();

            // Debug.Log(myLetterData.GetDebugDiacriticFix());
        }

        private void BtnClickMainLetterPanel()
        {
            AudioManager.I.PlayLetter(myLetterData, true, LetterDataSoundType.Phoneme);
        }

        private void playSound()
        {
            if (myLetterData.Kind == LetterDataKind.DiacriticCombo) {
                AudioManager.I.PlayLetter(myLetterData, true, LetterDataSoundType.Phoneme);
            } else {
                AudioManager.I.PlayLetter(myLetterData, true, LetterDataSoundType.Name);
            }
        }

        public void ShowDiacriticCombo(LetterInfo newLetterInfo)
        {
            ShowLetter(newLetterInfo);
        }

        private void HighlightLetterItem(string id)
        {
            foreach (Transform t in ListContainer.transform) {
                t.GetComponent<ItemLetter>().Select(id);
            }
        }

        private void HighlightDiacriticItem(string id)
        {
            foreach (Transform t in DiacriticsContainer.transform) {
                t.GetComponent<ItemDiacriticSymbol>().Select(id);
            }
        }

        private void emptyContainer(GameObject container)
        {
            foreach (Transform t in container.transform) {
                Destroy(t.gameObject);
            }
            // reset vertical position
            //ListPanel.GetComponent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 1.0f;
        }
    }
}