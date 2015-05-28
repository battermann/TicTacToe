namespace TicTacToe

open System

// -----------------------------------------------------------
// TicTacToeDomain 
// -----------------------------------------------------------

module TicTacToeDomain =

    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 

    type Player = PlayerO | PlayerX

    type CellState = 
        | Played of Player 
        | Empty

    type Cell = {
        pos : CellPosition 
        state : CellState 
        }

    /// Everything the UI needs to know to display the board
    type DisplayInfo = {
        cells : Cell list
        }
    
    /// The capability to make a move at a particular location.
    /// The gamestate, player and position are already "baked" into the function.
    type MoveCapability = 
        unit -> MoveResult 

    /// A capability along with the position the capability is associated with.
    /// This allows the UI to show information so that the user
    /// can pick a particular capability to exercise.
    and NextMoveInfo = {
        // the pos is for UI information only
        // the actual pos is baked into the cap.
        posToPlay : CellPosition 
        capability : MoveCapability }

    /// The result of a move. It includes: 
    /// * The information on the current board state.
    /// * The capabilities for the next move, if any.
    and MoveResult = 
        | PlayerXToMove of DisplayInfo * NextMoveInfo list 
        | PlayerOToMove of DisplayInfo * NextMoveInfo list 
        | GameWon of DisplayInfo * Player 
        | GameTied of DisplayInfo 

    // Only the newGame function is exported from the implementation
    // all other functions come from the results of the previous move
    type TicTacToeAPI  = 
        {
        newGame : MoveCapability
        }

// -----------------------------------------------------------
// TicTacToeImplementation 
// -----------------------------------------------------------

module TicTacToeImplementation =
    open TicTacToeDomain

    /// private implementation of game state
    type GameState = {
        cells : Cell list
        }

    /// the list of all horizontal positions
    let allHorizPositions = [Left; HCenter; Right]
    
    /// the list of all horizontal positions
    let allVertPositions = [Top; VCenter; Bottom]

    /// A type to store the list of cell positions in a line
    type Line = Line of CellPosition list

    /// a list of the eight lines to check for 3 in a row
    let linesToCheck = 
        let mkHLine v = Line [for h in allHorizPositions do yield (h,v)]
        let hLines= [for v in allVertPositions do yield mkHLine v] 

        let mkVLine h = Line [for v in allVertPositions do yield (h,v)]
        let vLines = [for h in allHorizPositions do yield mkVLine h] 

        let diagonalLine1 = Line [Left,Top; HCenter,VCenter; Right,Bottom]
        let diagonalLine2 = Line [Left,Bottom; HCenter,VCenter; Right,Top]

        // return all the lines to check
        [
        yield! hLines
        yield! vLines
        yield diagonalLine1 
        yield diagonalLine2 
        ]

    /// get the DisplayInfo from the gameState
    let getDisplayInfo gameState = 
        {DisplayInfo.cells = gameState.cells}

    /// get the cell corresponding to the cell position
    let getCell gameState posToFind = 
        gameState.cells 
        |> List.find (fun cell -> cell.pos = posToFind)

    /// update a particular cell in the GameState 
    /// and return a new GameState
    let private updateCell newCell gameState =

        // create a helper function
        let substituteNewCell oldCell =
            if oldCell.pos = newCell.pos then
                newCell
            else 
                oldCell                 

        // get a copy of the cells, with the new cell swapped in
        let newCells = gameState.cells |> List.map substituteNewCell 
        
        // return a new game state with the new cells
        {gameState with cells = newCells }

    /// Return true if the game was won by the specified player
    let private isGameWonBy player gameState = 
        
        // helper to check if a cell was played by a particular player
        let cellWasPlayedBy playerToCompare cell = 
            match cell.state with
            | Played player -> player = playerToCompare
            | Empty -> false

        // helper to see if every cell in the Line has been played by the same player
        let lineIsAllSamePlayer player (Line cellPosList) = 
            cellPosList 
            |> List.map (getCell gameState)
            |> List.forall (cellWasPlayedBy player)

        linesToCheck
        |> List.exists (lineIsAllSamePlayer player)


    /// Return true if all cells have been played
    let private isGameTied gameState = 
        // helper to check if a cell was played by any player
        let cellWasPlayed cell = 
            match cell.state with
            | Played _ -> true
            | Empty -> false

        gameState.cells
        |> List.forall cellWasPlayed 

    /// determine the remaining moves 
    let private remainingMoves gameState = 

        // helper to return Some if a cell is playable
        let playableCell cell = 
            match cell.state with
            | Played player -> None
            | Empty -> Some cell.pos

        gameState.cells
        |> List.choose playableCell

    // return the other player    
    let otherPlayer player = 
        match player with
        | PlayerX -> PlayerO
        | PlayerO -> PlayerX


    // return the move result case for a player 
    let moveResultFor player displayInfo nextMoves = 
        match player with
        | PlayerX -> PlayerXToMove (displayInfo, nextMoves)
        | PlayerO -> PlayerOToMove (displayInfo, nextMoves)

    // given a function, a player & a gameState & a position,
    // create a NextMoveInfo with the capability to call the function
    let makeNextMoveInfo f player gameState cellPos =
        // the capability has the player & cellPos & gameState baked in
        let capability() = f player cellPos gameState 
        {posToPlay=cellPos; capability=capability}

    // given a function, a player & a gameState & a list of positions,
    // create a list of NextMoveInfos wrapped in a MoveResult
    let makeMoveResultWithCapabilities f player gameState cellPosList =
        let displayInfo = getDisplayInfo gameState
        cellPosList
        |> List.map (makeNextMoveInfo f player gameState)
        |> moveResultFor player displayInfo 

    // player X or O makes a move
    let rec playerMove player cellPos gameState  = 
        let newCell = {pos = cellPos; state = Played player}
        let newGameState = gameState |> updateCell newCell 
        let displayInfo = getDisplayInfo newGameState 

        if newGameState |> isGameWonBy player then
            // return the move result
            GameWon (displayInfo, player) 
        elif newGameState |> isGameTied then
            // return the move result
            GameTied displayInfo 
        else
            let otherPlayer = otherPlayer player 
            let moveResult = 
                newGameState 
                |> remainingMoves
                |> makeMoveResultWithCapabilities playerMove otherPlayer newGameState
            moveResult 

    /// create the state of a new game
    let newGame() = 

        // allPositions is the cross-product of the positions
        let allPositions = [
            for h in allHorizPositions do 
            for v in allVertPositions do 
                yield (h,v)
            ]

        // all cells are empty initially
        let emptyCells = 
            allPositions 
            |> List.map (fun pos -> {pos = pos; state = Empty})
        
        // create initial game state
        let gameState = { cells=emptyCells }            

        // initial of valid moves for player X is all positions
        let moveResult = 
            allPositions 
            |> makeMoveResultWithCapabilities playerMove PlayerX gameState

        // return new game
        moveResult 


    /// export the API to the application
    let api = {
        newGame = newGame 
        }