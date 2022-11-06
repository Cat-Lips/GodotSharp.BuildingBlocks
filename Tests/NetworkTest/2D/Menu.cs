using System.Diagnostics;
using FastEnumUtility;
using Godot;
using GodotSharp.BuildingBlocks;
using Humanizer;

namespace NetworkTest
{
    [SceneTree]
    public partial class Menu : CanvasLayer
    {
        private readonly Settings<Menu> settings = new();

        [Notify]
        public NetworkState NetworkState
        {
            get => _networkState.Get();
            set => _networkState.Set(value);
        }

        public void SetClientStatus(StatusType status, string message)
            => SetStatus(ClientStatus, status, message);

        public void SetServerStatus(StatusType status, string message)
            => SetStatus(ServerStatus, status, message);

        public void Initialise(
            Action<int> startServerAction, Action stopServerAction,
            Action<string, int> createClientAction, Action closeClientAction)
        {
            StartServer.Pressed += () => startServerAction(ServerPort.Value);
            StopServer.Pressed += stopServerAction;

            CreateClient.Pressed += () => createClientAction(ConnectAddress.Text, ConnectPort.Value);
            CloseClient.Pressed += closeClientAction;
        }

        public string GetPlayerName() => PlayerName.Text;
        public Color GetPlayerColor() => PlayerColor.Color;
        public AvatarType GetPlayerAvatar() => (AvatarType)PlayerAvatar.Selected;

        public Menu()
            => LogStart();

        [GodotOverride]
        private void OnReady()
        {
            var myMenus = new[] { GameMenu, ClientMenu, ServerMenu, PlayerMenu };
            var windowTitle = GetWindow().Title;

            ShrinkMenus();
            InitAnimation();
            InitPlayerMenu();
            InitNetworkMenus();

            Quit.Pressed += QuitGame;
            Panel.PreSortChildren += ShrinkPanel;
            NetworkStateChanged += SetWindowTitle;
            NetworkStateChanged += InitNetworkMenus;

            ServerAddress.Text = Network.GetLocalAddress();

            void InitAnimation()
            {
                Debug.Assert(myMenus.All(x => MenuLabel(x).MouseFilter is not Control.MouseFilterEnum.Ignore));
                myMenus.ForEach(x => MenuLabel(x).MouseEntered += () => ShowAndShrink(x, myMenus.Except(x).ToArray()));
                Panel.MouseExited += ShrinkOnExit;

                void ShowAndShrink(Control show, params Control[] shrink)
                {
                    ShowMenuItems(show);
                    ShrinkMenuItems(shrink);
                }

                void ShrinkOnExit()
                {
                    var mouseInPanel = Panel.GetRect()
                        .HasPoint(GetViewport().GetMousePosition());
                    if (mouseInPanel) return;
                    ShrinkMenus();
                }
            }

            void InitPlayerMenu()
            {
                FastEnum.GetNames<AvatarType>().ForEach(x => PlayerAvatar.AddItem(x.Humanize()));

                PlayerName.Text = settings.Get<string>(PlayerMenu, PlayerName);
                PlayerColor.Color = settings.Get<Color>(PlayerMenu, PlayerColor);
                PlayerAvatar.Selected = GetAvatarAsIndex();

                PlayerName.FocusExited += () => { SetPlayerNameDefault(); settings.Set(PlayerMenu, PlayerName, PlayerName.Text); };
                PlayerColor.ColorChanged += x => settings.Set(PlayerMenu, PlayerColor, x);
                PlayerAvatar.ItemSelected += x => SetAvatarAsString(x);

                SetPlayerNameDefault();
                SetPlayerColorDefault();

                int GetAvatarAsIndex()
                    => FastEnum.TryParse<AvatarType>(settings.Get<string>(PlayerMenu, PlayerAvatar), out var avatarEnum) ? (int)avatarEnum : 0;

                void SetAvatarAsString(long avatarIndex)
                    => settings.Set(PlayerMenu, PlayerAvatar, ((AvatarType)avatarIndex).FastToString());

                void SetPlayerNameDefault()
                {
                    if (string.IsNullOrWhiteSpace(PlayerName.Text) || PlayerName.Text == PlayerName.PlaceholderText)
                        PlayerName.Text = System.Environment.MachineName;
                }

                void SetPlayerColorDefault()
                {
                    if (PlayerColor.Color == Colors.Black)
                        PlayerColor.Color = DefaultColors.GetRandom();
                }
            }

            void InitNetworkMenus()
            {
                switch (NetworkState)
                {
                    case NetworkState.ClientConnecting:
                        ServerMenu.Visible = false;

                        CreateClient.Visible = false;
                        CloseClient.Visible = true;
                        ConnectAddress.Editable = false;
                        ConnectPort.Editable = false;

                        PlayerName.Editable = false;
                        PlayerColor.Enabled(false);
                        PlayerAvatar.Enabled(false);

                        break;

                    case NetworkState.ServerStarting:
                        ClientMenu.Visible = false;

                        StartServer.Visible = false;
                        StopServer.Visible = true;
                        ServerPort.Editable = false;

                        PlayerName.Editable = false;
                        PlayerColor.Enabled(false);
                        PlayerAvatar.Enabled(false);

                        break;

                    case NetworkState.None:
                        ClientMenu.Visible = true;
                        ServerMenu.Visible = true;

                        CreateClient.Visible = true;
                        CloseClient.Visible = false;
                        ConnectAddress.Editable = true;
                        ConnectPort.Editable = true;

                        StartServer.Visible = true;
                        StopServer.Visible = false;
                        ServerPort.Editable = true;

                        SetStatus(ClientStatus, StatusType.Info, "(not connected)");
                        SetStatus(ServerStatus, StatusType.Info, "(not running)");

                        PlayerName.Editable = true;
                        PlayerColor.Enabled(true);
                        PlayerAvatar.Enabled(true);

                        break;
                }
            }

            Label MenuLabel(Control menu)
                => menu.GetChild<Label>(0);

            Container MenuItems(Control menu)
                => menu.GetChild<Container>(1);

            void ShowMenuItems(Control menu)
                => MenuItems(menu).Visible = true;

            void ShrinkMenuItems(params Control[] menus)
                => menus.ForEach(x => MenuItems(x).Visible = false);

            void ShrinkMenus()
                => ShrinkMenuItems(myMenus);

            void ShrinkPanel()
                => Panel.Size = Vector2.Zero;

            void SetWindowTitle()
            {
                GetWindow().Title = $"{windowTitle}{Extra()}";

                string Extra() => NetworkState switch
                {
                    NetworkState.ClientConnected => " - CLIENT",
                    NetworkState.ServerStarted => " - SERVER",
                    _ => "",
                };
            }

            void QuitGame()
            {
                GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
                GetTree().Quit();

                Log.Debug("Have a nice day!");
            }
        }

        [GodotOverride]
        private void OnNotification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                Input.MouseMode = default;
                LogEnd();
            }
        }

        public override partial void _Ready();
        public override partial void _Notification(int what);

        private static readonly Color WarnColor = Colors.Yellow;
        private static readonly Color ErrorColor = Colors.Red;
        private static readonly Color SuccessColor = Colors.Green;

        private static void SetStatus(Label statusLabel, StatusType status, string message)
        {
            SetStatus(statusLabel, message, StatusColor());

            Color? StatusColor() => status switch
            {
                StatusType.Warn => WarnColor,
                StatusType.Error => ErrorColor,
                StatusType.Success => SuccessColor,
                _ => null,
            };
        }

        private static void SetStatus(Label statusLabel, string message, Color? color = null)
        {
            statusLabel.Text = message;

            if (color is null)
                statusLabel.ResetFontColor();
            else
                statusLabel.SetFontColor(color.Value);
        }

        private static void LogStart() => Log.Debug(">>> GAME START <<<");
        private static void LogEnd() => Log.Debug(">>> GAME EXIT <<<");

        private static readonly Color[] DefaultColors =
        {
            new(1, 0, 0), // Red
            new(0, 1, 0), // Green
            new(0, 0, 1), // Blue
            new(1, 1, 0), // Yellow (red+green)
            new(0, 1, 1), // Cyan (green+blue)
            new(1, 0, 1), // Purple (red+blue)
        };
    }
}
