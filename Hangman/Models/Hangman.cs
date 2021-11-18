using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Hangman.Models
{
    public class Hangman
    {
        public string wordToGuess { get; set; }
        public string playerGuess { get; set; }
        public int? correctAnswerCount { get; set; }
        public int? incorrectAnswerCount { get; set; }
        public string[] usedLetters { get; set; }
        public string[] availableLetters { get; set; }
    }
}