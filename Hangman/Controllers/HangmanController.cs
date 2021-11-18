using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Hangman.Models;
namespace Hangman.Controllers
{
    public class HangmanController : Controller
    {
        private static HttpClient _client = new HttpClient();
        

        // GET: Hangman
        public ActionResult Index()
        {
            return View("StartGame");
        }

        public async Task<ActionResult> Start()
        {
            var hangman = new Models.Hangman();
            string apiUrl = "https://random-word-api.herokuapp.com/word?number=1&swear=0";
            HttpResponseMessage response = await _client.GetAsync(apiUrl);
            if ( response.IsSuccessStatusCode )
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                hangman.wordToGuess = FormatWord( bytes );
                hangman.playerGuess = FormatGuess( hangman.wordToGuess );
            }
            return View( "Game", hangman );
        }

        private string FormatGuess( string wordToGuess )
        {
            string guess = "";
            foreach( char letter in wordToGuess )
            {
                guess += "_";
            }
            return guess;
        }

        private string FormatWord(Byte[] bytes)
        {
            var wordTrimmed = System.Text.Encoding.Default.GetString(bytes).Remove(0,2);
            var wordLength = wordTrimmed.Length - 2;
            return wordTrimmed.Substring(0, wordLength);
        }

    }
}