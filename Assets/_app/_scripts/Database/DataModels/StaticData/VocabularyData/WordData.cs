﻿using System;
using System.Linq;
using Antura.LivingLetters;
using Antura.Helpers;
using SQLite;
using UnityEngine;

namespace Antura.Database
{
    /// <summary>
    /// Data defining a Word.
    /// This is one of the fundamental dictionary (i.e. learning content) elements.
    /// <seealso cref="PhraseData"/>
    /// <seealso cref="LetterData"/>
    /// </summary>
    [Serializable]
    public class WordData : IVocabularyData, IConvertibleToLivingLetterData
    {
        [PrimaryKey]
        public string Id
        {
            get { return _Id; }
            set { _Id = value; }
        }
        [SerializeField]
        private string _Id;

        public bool Active
        {
            get { return _Active; }
            set { _Active = value; }
        }
        [SerializeField]
        private bool _Active;

        public WordDataKind Kind
        {
            get { return _Kind; }
            set { _Kind = value; }
        }
        [SerializeField]
        private WordDataKind _Kind;

        public WordDataCategory Category
        {
            get { return _Category; }
            set { _Category = value; }
        }
        [SerializeField]
        private WordDataCategory _Category;

        public WordDataForm Form
        {
            get { return _Form; }
            set { _Form = value; }
        }
        [SerializeField]
        private WordDataForm _Form;

        public WordDataArticle Article
        {
            get { return _Article; }
            set { _Article = value; }
        }
        [SerializeField]
        private WordDataArticle _Article;

        public VocabularyDataGender Gender
        {
            get { return _Gender; }
            set { _Gender = value; }
        }
        [SerializeField]
        private VocabularyDataGender _Gender;

        public string LinkedWord
        {
            get { return _LinkedWord; }
            set { _LinkedWord = value; }
        }
        [SerializeField]
        private string _LinkedWord;

        public string Arabic
        {
            get { return _Arabic; }
            set { _Arabic = value; }
        }
        [SerializeField]
        private string _Arabic;

        public string ArabicNoShaddah
        {
            get { return _ArabicNoShaddah; }
            set { _ArabicNoShaddah = value; }
        }
        [SerializeField]
        private string _ArabicNoShaddah;

        public string Value
        {
            get { return _Value; }
            set { _Value = value; }
        }
        [SerializeField]
        private string _Value;

        [Ignore]
        public string[] Letters
        {
            get { return _Letters; }
            set { _Letters = value; }
        }
        [SerializeField]
        private string[] _Letters;
        public string Letters_list
        {
            get { return _Letters.ToJoinedString(); }
            set { }
        }

        public string Drawing
        {
            get { return _Drawing; }
            set { _Drawing = value; }
        }
        [SerializeField]
        private string _Drawing;

        public float Complexity
        {
            get { return _Complexity; }
            set { _Complexity = value; }
        }
        [SerializeField]
        private float _Complexity;

        //public LetterSymbol[] Symbols; //TODO

        public int NumberOfLetters { get { return Letters.Length; } }

        public string GetId()
        {
            return Id;
        }

        public float GetIntrinsicDifficulty()
        {
            return Complexity;
        }

        public override string ToString()
        {
            string s = Id + ": " + Arabic;
            s += "  ";
            foreach (var letter in Letters) s += letter + ", ";
            return s;
        }

        public ILivingLetterData ConvertToLivingLetterData()
        {
            return new LL_WordData(GetId(), this);
        }

        public bool HasDrawing()
        {
            return Drawing != "";
        }

    }
}