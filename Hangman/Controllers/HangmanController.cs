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
        public bool isPlaying = false;
        public bool hasLost = false;
        public HangmanClass hangman = new HangmanClass();


        // GET: Hangman
        public ActionResult Index()
        {
            return View("NewGame");
        }

        public ActionResult NewGame(){
            if ( isPlaying == false )
            {
               var task = Task.Run( () => InitiateGame() );
                Task newTask = task;
                newTask.Wait();
            }

            return View( "Game", hangman );
        }

        // Gets a random word from the API and load it into our Hangman model
        private async Task InitiateGame()
        {
            string apiUrl = "https://random-word-api.herokuapp.com/word?number=1&swear=0";
            HttpResponseMessage response = await _client.GetAsync(apiUrl);
            if ( response.IsSuccessStatusCode )
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                hangman.wordToGuess = FormatWord( bytes );
                hangman.playerGuess = FormatGuess( hangman.wordToGuess );
                hangman.hangmanModels = SetupModel();
                hangman.incorrectAnswerCount = 0;
                hangman.currentState = GetCurrentState(hangman);
                isPlaying = true;
            }
            else
            {
                // todo:
                // failed to initate game
                // throw error message
            }
        }

       private void ResetState()
        {
            ModelState.Remove( "playerGuess" );
            ModelState.Remove( "playerInput" );
            ModelState.Remove( "usedLetters" );
            ModelState.Remove( "incorrectAnswerCount" );
        }

        private HangmanModel GetCurrentState(HangmanClass hangman)
        {
            return hangman.hangmanModels[hangman.incorrectAnswerCount];
        }

        [HttpPost]
        public ActionResult PlayerInput( HangmanClass hangman )
        {
            if ( ModelState.IsValid )
            {
                var playerInput = hangman.playerInput;
                if ( !string.IsNullOrWhiteSpace( playerInput ) )
                {
                    char inputChar = playerInput[0];

                    bool isValid = ValidatePlayerInput( inputChar );
                    if ( isValid )
                    {
                        bool isNewLetter = CheckLetter( inputChar );
                        if ( isNewLetter )
                        {
                            if( hangman.usedLetters == null )
                            {
                                var initiateList = new List<char>();
                                initiateList.Add( inputChar );
                                hangman.usedLetters = initiateList;
                            }
                            else
                            {
                                hangman.usedLetters.Add( inputChar );
                            }
                            var playerGuess = hangman.playerGuess;
                            hangman = doesWordContainInput( hangman );
                            if ( hangman.playerGuess == playerGuess )
                            {
                                hangman.incorrectAnswerCount++;
                            }
                            // correct answer
                        }
                        else
                        {
                            //already used letter message
                        }
                    }
                    else
                    {
                        // incorrect letter message
                    }
                }
            }
            hangman.hangmanModels = SetupModel();
            hangman.currentState = GetCurrentState( hangman );
            ResetState();
            return View( "Game", hangman );
        }

        private bool CheckLetter(char input)
        {
            if( hangman.usedLetters == null )
            {
                return true;
            }
            if( hangman.usedLetters.Contains( input ) )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public HangmanClass doesWordContainInput( HangmanClass hangman)
        {
            char input = hangman.playerInput[0];
            List<char> letters = hangman.wordToGuess.ToList();
            List<char> revealedLetters = hangman.playerGuess.ToList();

            for ( int i = 0; i < letters.Count; i++ )
            {
                if( letters[i] == input )
                {
                    revealedLetters[i] = input;
                }
            }
            hangman.playerGuess = new string( revealedLetters.ToArray() );
            return hangman;
        }

        private bool ValidatePlayerInput( char input)
        {
            return char.IsLetter(input);
        }

        // Creates an underscore for every letter in the word
        private string FormatGuess( string wordToGuess )
        {
            string guess = "";
            foreach( char letter in wordToGuess )
            {
                guess += "_";
            }
            return guess;
        }

        // bytes: the random word received from API call
        // Strips the array characters off of the API return
        private string FormatWord(Byte[] bytes)
        {
            var wordTrimmed = System.Text.Encoding.Default.GetString(bytes).Remove(0,2);
            var wordLength = wordTrimmed.Length - 2;
            return wordTrimmed.Substring(0, wordLength);
        }

        private List<HangmanModel> SetupModel()
        {
            var models = new List<HangmanModel>();
            models.Add( new HangmanModel
            {
                IncorrectCount = 0 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_dim__2tELn' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_dim__2tELn' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 1 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_dim__2tELn' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 2 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_on__3Tbdc' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 3 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_dim__2tELn' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_on__3Tbdc' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 4 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_dim__2tELn' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_on__3Tbdc' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 5 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_dim__2tELn' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_on__3Tbdc' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='undefined noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='undefined noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='undefined noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='undefined noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='undefined noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='undefined noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            models.Add( new HangmanModel
            {
                IncorrectCount = 6 ,
                Image = "<svg class='noose_parent__3UpaV ' x='0px' y='0px' viewBox='0 0 236 330'><line class='noose_on__3Tbdc' x1='0' y1='330' x2='236' y2='330'></line><line class='noose_on__3Tbdc' x1='59' y1='0' x2='59' y2='330'></line><line class='noose_on__3Tbdc' x1='139.4' y1='53.5' x2='139.4' y2='0'></line><line class='noose_on__3Tbdc' x1='58' y1='2.5' x2='139.4' y2='2.5'></line><line class='noose_on__3Tbdc' x1='139.4' y1='133.9' x2='121.6' y2='212.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='134.3' x2='154.2' y2='210.8'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='167.5' y2='291.4'></line><line class='noose_on__3Tbdc' x1='139.4' y1='218.7' x2='114.2' y2='299.3'></line><line class='noose_on__3Tbdc' x1='139.4' y1='124.7' x2='139.4' y2='218.7'></line><circle class='noose_on__3Tbdc' cx='139.4' cy='89.1' r='35.6'></circle><line class='noose_on__3Tbdc noose_face__340qe' x1='122.9' y1='83.4' x2='131.1' y2='91.6'></line><line class='noose_on__3Tbdc noose_face__340qe' x1='122.6' y1='91.9' x2='131.6' y2='82.9'></line><line class='noose_on__3Tbdc noose_face__340qe' x1='147' y1='83.5' x2='155.3' y2='91.8'></line><line class='noose_on__3Tbdc noose_face__340qe' x1='146.8' y1='92' x2='155.8' y2='83'></line><path class='noose_on__3Tbdc noose_face__340qe' d='M126.4,106.5c0-1.8,5.6-3.2,12.4-3.2'></path><path class='noose_on__3Tbdc noose_face__340qe' d='M150.9,106.5c0-1.8-5.6-3.2-12.4-3.2'></path><circle class='' cx='167' cy='119.9' r='35.6'></circle><line class='' x1='167' y1='169' x2='231.8' y2='96.7'></line><line class='' x1='167' y1='155.5' x2='167' y2='249.4'></line><line class='' x1='167' y1='249.4' x2='180.4' y2='330.5'></line><line class='' x1='167' y1='249.4' x2='142.4' y2='330.5'></line><line class='' x1='167' y1='169' x2='92.8' y2='106.4'></line><path class='' d='M151.8,133.7c0,4.2,6.9,7.7,15.4,7.7'></path><path class='' d='M182.2,133.7c0,4.2-6.9,7.7-15.4,7.7'></path><line class='' x1='151' y1='118.1' x2='161.1' y2='118.1'></line><line class='' x1='173.1' y1='118.1' x2='183.2' y2='118.1'></line></svg>"
            } );
            return models;
        }

    }
}