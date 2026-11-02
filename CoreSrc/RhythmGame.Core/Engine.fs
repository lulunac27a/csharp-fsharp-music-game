namespace RhythmGame.Core

open System

// Distinct scoring accuracy evaluation tiers
type Judgment = //represents the different levels of accuracy for player input timing
    | Perfect // Within 40ms
    | Great // Within 90ms
    | Good // Within 150ms
    | Miss // Out of bounds or skipped

type Note =
    { //represents a musical note in the rhythm game with its target time and processing state
      TargetTime: double //the exact time in seconds when the note should be hit by the player
      IsProcessed: bool } //a flag indicating whether the note has already been processed (hit or missed) by the player

type GameState =
    { //represents the current state of the rhythm game, including score, combo, multiplier, last judgment, and the list of notes
      Score: int //the player's current score in the game, which accumulates based on successful hits and their corresponding judgments
      Combo: int //the current streak of consecutive successful hits by the player, which can affect scoring and multiplier
      Multiplier: int //the current score multiplier that increases with higher combos, enhancing the points earned for each successful hit
      LastJudgment: Judgment option //the most recent judgment received by the player for their last input, which can be Perfect, Great, Good, or Miss
      Notes: Note list } //the list of notes in the game, each with its target time and processing state, representing the sequence of musical events the player must interact with

module RhythmEngine = //contains the core logic for the rhythm game engine, including timing evaluation, scoring, and state updates
    // Precision millisecond boundaries converted to seconds
    [<Literal>]
    let PerfectWindow = 0.040 //the time window in seconds for a Perfect judgment, meaning the player's input must be within 40 milliseconds of the note's target time to receive this judgment

    [<Literal>]
    let GreatWindow = 0.090 //the time window in seconds for a Great judgment, meaning the player's input must be within 90 milliseconds of the note's target time to receive this judgment

    [<Literal>]
    let GoodWindow = 0.150 //the time window in seconds for a Good judgment, meaning the player's input must be within 150 milliseconds of the note's target time to receive this judgment

    let createInitialState (times: double list) : GameState = //creates the initial game state with a score of 0, combo of 0, multiplier of 1, no last judgment, and a list of notes based on the provided target times
        { Score = 0
          Combo = 0
          Multiplier = 1
          LastJudgment = None
          Notes = times |> List.map (fun t -> { TargetTime = t; IsProcessed = false }) }

    // Evaluates what a specific timing differential translates to in judgment tiers
    let evaluateTiming (delta: double) : Judgment = //evaluates the timing difference (delta) between the player's input and the note's target time, returning the corresponding judgment tier based on predefined windows for Perfect, Great, Good, and Miss
        if delta <= PerfectWindow then Perfect
        elif delta <= GreatWindow then Great
        elif delta <= GoodWindow then Good
        else Miss

    // Translates judgment tiers into raw baseline scores
    let getBaseScore = //translates a given judgment tier into its corresponding base score value, where Perfect yields 300 points, Great yields 100 points, Good yields 50 points, and Miss yields 0 points
        function
        | Perfect -> 300
        | Great -> 100
        | Good -> 50
        | Miss -> 0

    // Calculates scaling multiplier tier increments based on ongoing streak benchmarks
    let calculateMultiplier (combo: int) : int = //calculates the scaling multiplier tier based on the player's current combo, with tiers of 1, 2, 3, or 4 corresponding to combo thresholds of 10, 20, and 40
        if combo >= 40 then 4
        elif combo >= 20 then 3
        elif combo >= 10 then 2
        else 1

    // 1. Tick engine update loop sweep
    let updateTimeTick (elapsed: double) (state: GameState) : GameState = //updates the game state based on the elapsed time, checking for any notes that have passed entirely out of the widest acceptable window (+150ms) and marking them as missed, resetting the combo and multiplier, and updating the last judgment to Miss if any missed notes exist
        // Auto-miss notes that pass entirely out of the widest acceptable window (+150ms)
        let notePastWindow n = //check if a note has passed the GoodWindow and is unprocessed, indicating that it should be marked as missed
            not n.IsProcessed && elapsed > n.TargetTime + GoodWindow

        let missedNotesExist = state.Notes |> List.exists notePastWindow //check if any notes have passed the GoodWindow and are unprocessed, indicating that they should be marked as missed

        if missedNotesExist then
            let updatedNotes =
                state.Notes
                |> List.map (fun n ->
                    if notePastWindow n then
                        { n with IsProcessed = true }
                    else
                        n)

            { state with //update state for missed notes
                Notes = updatedNotes
                Combo = 0
                Multiplier = 1
                LastJudgment = Some Miss }
        else
            state

    // 2. Action engine player input execution intercept
    let registerPlayerTap (elapsed: double) (state: GameState) : GameState = //registers a player's tap input at the given elapsed time, evaluating the timing against the closest unprocessed note within the GoodWindow, updating the game state with the appropriate judgment, score, combo, and multiplier, or resetting the combo and multiplier if no valid note is found
        let getProximity n = abs (n.TargetTime - elapsed) //calculate the absolute difference between the note's target time and the elapsed time to determine how close the player's input is to the note's timing

        let targetNote =
            state.Notes
            |> List.filter (fun n -> not n.IsProcessed && getProximity n <= GoodWindow)
            |> List.sortBy getProximity
            |> List.tryHead

        match targetNote with
        | Some note ->
            let judgment = evaluateTiming (getProximity note)

            let updatedNotes =
                state.Notes
                |> List.map (fun n ->
                    if n.TargetTime = note.TargetTime then
                        { n with IsProcessed = true }
                    else
                        n)

            match judgment with
            | Miss -> // Boundary configuration fall-through fail-safes
                { state with //reset combo multiplier for missed notes
                    Notes = updatedNotes
                    Combo = 0
                    Multiplier = 1
                    LastJudgment = Some Miss }
            | hitJudgment -> //successful hit, update score and combo multiplier
                let nextCombo = state.Combo + 1
                let nextMult = calculateMultiplier nextCombo
                let addedValue = getBaseScore hitJudgment * state.Multiplier

                { Score = state.Score + addedValue //update score
                  Combo = nextCombo
                  Multiplier = nextMult
                  LastJudgment = Some hitJudgment
                  Notes = updatedNotes }
        | None -> //reset combo multiplier for stray taps
            // Penalty for stray blind taps out of phase completely
            { state with
                Combo = 0
                Multiplier = 1
                LastJudgment = Some Miss }
