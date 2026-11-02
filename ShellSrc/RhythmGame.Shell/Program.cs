using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RhythmGame.Core;

namespace RhythmGame.Shell
{
    public class Program
    {
        private const double TrackLength = 6.0; //track length in seconds

        public static void Main() //entry point for the rhythm game shell application
        {
            Console.Clear(); //clear the console window
            Console.CursorVisible = false; //hide the cursor for a cleaner interface
            Console.WriteLine(
                "⚡ MULTIPLIER CORE ENGAGED! Press [SPACEBAR] to match timing windows."
            ); //display initial instructions to the player
            Thread.Sleep(1200); //pause for 1.2 seconds to allow the player to read the instructions

            var noteTimeline = Microsoft.FSharp.Collections.ListModule.OfSeq( //create a list of note timings for the rhythm game
                new double[] { 1.0, 1.5, 2.0, 2.5, 3.2, 3.8, 4.4, 5.0 }
            );

            GameState state = RhythmEngine.createInitialState(noteTimeline); //initialize the game state with the note timeline
            var stopwatch = Stopwatch.StartNew(); //start a stopwatch to track the elapsed time of the game

            while (stopwatch.Elapsed.TotalSeconds < TrackLength) //continue the game loop until the elapsed time exceeds the track length
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds; //get the total elapsed time in seconds since the game started

                // Sync tick sweeps for dropped/overlooked milestones
                state = RhythmEngine.updateTimeTick(elapsed, state); //update the game state based on the elapsed time, checking for any notes that have been missed or processed

                if (Console.KeyAvailable) //check if a key has been pressed by the player
                {
                    var key = Console.ReadKey(intercept: true); //read the key press without displaying it in the console
                    if (key.Key == ConsoleKey.Spacebar) //check if the pressed key is the spacebar, which is used for player input in the rhythm game
                    {
                        state = RhythmEngine.registerPlayerTap(elapsed, state); //update the game state based on the player's tap input, checking if it matches any notes in the timeline
                    }
                }

                DrawViewportFrame(elapsed, state); //draw the current state of the game, including the elapsed time, score, combo, multiplier, and the scrolling chart of notes
                Thread.Sleep(16); // ~60fps layout refresh rate lock
            }

            Console.Clear(); //clear the console window after the game loop ends
            Console.WriteLine("🏁 CHART COMPLETED!");
            Console.WriteLine($"Final Score: {state.Score}"); //display the final score to the player
            Console.CursorVisible = true; //make the cursor visible again after the game ends
        }

        private static void DrawViewportFrame(double elapsed, GameState state) //method to render the current state of the game in the console window
        {
            Console.SetCursorPosition(0, 2); //set the cursor position to the top of the console window for consistent rendering of the game state
            Console.WriteLine($"Time: {elapsed:F2}s / {TrackLength:F2}s   "); //display the elapsed time and total track length in seconds, formatted to two decimal places
            Console.WriteLine(
                $"Score: {state.Score:D6}  |  Combo: {state.Combo}  |  Multiplier: {state.Multiplier}x     "
            ); //display the current score, combo count, and multiplier in a formatted manner

            // Format out the internal F# Option and Union structure cleanly into text formatting strings
            string judgmentText = "---"; //initialize the judgment text to a default value
            ConsoleColor judgmentColor = ConsoleColor.Gray; //initialize the judgment color to a default value

            if (
                state.LastJudgment != null
                && Microsoft.FSharp.Core.OptionModule.IsSome(state.LastJudgment)
            ) //check if there is a last judgment available in the game state
            {
                var val = state.LastJudgment.Value; //get the value of the last judgment if it exists
                if (val.IsPerfect)
                {
                    judgmentText = "⭐ PERFECT! [300]";
                    judgmentColor = ConsoleColor.Cyan;
                } //check if the last judgment was a perfect hit and set the corresponding text and color
                if (val.IsGreat)
                {
                    judgmentText = "👍 GREAT!   [100]";
                    judgmentColor = ConsoleColor.Green;
                }
                if (val.IsGood)
                {
                    judgmentText = "👌 GOOD    [50] ";
                    judgmentColor = ConsoleColor.Yellow;
                }
                if (val.IsMiss)
                {
                    judgmentText = "❌ MISS         ";
                    judgmentColor = ConsoleColor.Red;
                }
            }

            Console.Write("Rating: "); //display the rating label before the judgment text
            Console.ForegroundColor = judgmentColor; //set the console text color to the judgment color for visual feedback
            Console.Write(judgmentText); //display the judgment text based on the last judgment in the game state
            Console.ResetColor(); //reset the console text color to the default color after displaying the judgment text
            Console.WriteLine(
                "    \n==============================================================="
            );

            // Render scrolling chart path
            Console.Write("Lane: [ "); //display the lane label before rendering the scrolling chart of notes
            for (double t = 0; t < TrackLength; t += 0.15) //loop through the track length in increments of 0.15 seconds to render the notes and target line
            {
                var note = state.Notes.FirstOrDefault(n => Math.Abs(n.TargetTime - t) < 0.07); //find the first note in the game state that is close to the current time increment, within a threshold of 0.07 seconds

                if (Math.Abs(t - elapsed) < 0.07) //check if the current time increment is close to the elapsed time, within a threshold of 0.07 seconds, to render the target line
                {
                    Console.Write("||"); // Target line
                }
                else if (note != null) //check if a note exists at the current time increment to render it in the scrolling chart
                {
                    Console.Write(note.IsProcessed ? "-" : "O"); //render a processed note as a dash and an unprocessed note as an "O" in the scrolling chart
                }
                else
                {
                    Console.Write("-"); //render an empty space in the scrolling chart if no note exists at the current time increment
                }
            }
            Console.WriteLine(" ]"); //close the lane display with a closing bracket after rendering the scrolling chart of notes
        }
    }
}
