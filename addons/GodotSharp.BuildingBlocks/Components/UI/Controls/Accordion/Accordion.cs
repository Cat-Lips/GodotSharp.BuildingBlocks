using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class Accordion : GridContainer
    {
        [Export] public bool FixWidths { get; set; } = true;

        public void Clear()
            => this.RemoveChildren(free: true);

        public void AddGroup(string name, params (Label Label, Control Value)[] controls)
        {
            var title = App.InstantiateScene<AccordionTitle>(name);
            var blank = UI.Blank(name);

            AddChild(title);
            AddChild(blank);

            controls.ForEach(x => { AddChild(x.Label); AddChild(x.Value); });

            if (FixWidths)
                title.Toggle.Pressed += FixWidthOnFirstToggle;

            title.Toggle.Pressed += () => controls.ForEach(x =>
            {
                x.Label.Visible = title.Toggle.ButtonPressed;
                x.Value.Visible = title.Toggle.ButtonPressed;
            });

            void FixWidthOnFirstToggle()
            {
                title.Toggle.Pressed -= FixWidthOnFirstToggle;

                var maxLabelWidth = title.Size.X;
                var maxValueWidth = blank.Size.X;

                controls.ForEach(x =>
                {
                    if (maxLabelWidth < x.Label.Size.X) maxLabelWidth = x.Label.Size.X;
                    if (maxValueWidth < x.Value.Size.X) maxValueWidth = x.Value.Size.X;
                });

                title.CustomMinimumSize = new(maxLabelWidth, 0);
                blank.CustomMinimumSize = new(maxValueWidth, 0);
            }
        }
    }
}
