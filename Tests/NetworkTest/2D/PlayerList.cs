using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;

namespace NetworkTest
{
    [SceneTree]
    public partial class PlayerList : CanvasLayer
    {
        private readonly Dictionary<int, Control[]> content = new();

        public void AddPlayer(PlayerProfile pp)
        {
            Visible = true;

            var items = Items().ToArray();
            content.Add(pp.GetMultiplayerAuthority(), items);
            items.ForEach(x => Content.AddChild(x));

            IEnumerable<Control> Items()
            {
                yield return Item(Player, pp.PlayerName);
                yield return Sep(Sep1);
                yield return Item(Vehicle);
                yield return Sep(Sep2);
                yield return Item(Score);

                Label Item(Label source, string content = "")
                {
                    var item = (Label)source.Duplicate();
                    item.SetFontColor(pp.PlayerColor);
                    item.Text = content;
                    return item;
                }

                Separator Sep(Separator source)
                    => (Separator)source.Duplicate();
            }
        }

        public void RemovePlayer(PlayerProfile pp)
        {
            content.Remove(pp.GetMultiplayerAuthority(), out var items);
            items.ForEach(x => Content.RemoveChild(x, free: true));

            if (content.Count is 0)
                Visible = false;
        }

        public void UpdatePlayer(PlayerStatus status)
        {
            if (!TryGetContent(status.PlayerId, out var items)) return;

            ((Label)items[Column.Vehicle]).Text = status.VehicleName?.Humanize(Humanizer.LetterCasing.Title);
            ((Label)items[Column.Score]).Text = $"{status.CurrentScore}";
            items[Column.Score].TooltipText = string.Join("\n", ScoreTooltip());
            items[Column.Score].SetMeta("Score", status.CurrentScore);

            SortByScore();

            IEnumerable<string> ScoreTooltip()
            {
                yield return $"Deliveries: {status.Deliveries}";
                yield return $"Captures:   {status.Captures}";
                yield return $"Steals:     {status.Steals}";
            }

            void SortByScore()
            {
                var columns = Content.Columns;

                var curIdx = items[0].GetIndex();
                if (curIdx <= columns) return;

                var prevScore = (int)Content.GetChild(curIdx - 1).GetMeta("Score", 0);
                if (status.CurrentScore > prevScore)
                {
                    items.ForEach(x => Content.MoveChild(x, x.GetIndex() - columns));
                    SortByScore();
                }
            }
        }

        public Control GetPlayerIcon(int playerId)
        {
            return TryGetContent(playerId, out var items)
                ? (Control)(Label)items[Column.Player].Duplicate()
                : null;
        }

        private bool TryGetContent(int playerId, out Control[] items)
        {
            Debug.Assert(playerId is not 0, "Wait for PlayerStatus.OnReady");

            if (content.TryGetValue(playerId, out items))
                return true;

            Log.Warn($"Unknown PlayerId {playerId}");
            return false;
        }

        private static class Column
        {
            public const int Player = 0;
            public const int Sep1 = 1;
            public const int Vehicle = 2;
            public const int Sep2 = 3;
            public const int Score = 4;
        }
    }
}
