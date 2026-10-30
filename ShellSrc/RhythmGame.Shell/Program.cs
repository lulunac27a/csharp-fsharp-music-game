using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RhythmGame.Core;

namespace RhythmGame.Shell
{
    public class Program
    {
        private const double TrackLength = 6.0;

        public static void Main()
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.WriteLine("⚡ MULTIPLIER CORE ENGAGED! Press [SPACEBAR] to match timing windows.");
            Thread.Sleep(1200);

            var noteTimeline = Microsoft.FSharp.Collections.ListModule.OfSeq(
                new double[] { 1.0, 1.5, 2.0, 2.5, 3.2, 3.8, 4.4, 5.0 }
            );

            GameState state = RhythmEngine.createInitialState(noteTimeline);
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed.TotalSeconds < TrackLength)
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;

                // Sync tick sweeps for dropped/overlooked milestones
                state = RhythmEngine.updateTimeTick(elapsed, state);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        state = RhythmEngine.registerPlayerTap(elapsed, state);
                    }
                }

                DrawViewportFrame(elapsed, state);
                Thread.Sleep(16); // ~60fps layout refresh rate lock
            }

            Console.Clear();
            Console.WriteLine("🏁 CHART COMPLETED!");
            Console.WriteLine($"Final Score: {state.Score}");
            Console.CursorVisible = true;
        }

        private static void DrawViewportFrame(double elapsed, GameState state)
        {
            Console.SetCursorPosition(0, 2);
            Console.WriteLine($"Time: {elapsed:F2}s / {TrackLength:F2}s   ");
            Console.WriteLine($"Score: {state.Score:D6}  |  Combo: {state.Combo}  |  Multiplier: {state.Multiplier}x     ");
            
            // Format out the internal F# Option and Union structure cleanly into text formatting strings
            string judgmentText = "---";
            ConsoleColor judgmentColor = ConsoleColor.Gray;

            if (state.LastJudgment != null && Microsoft.FSharp.Core.OptionModule.IsSome(state.LastJudgment))
            {
                var val = state.LastJudgment.Value;
                if (val.IsPerfect)  { judgmentText = "⭐ PERFECT! [300]"; judgmentColor = ConsoleColor.Cyan; }
                if (val.IsGreat)    { judgmentText = "👍 GREAT!   [100]"; judgmentColor = ConsoleColor.Green; }
                if (val.IsGood)     { judgmentText = "👌 GOOD    [50] "; judgmentColor = ConsoleColor.Yellow; }
                if (val.IsMiss)     { judgmentText = "❌ MISS         "; judgmentColor = ConsoleColor.Red; }
            }

            Console.Write("Rating: ");
            Console.ForegroundColor = judgmentColor;
            Console.Write(judgmentText);
            Console.ResetColor();
            Console.WriteLine("    \n===============================================================");

            // Render scrolling chart path
            Console.Write("Lane: [ ");
            for (double t = 0; t < TrackLength; t += 0.15)
            {
                var note = state.Notes.FirstOrDefault(n => Math.Abs(n.TargetTime - t) < 0.07);

                if (Math.Abs(t - elapsed) < 0.07) 
                {
                    Console.Write("||"); // Target line
                }
                else if (note != null) 
                {
                    Console.Write(note.IsProcessed ? "-" : "O");
                }
                else 
                {
                    Console.Write("-");
                }
            }
            Console.WriteLine(" ]");
        }
    }
}
