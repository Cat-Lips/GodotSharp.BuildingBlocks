using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using static Godot.MultiplayerApi;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class DataSync : Node
    {
        private const bool log = false;

        private event Action Synchronized;

        [Export] public Node RootPath { get; set; }
        private Node Source => RootPath ?? GetParent();

        public void OnReady(Action action)
        {
            if (ready)
                this.CallDeferred(action);
            else
                Synchronized += OnSync;

            void OnSync()
            {
                Synchronized -= OnSync;
                action();
            }
        }

        public void OnSync(Action action)
            => Synchronized += action;

        private readonly HashSet<StringName> pingProps = new();
        public Action Add<TProperty>(TProperty _, [CallerArgumentExpression(nameof(_))] string property = null)
        {
            pingProps.Add(property);
            return OnDataChanged;

            void OnDataChanged()
            {
                if (IsMultiplayerAuthority())
                {
                    var value = Source.Get(property);
                    Log.If(log, $"Sending {Source.Name}.{property} as {value} from {Multiplayer.GetUniqueId()}]");
                    Rpc(nameof(RpcReceive), property, value);
                    OnDataReceived();
                }
            }
        }

        [Rpc(RpcMode.Authority)]
        private void RpcReceive(StringName property, Variant value)
        {
            Debug.Assert(Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority());

            Log.If(log, $"Receiving {Source.Name}.{property} as {value} from {Multiplayer.GetRemoteSenderId()}]");

            Source.Set(property, value);
            OnDataReceived();
        }

        [Rpc(RpcMode.AnyPeer)]
        private void RpcPing()
        {
            Debug.Assert(IsMultiplayerAuthority());

            Log.If(log, $"Receiving {Source.Name}.Ping from {Multiplayer.GetRemoteSenderId()}]");

            foreach (var property in pingProps)
                RpcId(Multiplayer.GetRemoteSenderId(), nameof(RpcReceive), property, Source.Get(property));
        }

        private bool ready;
        private bool dataReceived;
        private void OnDataReceived()
        {
            if (dataReceived) return;
            dataReceived = true;

            this.CallDeferred(() =>
            {
                ready = true;
                dataReceived = false;
                Synchronized?.Invoke();
            });
        }

        [GodotOverride]
        private void OnReady()
        {
            if (IsMultiplayerAuthority())
            {
                ready = true;
                return;
            }

            Log.If(log, $"Sending {Source.Name}.Ping from {Multiplayer.GetUniqueId()}]");
            RpcId(GetMultiplayerAuthority(), nameof(RpcPing));
        }

        public override partial void _Ready();
    }
}
