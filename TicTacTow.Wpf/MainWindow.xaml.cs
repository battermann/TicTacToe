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
                .Select(b => Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(x => b.Click += x, x => b.Click -= x).Select(x => ((Button) x.Sender).Name.ToPosition()))
                .Merge()
                .Select(playedPosition =>
                {
                    moveResult = HandleUserInput(playedPosition, moveResult);
                    return moveResult;
                })
                .ObserveOnDispatcher()
                .Subscribe(ui.Update);
        }

        private static TicTacToeDomain.MoveResult HandleUserInput(Tuple<TicTacToeDomain.HorizPosition, TicTacToeDomain.VertPosition> playedPosition, TicTacToeDomain.MoveResult moveResult)
        {
            if (moveResult.IsPlayerXToMove)
            {
                var useraction = (moveResult as TicTacToeDomain.MoveResult.PlayerXToMove).Item2.FirstOrDefault(m => m.posToPlay.Equals(playedPosition));
                return useraction != null ? useraction.capability.Invoke(null) : moveResult;
            }

            if (moveResult.IsPlayerOToMove)
            {
                var useraction = (moveResult as TicTacToeDomain.MoveResult.PlayerOToMove).Item2.FirstOrDefault(m => m.posToPlay.Equals(playedPosition));
                return useraction != null ? useraction.capability.Invoke(null) : moveResult;
            }

            return moveResult;
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
        private TextBlock StatusInfo { get; set; }

        public void Update(TicTacToeDomain.MoveResult moveResult)
        {
            Func<TicTacToeDomain.CellState, string> makeMarker = state =>
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
                var msg = String.Format("GAME WON by Player {0}", gameWon.Item2.IsPlayerX ? "X" : "O");
                StatusInfo.Text = msg;
                MessageBox.Show(msg);
            }

            if (moveResult.IsGameTied)
            {
                displayInfo = (moveResult as TicTacToeDomain.MoveResult.GameTied).Item;
                StatusInfo.Text = "GAME OVER - Tie";
            }

            displayInfo.cells
                .ToList()
                .ForEach(c => Positions.Single(position => position.Name.ToPosition().Equals(c.pos)).Content = makeMarker(c.state));
        }
    }

    public static class Helper
    {
        public static Tuple<TicTacToeDomain.HorizPosition, TicTacToeDomain.VertPosition> ToPosition(this string pos)
        {
            switch (pos)
            {
                case "LeftTop": return Tuple.Create(TicTacToeDomain.HorizPosition.Left, TicTacToeDomain.VertPosition.Top);
                case "LeftVCenter": return Tuple.Create(TicTacToeDomain.HorizPosition.Left, TicTacToeDomain.VertPosition.VCenter);
                case "LeftBottom": return Tuple.Create(TicTacToeDomain.HorizPosition.Left, TicTacToeDomain.VertPosition.Bottom);
                case "HCenterTop": return Tuple.Create(TicTacToeDomain.HorizPosition.HCenter, TicTacToeDomain.VertPosition.Top);
                case "HCenterVCenter": return Tuple.Create(TicTacToeDomain.HorizPosition.HCenter, TicTacToeDomain.VertPosition.VCenter);
                case "HCenterBottom": return Tuple.Create(TicTacToeDomain.HorizPosition.HCenter, TicTacToeDomain.VertPosition.Bottom);
                case "RightTop": return Tuple.Create(TicTacToeDomain.HorizPosition.Right, TicTacToeDomain.VertPosition.Top);
                case "RightVCenter": return Tuple.Create(TicTacToeDomain.HorizPosition.Right, TicTacToeDomain.VertPosition.VCenter);
                case "RightBottom": return Tuple.Create(TicTacToeDomain.HorizPosition.Right, TicTacToeDomain.VertPosition.Bottom);
                default: throw new Exception("cannot process cell state");
            }
        }
    }

    public static class ActionConverter
    {
        private static readonly Unit Unit = (Unit)Activator.CreateInstance(typeof(Unit), true);

        private static Func<T, Unit> ToFunc<T>(this Action<T> action)
        {
            return x => { action(x); return Unit; };
        }

        public static FSharpFunc<T, Unit> ToFSharpFunc<T>(this Action<T> action)
        {
            return FSharpFunc<T, Unit>.FromConverter(new Converter<T, Unit>(action.ToFunc()));
        }
    }
}
