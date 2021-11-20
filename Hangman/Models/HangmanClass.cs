using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hangman.Models
{
    public class HangmanClass
    {
        public string playerInput { get; set; }
        public string wordToGuess { get; set; }
        public string playerGuess { get; set; }
        public int incorrectAnswerCount { get; set; }
        public List<char> usedLetters { get; set; }
        public List<char> correctLetters { get; set; }
        public List<char> incorrectLetters { get; set; }
        public HangmanModel currentState { get; set; }
        public List<HangmanModel> hangmanModels { get; set; }
        public bool isPlaying = false;
        public bool? hasWon = null;
    }

    public class HangmanModel
    {
        public int IncorrectCount { get; set; }
        public string Image { get; set; }
    }
}