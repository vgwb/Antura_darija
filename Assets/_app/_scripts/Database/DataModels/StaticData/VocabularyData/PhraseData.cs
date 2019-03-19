﻿using Antura.Helpers;
using Antura.LivingLetters;
using System;
using SQLite;
using UnityEngine;

namespace Antura.Database
{
    /// <summary>
    /// Data defining a Phrase.
    /// This is one of the fundamental dictionary (i.e. learning content) elements.
    /// <seealso cref="WordData"/>
    /// <seealso cref="LetterData"/>
    /// </summary>
    [Serializable]
    public class PhraseData : IVocabularyData, IConvertibleToLivingLetterData
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

        public string English
        {
            get { return _English; }
            set { _English = value; }
        }
        [SerializeField]
        private string _English;

        public string Arabic
        {
            get { return _Arabic; }
            set { _Arabic = value; }
        }
        [SerializeField]
        private string _Arabic;

        public PhraseDataCategory Category
        {
            get { return _Category; }
            set { _Category = value; }
        }
        [SerializeField]
        private PhraseDataCategory _Category;

        public string Linked
        {
            get { return _Linked; }
            set { _Linked = value; }
        }
        [SerializeField]
        private string _Linked;

        [Ignore]
        public string[] Words
        {
            get { return _Words; }
            set { _Words = value; }
        }
        [SerializeField]
        private string[] _Words;
        public string Words_list
        {
            get { return _Words.ToJoinedString(); }
            set { }
        }

        [Ignore]
        public string[] Answers
        {
            get { return _Answers; }
            set { _Answers = value; }
        }
        [SerializeField]
        private string[] _Answers;
        public string Answers_list
        {
            get { return _Answers.ToJoinedString(); }
            set { }
        }

        public float Complexity
        {
            get { return _Complexity; }
            set { _Complexity = value; }
        }
        [SerializeField]
        private float _Complexity;

        public float GetIntrinsicDifficulty()
        {
            return Complexity;
        }

        public override string ToString()
        {
            return Id + ": " + English;
        }

        public string GetId()
        {
            return Id;
        }

        public ILivingLetterData ConvertToLivingLetterData()
        {
            return new LL_PhraseData(GetId(), this);
        }
    }
}