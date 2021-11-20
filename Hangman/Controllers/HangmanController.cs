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
        public HangmanClass hangman = new HangmanClass();


        // GET: Hangman
        public ActionResult Index()
        {
            return View("NewGame");
        }

        public ActionResult NewGame()
        {
            TempData.Clear();
            var task = Task.Run( () => InitiateGame() );
            Task newTask = task;
            newTask.Wait();
            ViewBag.Message = "Press any letter key to make a guess!";
            ViewBag.IsPlaying = true;
            return View( "Game" );
        }

        public ActionResult NextGame( bool? HasWon )
        {
            if ( HasWon == null )
            {

                bool? hasWon = (bool?)TempData["HasWon"];
                if ( hasWon == null )
                {
                    var hangman = (HangmanClass)TempData["Hangman"];
                    var models = SetupModel();
                    int incorrectAnswerCount = hangman.incorrectAnswerCount;

                    ViewBag.HasWon = hangman.hasWon;
                    ViewBag.WordToGuess = hangman.wordToGuess;
                    ViewBag.CurrentState = models.ElementAt( incorrectAnswerCount ).Image;
                    ViewBag.IncorrectLetters = string.Join( ", " , hangman.incorrectLetters );
                    TempData["Hangman"] = hangman;
                }
                else
                {
                    ViewBag.HasWon = ( bool ) TempData["HasWon"];
                    ViewBag.WordToGuess = TempData["WordToGuess"].ToString();
                    ViewBag.CurrentState = TempData["CurrentState"].ToString();
                    ViewBag.IncorrectLetters = TempData["IncorrectLetters"].ToString();
                }
                return View( "Game" , hangman );
            }
            else
            {
                return View( "NewGame" );
            }
        }

        /// <summary>
        /// Gets a random word from an API
        /// Setup Game by configuring model
        /// </summary>
        /// <returns></returns>
        private async Task InitiateGame()
        {
            
            string apiUrl = "https://random-word-api.herokuapp.com/word?number=1&swear=0";
            HttpResponseMessage response = await _client.GetAsync(apiUrl);
            if ( response.IsSuccessStatusCode )
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                hangman.wordToGuess = FormatWord( bytes );
                hangman.playerGuess = FormatGuess( hangman.wordToGuess );

                ViewBag.WordToGuess = hangman.playerGuess;
                hangman.hangmanModels = SetupModel();
                hangman.incorrectLetters = new List<char>();
                hangman.correctLetters = new List<char>();
                hangman.usedLetters = new List<char>();
                ViewBag.CurrentState = hangman.hangmanModels.ElementAt( 0 ).Image;
                hangman.incorrectAnswerCount = 0;
                hangman.currentState = hangman.hangmanModels.ElementAt(0);
                hangman.isPlaying = true;
                TempData["Hangman"] = hangman;
            }
            else
            {
                // todo:
                // failed to initate game
                // throw error message
                ViewBag.ErrorMessage = "Error starting game. Please try again";
            }
        }


        /// <summary>
        ///  Captures the users input
        /// </summary>
        /// <param name="input">HangmanClass contains values captured by players input</param>
        /// 
        /// <returns>Returns the next phase of the game</returns>
        [HttpPost]
        public ActionResult PlayerInput(HangmanClass input)
        {
            if ( ModelState.IsValid )
            {
                // join input to saved model
                var playerInput = input.playerInput;
                HangmanClass hangman = TempData["Hangman"] as HangmanClass;
                hangman.playerInput = playerInput;
                if( hangman.isPlaying == false)
                    return RedirectToAction( "NewGame" );
                if ( !string.IsNullOrWhiteSpace( playerInput ) )
                {
                    char inputChar = playerInput[0];

                    bool isValid = ValidatePlayerInput( inputChar );
                    if ( isValid )
                    {
                        bool isNewLetter = CheckLetter( inputChar );
                        if ( isNewLetter )
                        {
                            hangman.usedLetters.Add( inputChar );
                            bool isMatch = doesWordContainInput();
                            if ( !isMatch )
                            {
                                hangman.incorrectAnswerCount++;
                                hangman.incorrectLetters.Add( inputChar );
                            }
                            // correct answer
                            hangman.correctLetters.Add( inputChar );
                        }
                        else
                        {
                            //already used letter message
                            ViewBag.Message = $"You already selected {hangman.playerInput}";
                        }
                    }
                    else
                    {
                        // incorrect letter message
                        ViewBag.Message = "Please select a letter";
                    }
                }

                var models = SetupModel();
                int incorrectAnswerCount = hangman.incorrectAnswerCount;

                ViewBag.CurrentState = models.ElementAt( incorrectAnswerCount ).Image;
                ViewBag.WordToGuess = hangman.playerGuess;
                ViewBag.UsedLetters = string.Join(", ", hangman.usedLetters);
                ViewBag.IncorrectLetters = string.Join( ", ", hangman.incorrectLetters );
                ViewBag.IncorrectCount = hangman.incorrectAnswerCount;
                ViewBag.IsPlaying = true;

                TempData["Hangman"] = hangman;

                if ( hangman.playerGuess == hangman.wordToGuess )
                {
                    // won game
                    TempData["HasWon"] = true;
                    TempData["WordToGuess"] = hangman.wordToGuess;
                    TempData["CurrentState"] = models.ElementAt( incorrectAnswerCount ).Image;
                    TempData["IncorrectLetters"] = string.Join( ", " , hangman.incorrectLetters );
                    hangman.hasWon = true;
                    TempData["Hangman"] = hangman;
                    return RedirectToAction( "NextGame");
                }
                else if ( hangman.incorrectAnswerCount == 6 )
                {
                    // game over                
                    TempData["HasWon"] = false;
                    TempData["WordToGuess"] = hangman.wordToGuess;
                    TempData["CurrentState"] = models.ElementAt( incorrectAnswerCount ).Image;
                    TempData["IncorrectLetters"] = string.Join( ", " , hangman.incorrectLetters );
                    hangman.hasWon = false;
                    TempData["Hangman"] = hangman;
                    return RedirectToAction( "NextGame" );
                }
                else
                {
                    return View( "Game" , hangman );
                }
            }
            return View( "Index" );
        }

        private bool CheckLetter(char input)
        {
            var hangman = TempData["Hangman"] as HangmanClass;
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

        public bool doesWordContainInput()
        {
            var hangman = TempData["Hangman"] as HangmanClass;
            char input = hangman.playerInput[0];
            List<char> letters = hangman.wordToGuess.ToList();
            List<char> revealedLetters = hangman.playerGuess.ToList();
            bool match = false;
            for ( int i = 0; i < letters.Count; i++ )
            {
                var l = letters[i];
                if( l == input )
                {
                    revealedLetters[i] = input;
                    match = true;
                }
            }
            hangman.playerGuess = new string( revealedLetters.ToArray() );
            TempData["Hangman"] = hangman;
            return match;
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