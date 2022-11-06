using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class DecoratedSlider : Control
    {
        private float minValue;
        private float maxValue;

        [GodotOverride]
        private void OnReady()
        {
            minValue = (float)Slider.MinValue;
            maxValue = (float)Slider.MaxValue;

            Min.Text = Slider.MinValue.ToString();
            Max.Text = Slider.MaxValue.ToString();
            Value.Text = Math.Round(Slider.Value, 3).ToString();
            Slider.ValueChanged += x => Value.Text = Math.Round(x, 3).ToString();

            OnValueChanged(Slider.Value);
            Slider.ValueChanged += OnValueChanged;

            void OnValueChanged(double value)
            {
                SetMinValue();
                SetMaxValue();
                SetValueText();

                void SetMinValue()
                {
                    if (Slider.AllowLesser)
                    {
                        var v = (int)value - 1;
                        Min.Text = v < minValue
                            ? $"<<{Slider.MinValue = v}"
                            : $"<<{Slider.MinValue = minValue}";
                    }
                    else
                    {
                        Min.Text = $"{Slider.MinValue}";
                    }
                }

                void SetMaxValue()
                {
                    if (Slider.AllowGreater)
                    {
                        var v = (int)value + 1;
                        Max.Text = v > minValue
                            ? $"{Slider.MaxValue = v}>>"
                            : $"{Slider.MaxValue = maxValue}>>";
                    }
                    else
                    {
                        Max.Text = $"{Slider.MaxValue}";
                    }
                }

                void SetValueText()
                    => Value.Text = $"{Math.Round(value, 3)}";
            }
        }

        public override partial void _Ready();
    }
}
