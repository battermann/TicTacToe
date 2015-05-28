using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.FSharp.Core;

namespace TicTacToe.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var ui = new Ui(
                positions: new[] { LeftTop, LeftVCenter, LeftBottom, HCenterTop, HCenterVCenter, HCenterBottom, RightTop, RightVCenter, RightBottom },
                statusInfo: StatusText);

            var log = ActionConverter.ToFSharpFunc((string s) => Log.Text = s + (String.IsNullOrWhiteSpace(Log.Text) ? String.Empty : Environment.NewLine) + Log.Text);
            var api = Logger.injectLogging(TicTacToeImplementation.api, log);
            var moveResult = api.newGame.Invoke(null);
            ui.Update(moveResult);

            NewGameButton.Click += (sender, args) =>
            {
                moveResult = api.newGame.Invoke(null);
                ui.Update(moveResult);
            };

            ui.Positions
                .Select(b => Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(x => b.Click += x, x => b.Click -= x).Select(x => ((Button) x.Sender).Name))
                .Merge()
                .Select(playedPosition =>
                {
                    moveResult = HandleUserInput(playedPosition, moveResult);
                    return moveResult;
                })
                .ObserveOnDispatcher()
                .Subscribe(ui.Update);
        }

        private static TicTacToeDomain.MoveResult HandleUserInput(string playedPosition, TicTacToeDomain.MoveResult moveResult)
        {
            TicTacToeDomain.NextMoveInfo userAction = null;

            if (moveResult.IsPlayerXToMove)
                userAction = (moveResult as TicTacToeDomain.MoveResult.PlayerXToMove).Item2.FirstOrDefault(m => m.posToPlay.ToPositionName() == playedPosition);

            if (moveResult.IsPlayerOToMove)
                userAction = (moveResult as TicTacToeDomain.MoveResult.PlayerOToMove).Item2.FirstOrDefault(m => m.posToPlay.ToPositionName() == playedPosition);

            return userAction != null 
                ? userAction.capability.Invoke(null) 
                : moveResult;
        }
    }

    public class Ui
    {
        public Ui(Button[] positions, TextBlock statusInfo)
        {
            StatusInfo = statusInfo;
            Positions = positions;
        }

        public Button[] Positions { get; private set; }
        public TextBlock StatusInfo { get; private set; }

        public void Update(TicTacToeDomain.MoveResult moveResult)
        {
            Func<TicTacToeDomain.CellState, string> makeMarker;
            makeMarker = state =>
                state.IsPlayed
                    ? (state as TicTacToeDomain.CellState.Played).Item == TicTacToeDomain.Player.PlayerX ? "X" : "0"
                    : String.Empty;

            TicTacToeDomain.DisplayInfo displayInfo = null;

            if (moveResult.IsPlayerXToMove)
            {
                displayInfo = (moveResult as TicTacToeDomain.MoveResult.PlayerXToMove).Item1;
                StatusInfo.Text = "Player X to move";
            }

            if (moveResult.IsPlayerOToMove)
            {
                displayInfo = (moveResult as TicTacToeDomain.MoveResult.PlayerOToMove).Item1;
                StatusInfo.Text = "Player O to move";
            }

            if (moveResult.IsGameWon)
            {
                var gameWon = (moveResult as TicTacToeDomain.MoveResult.GameWon);
                displayInfo = gameWon.Item1;
                StatusInfo.Text = String.Format("GAME WON by Player {0}", gameWon.Item2.IsPlayerX ? "X" : "O");
            }

            if (moveResult.IsGameTied)
            {
                displayInfo = (moveResult as TicTacToeDomain.MoveResult.GameTied).Item;
                StatusInfo.Text = "GAME OVER - Tie";
            }

            displayInfo.cells
                .ToList()
                .ForEach(c => Positions.Single(position => position.Name == c.pos.ToPositionName()).Content = makeMarker(c.state));
        }
    }

    public static class Helper
    {
        public static string ToPositionName(this Tuple<TicTacToeDomain.HorizPosition, TicTacToeDomain.VertPosition> pos)
        {
            if (pos.Item1.IsLeft && pos.Item2.IsTop)
                return "LeftTop";
            if (pos.Item1.IsLeft && pos.Item2.IsVCenter)
                return "LeftVCenter";
            if (pos.Item1.IsLeft && pos.Item2.IsBottom)
                return "LeftBottom";
            if (pos.Item1.IsHCenter && pos.Item2.IsTop)
                return "HCenterTop";
            if (pos.Item1.IsHCenter && pos.Item2.IsVCenter)
                return "HCenterVCenter";
            if (pos.Item1.IsHCenter && pos.Item2.IsBottom)
                return "HCenterBottom";
            if (pos.Item1.IsRight && pos.Item2.IsTop)
                return "RightTop";
            if (pos.Item1.IsRight && pos.Item2.IsVCenter)
                return "RightVCenter";
            if (pos.Item1.IsRight && pos.Item2.IsBottom)
                return "RightBottom";
            throw new Exception("cannot process cell state");
        }
    }

    public static class ActionConverter
    {
        private static readonly Unit Unit = (Unit)Activator.CreateInstance(typeof(Unit), true);

        public static Func<T, Unit> ToFunc<T>(this Action<T> action)
        {
            return x => { action(x); return Unit; };
        }

        public static FSharpFunc<T, Unit> ToFSharpFunc<T>(this Action<T> action)
        {
            return FSharpFunc<T, Unit>.FromConverter(new Converter<T, Unit>(action.ToFunc()));
        }
    }
}
