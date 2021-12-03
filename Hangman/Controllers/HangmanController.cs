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
        public List<HangmanClass> hangmanGames = new List<HangmanClass>();

        /// <summary>
        /// Get
        /// </summary>
        /// <returns>Forwards to NewGame</returns>
        public ActionResult Index()
        {
            return View("PlayNow");
        }

        /// <summary>
        /// Sets up a new game
        /// </summary>
        /// <returns>The beginning of a new game</returns>
        public ActionResult NewGame()
        {
            // clear possible state of a previous game
            TempData.Clear();

            // setup hangman and wait for everything to complete
            var task = Task.Run( () => InitiateGame() );
            Task newTask = task;
            newTask.Wait();
            
            // setup ViewBag for UI
            ViewBag.Message = "Press any letter key to make a guess!";
            ViewBag.IsPlaying = true;
            return PartialView( "Input" );
        }

        /// <summary>
        ///  Ask the user to start another game
        /// </summary>
        /// <param name="HasWon"> Will be null which means its the players first game </param>
        /// <returns> The next state of the game </returns>
        public ActionResult NextGame( string HasWon )
        {
            // if null its the first game
            if ( HasWon == null )
            {
                var hangman = (HangmanClass)TempData["Hangman"];
                // check to see if we have any state from previous player session
                bool? hasWon = (bool?)TempData["HasWon"];
                if ( hasWon == null )
                {
                    // setup View with details created by the controller
                    
                    var models = SetupModel();
                    int incorrectAnswerCount = hangman.incorrectAnswerCount;

                    // place game details in ViewBag for UI
                    ViewBag.HasWon = hangman.hasWon;
                    ViewBag.WordToGuess = hangman.wordToGuess;
                    ViewBag.CurrentState = models.ElementAt( incorrectAnswerCount ).Image;
                    ViewBag.IncorrectLetters = string.Join( ", " , hangman.incorrectLetters );
                    TempData["Hangman"] = hangman;
                }
                else
                {
                    // re-setup the view with details in temp storage
                    ViewBag.HasWon = ( bool ) TempData["HasWon"];
                    ViewBag.WordToGuess = TempData["WordToGuess"].ToString();
                    ViewBag.CurrentState = TempData["CurrentState"].ToString();
                    ViewBag.IncorrectLetters = TempData["IncorrectLetters"].ToString();
                }
                hangmanGames.Add( hangman );
                // present the player with Game.cshtml
                return PartialView( "Input" , hangman );
            }
            else
            {
                // present the player with NewGame.cshtml
                return PartialView( "NewGame" );
            }
        }

        /// <summary>
        /// Gets a random word from an API
        /// Setup Game by configuring model
        /// </summary>
        /// <returns></returns>
        private async Task InitiateGame()
        {
            // get a Random word generator
            //string apiUrl = "https://random-word-api.herokuapp.com/word?number=1&swear=0";
            //HttpResponseMessage response = await _client.GetAsync(apiUrl);
            //if ( response.IsSuccessStatusCode )
            //{
            //// bytes is the returned value from the API
            //var bytes = await response.Content.ReadAsByteArrayAsync();

            //// strip the array brackets off the word
            //hangman.wordToGuess = FormatWord( bytes );

            hangman.wordToGuess = GetRandomWord();
            //// setup the players guess from the word: _ _ _ _ 
            hangman.playerGuess = FormatGuess( hangman.wordToGuess );

            // setup ViewBag for UI and TempData for game state
                
            ViewBag.PlayerGuess = hangman.playerGuess;
            hangman.hangmanModels = SetupModel();
            hangman.incorrectLetters = new List<char>();
            hangman.correctLetters = new List<char>();
            hangman.usedLetters = new List<char>();
            ViewBag.CurrentState = hangman.hangmanModels.ElementAt( 0 ).Image;
            hangman.incorrectAnswerCount = 0;
            hangman.currentState = hangman.hangmanModels.ElementAt(0);
            hangman.isPlaying = true;
            TempData["Hangman"] = hangman;
            //}
            //else
            //{
            //    // failed to initate game
            //    ViewBag.Message = "Error starting game. Please try again";
            //}
        }


        /// <summary>
        ///  Captures the users input and tests its value
        /// </summary>
        /// <param name="input">HangmanClass contains values captured by players input</param>
        /// <returns>Returns the next phase of the game</returns>
        [HttpPost]
        public ActionResult PlayerInput(string input)
        {
            HangmanClass hangman = TempData["Hangman"] as HangmanClass;
            if ( ModelState.IsValid && hangman != null && hangman.isPlaying == true )
            {
                // join input to game state
                var playerInput = input.ToLower();
                hangman.playerInput = playerInput;

                // move forward with valid input
                if ( !string.IsNullOrWhiteSpace( playerInput ) )
                {
                    char inputChar = playerInput[0];
                    // validate the input char is a valid a-z letter
                    bool isValid = ValidatePlayerInput( inputChar );
                    if ( isValid )
                    {
                        // valid the player hasn't used this letter
                        bool isNewLetter = CheckLetter( inputChar );
                        if ( isNewLetter )
                        {
                            hangman.usedLetters.Add( inputChar );

                            // check if the letter is in the word
                            bool isMatch = doesWordContainInput();
                            if ( !isMatch )
                            {
                                // incorrect answer
                                hangman.incorrectAnswerCount++;
                                hangman.incorrectLetters.Add( inputChar );
                            }

                            // correct answer
                            hangman.correctLetters.Add( inputChar );

                        }
                        else
                        {
                            //already used letter error
                            ViewBag.Message = $"You already selected {hangman.playerInput}";
                        }
                    }
                    else
                    {
                        // incorrect letter error
                        ViewBag.Message = "Please select a letter";
                    }
                }

                // resetup model
                var models = SetupModel();
                int incorrectAnswerCount = hangman.incorrectAnswerCount;

                // store game state in ViewBag for UI
                if( hangman.incorrectAnswerCount < 6 )
                {
                    ViewBag.CurrentState = models.ElementAt( incorrectAnswerCount ).Image;
                    TempData["CurrentState"] = models.ElementAt( incorrectAnswerCount ).Image;
                }
                else
                {
                    ViewBag.CurrentState = models.Last().Image;
                    TempData["CurrentState"] = models.Last().Image;
                }

                ViewBag.PlayerGuess = hangman.playerGuess;
                ViewBag.UsedLetters = string.Join(", ", hangman.usedLetters);
                ViewBag.IncorrectLetters = string.Join( ", ", hangman.incorrectLetters );
                ViewBag.IncorrectCount = hangman.incorrectAnswerCount;
                ViewBag.IsPlaying = true;
                TempData["WordToGuess"] = hangman.wordToGuess;
                TempData["IncorrectLetters"] = string.Join( ", " , hangman.incorrectLetters );

                if ( hangman.playerGuess == hangman.wordToGuess )
                {
                    // won game
                    hangman.hasWon = true;
                    ViewBag.HasWon = "true";
                    ViewBag.PlayerGuess = hangman.playerGuess;
                    hangman.isPlaying = false;
                    // save Game State
                    TempData["Hangman"] = hangman;
                    return PartialView( "Input", hangman );

                }
                else if ( hangman.incorrectAnswerCount >= 6 )
                {
                    // game over                
                    hangman.hasWon = false;
                    ViewBag.HasWon = "false";
                    ViewBag.WordToGuess = hangman.wordToGuess;
                    ViewBag.PlayerGuess = hangman.playerGuess;
                    hangman.isPlaying = false;
                    // save Game State
                    TempData["Hangman"] = hangman;
                    return PartialView( "Input", hangman );
                }
                else
                {

                    ViewBag.PlayerGuess = hangman.playerGuess;
                    // save Game State
                    TempData["Hangman"] = hangman;
                    return PartialView( "GameBoard" , hangman );
                }
            }
            // todo:
            // Catch errors
            return View( "Index" );
        }

        /// <summary>
        ///  Checks to see if the input is a new letter
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool CheckLetter(char input)
        {
            // load Game State
            var hangman = TempData["Hangman"] as HangmanClass;

            if( hangman.usedLetters == null )
            {
                // is the first letter
                return true;
            }
            if( hangman.usedLetters.Contains( input ) )
            {
                // already used
                return false;
            }
            else
            {
                // is new letter
                return true;
            }
        }

        /// <summary>
        /// Iterates through the word to guess and updates the plays word with matches
        /// </summary>
        /// <returns>If the players input matched a letter</returns>
        public bool doesWordContainInput()
        {
            // load game state
            var hangman = TempData["Hangman"] as HangmanClass;

            // players chosen letter
            char input = hangman.playerInput[0];

            // word to guess in char list
            List<char> letters = hangman.wordToGuess.ToList();

            // players current word char list
            List<char> revealedLetters = hangman.playerGuess.ToList();

            // default with no match found
            bool match = false;

            // loop through word to guess
            for ( int i = 0; i < letters.Count; i++ )
            {
                var l = letters[i];
                if( l == input )
                {
                    // if letter matches replace the _ with the letter 
                    revealedLetters[i] = input;

                    // flag for match found
                    match = true;
                }
            }

            // set the players guesses new state
            hangman.playerGuess = new string( revealedLetters.ToArray() );

            // save game state
            TempData["Hangman"] = hangman;

            return match;
        }

        /// <summary>
        /// Validates the players input is a letter a-z
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool ValidatePlayerInput( char input)
        {
            return char.IsLetter(input);
        }

        /// <summary>
        /// Creates an underscore for every letter in the word
        /// </summary>
        /// <param name="wordToGuess"></param>
        /// <returns></returns>
        private string FormatGuess( string wordToGuess )
        {
            string guess = "";
            foreach( char letter in wordToGuess )
            {
                guess += "_";
            }
            return guess;
        }

        /// <summary>
        /// Strips the array characters off of the API return
        /// </summary>
        /// <param name="bytes">the random word received from API</param>
        /// <returns></returns>
        private string FormatWord(Byte[] bytes)
        {
            var wordTrimmed = System.Text.Encoding.Default.GetString(bytes).Remove(0,2);
            var wordLength = wordTrimmed.Length - 2;
            return wordTrimmed.Substring(0, wordLength);
        }

        private string GetRandomWord()
        {
            var words = new string[] { "craven","satisfying","cumbersome","harmonious","dirty","basketball","furtive","downtown","wrathful","gruesome","sail","please","puzzled","shaggy","truculent","moor","order","whistle","educated","responsible","volcano","wrong","magical","arch","squirrel","cool","queen","suspect","innate","wry","dapper","obscene","sink","naughty","elfin","channel","birthday","activity","juice","deserted","sleet","greedy","throat","TRUE","save","man","spiders","flimsy","kneel","imminent","open","hurried","current","trade","wonderful","careful","frequent","cemetery","miss","condition","butter","governor","scratch","knit","language","knock","abhorrent","dazzling","frightened","ray","ice","gullible","dog","lick","oatmeal","tiger","amusing","tangy","dad","grandiose","tangible","tug","fish","icicle","uptight","engine","smash","victorious","attraction","substantial","paste","obedient","woozy","airport","hill","helpless","precious","agree","rampant","locket","decorous","vegetable","flowery","desire","brass","cushion","blushing","cats","wobble","sea","cloth","sincere","spectacular","pathetic","earsplitting","famous","recondite","absent","supply","monkey","ritzy","humorous","motion","efficient","shy","obnoxious","duck","trouble","flagrant","queue","train","successful","jumbled","flood","van","snore","aback","behave","lie","unite","money","hospital","picture","end","defective","daffy","circle","equable","matter","versed","unique","turn","agreeable","naive","frantic","innocent","material","buzz","disarm","rate","wind","deep","boundless","suck","spell","outrageous","laborer","consider","tub","fit","treat","chalk","questionable","huge","program","wrestle","plastic","book","milk","room","analyze","park","accept","development","afternoon","friction","snakes","useless","excuse","plane","broken","delicate","bounce","double","sleepy","grouchy","exchange","dinner","slope","stupendous","lame","whine","rake","watery","selection","political","seat","drum","shoe","taste","weary","skinny","trees","caring","squeamish","lean","amuck","upset","church","effect","surround","inexpensive","wool","cagey","school","long","elderly","warm","squeak","defeated","tramp","label","neck","tricky","guttural","cub","right","top","ripe","iron","paint","title","minute","place","tumble","unsightly","mug","plants","somber","lighten","lonely","prick","whip","event","nutty","spring","war","violet","bad","bee","friend","ultra","tame","press","control","bashful","itch","adaptable","sordid","mean","afterthought","thank","cheer","gaudy","overwrought","battle","hissing","overt","oven","hapless","slippery","produce","resolute","voracious","brown","squash","object","record","aquatic","crazy","nation","yak","club","squealing","obsolete","fall","bedroom","root","complex","hurt","fresh","sky","hair","near","rhyme","unusual","anxious","song","shiny","serve","expansion","unaccountable","things","sour","comparison","terrific","veil","lunchroom","deafening","elated","dark","orange","sore","kindhearted","crack","mellow","flow","sophisticated","donkey","baby","nice","arm","rabid","unnatural","day","helpful","men","unknown","mammoth","playground","squeal","judge","support","wet","desk","certain","punishment","moaning","breath","riddle","thunder","name","verse","whole","quixotic","real","river","group","stay","wakeful","wretched","imported","abstracted","hollow","lunch","remove","correct","fantastic","alert","tray","chickens","different","important","pear","frame","alarm","discover","border","utopian","cut","busy","appear","zoom","flash","invent","puny","relax","answer","heat","hysterical","salt","painstaking","whisper","greasy","route","boat","question","steadfast","simplistic","little","imagine","succeed","bat","uppity","glib","thirsty","basket","wide","birds","spotty","utter","melodic","thin","grandfather","approval","pause","shake","remember","mint","flashy","detect","sudden","married","enthusiastic","search","peaceful","glistening","vivacious","tasteful","resonant","scattered","awful","coordinated","moon","misty","sneaky","overconfident","land","cheat","repair","aboard","hum","proud","promise","eager","ban","aggressive","crowd","store","talented","sigh","sisters","rely","week","filthy","disgusted","slim","suggest","thick","cry","gratis","gorgeous","damage","cross","use","uninterested","hideous","weather","power","attach","addition","paddle","yard","excellent","digestion","agonizing","finger","bomb","vein","wash","love","tickle","pastoral","hand","base","merciful","roomy","rob","glossy","elastic","tease","glove","claim","list","women" };
            Random rand = new Random();
            int number = rand.Next(0,494);
            return words[number];
        }

        /// <summary>
        /// Sets up the hangman model in state
        /// </summary>
        /// <returns></returns>
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