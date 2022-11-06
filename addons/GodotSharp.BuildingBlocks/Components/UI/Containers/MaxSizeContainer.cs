using Godot;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class MaxSizeContainer : ScrollContainer
    {
        [Export, Notify]
        public Vector2 MaxSize
        {
            get => _maxSize.Get();
            set => _maxSize.Set(value);
        }

        public MaxSizeContainer()
        {
            _maxSize.Changed += ResetSize;
            ChildEnteredTree += OnChildEntered;
            ChildExitingTree += OnChildExiting;
            OnChildSizeChanged();

            void OnChildEntered(Node x)
            {
                if (x is Control c)
                    c.Resized += OnChildSizeChanged;
                this.CallDeferred(OnChildSizeChanged);
            }

            void OnChildExiting(Node x)
            {
                if (x is Control c)
                    c.Resized -= OnChildSizeChanged;
                this.CallDeferred(OnChildSizeChanged);
            }

            void OnChildSizeChanged()
            {
                var content = this.GetChildren<Control>().SingleOrDefault();

                var preferredSize = content?.Size ?? Vector2.Zero;
                if (MaxSize.X > 0 && preferredSize.X > MaxSize.X) preferredSize.X = MaxSize.X;
                if (MaxSize.Y > 0 && preferredSize.Y > MaxSize.Y) preferredSize.Y = MaxSize.Y;
                CustomMinimumSize = preferredSize;
            }
        }
    }
}
