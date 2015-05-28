// -----------------------------------------------------------
// Logging
// -----------------------------------------------------------
namespace TicTacToe

module Logger = 
    open TicTacToeDomain

    /// Transform a MoveCapability into a logged version
    let transformCapability log transformMR player cellPos (cap:MoveCapability) :MoveCapability =
        
        // create a new capability that logs the player & cellPos when run
        let newCap() =
            log (sprintf "LOGINFO: %A played %A" player cellPos)
            let moveResult = cap() 
            transformMR moveResult 
        newCap
 
    /// Transform a NextMove into a logged version
    let transformNextMove log transformMR player (move:NextMoveInfo) :NextMoveInfo = 
        let cellPos = move.posToPlay 
        let cap = move.capability
        {move with capability = transformCapability log transformMR player cellPos cap} 
 
    /// Transform a MoveResult into a logged version
    let rec transformMoveResult log (moveResult:MoveResult) :MoveResult =
        
        let tmr = transformMoveResult // abbreviate!
 
        match moveResult with
        | PlayerXToMove (display,nextMoves) ->
            let nextMoves' = nextMoves |> List.map (transformNextMove log (tmr log) PlayerX) 
            PlayerXToMove (display,nextMoves') 
        | PlayerOToMove (display,nextMoves) ->
            let nextMoves' = nextMoves |> List.map (transformNextMove log (tmr log) PlayerO)
            PlayerOToMove (display,nextMoves') 
        | GameWon (display,player) ->
            log (sprintf "LOGINFO: Game won by %A" player)
            moveResult
        | GameTied display ->
            log (sprintf "LOGINFO: Game tied")
            moveResult
 
    /// inject logging into the API
    let injectLogging api log =
       
        // create a new API with the functions 
        // replaced with logged versions
        { api with
            newGame = fun () -> 
                log("LOGINFO: New Game started")
                api.newGame() |> transformMoveResult log
            }