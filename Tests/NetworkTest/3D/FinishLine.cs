using System.Diagnostics;
using Godot;
using static Godot.MultiplayerApi;

namespace NetworkTest
{
    [Tool, SceneTree]
    public partial class FinishLine : Node3D
    {
        private const string SnipAnim = "PhysicsRibbon|Snip";

        [Rpc(RpcMode.Authority, CallLocal = true)]
        public void PlaySnipAnim()
        {
            Ribbon.Visible = false;
            AnimationPlayer.Play(SnipAnim);
        }

        public void OnCapture(Action action)
        {
            AnimationPlayer.AnimationStarted += OnAnimStart;

            void OnAnimStart(StringName anim)
            {
                AnimationPlayer.AnimationStarted -= OnAnimStart;
                Debug.Assert(anim == SnipAnim);
                action?.Invoke();
            };
        }

        public void OnCaptureComplete(Action action)
        {
            AnimationPlayer.AnimationFinished += OnAnimEnd;

            void OnAnimEnd(StringName anim)
            {
                AnimationPlayer.AnimationFinished -= OnAnimEnd;
                Debug.Assert(anim == SnipAnim);
                action?.Invoke();
            };
        }

        [GodotOverride]
        private void OnReady()
        {
            ExpandCaptureArea();
            if (Engine.IsEditorHint()) return;
            InitialiseDataSync();

            void ExpandCaptureArea()
            {
                var poles = Poles.GetAabb();
                var area = Ribbon.GetAabb();

                area = area.Expand(new(0, poles.End.Y, 0));
                area = area.Expand(new(0, poles.Position.Y, 0));
                RibbonShape.Position = area.GetCenter();
                ((BoxShape3D)RibbonShape.Shape).Size = area.Size;
            }

            void InitialiseDataSync()
            {
                _.DataSync.Add(Position, "position"); // No sync required
                _.DataSync.Add(Rotation, "rotation"); // No sync required
            }
        }

        public override partial void _Ready();
    }
}
