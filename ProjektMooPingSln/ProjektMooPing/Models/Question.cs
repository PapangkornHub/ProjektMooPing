using System;
using System.Collections.Generic;
using System.Text;

namespace ProjektMooPing.Models
{
    public class QuestionData
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string QuestionTh { get; set; }
        public List<string> Choices { get; set; }
        public List<string> ChoicesTh { get; set; }
        public int CorrectIndex { get; set; }
        public string Trivia { get; set; }
        public string TriviaTh { get; set; }

        private static ProjektMooPing.Services.LocalizationService Loc
            => ProjektMooPing.Services.LocalizationService.Instance;

        /// <summary>คำถามตามภาษาที่เลือก</summary>
        public string DisplayQuestion =>
            Loc.IsThai && !string.IsNullOrEmpty(QuestionTh) ? QuestionTh : Question;

        /// <summary>ตัวเลือกตามภาษาที่เลือก</summary>
        public List<string> DisplayChoices =>
            Loc.IsThai && ChoicesTh != null && ChoicesTh.Count > 0 ? ChoicesTh : Choices;

        /// <summary>Trivia ตามภาษาที่เลือก</summary>
        public string DisplayTrivia =>
            Loc.IsThai && !string.IsNullOrEmpty(TriviaTh) ? TriviaTh : Trivia;
    }
}