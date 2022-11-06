using Godot;
using Humanizer;
using static Godot.LineEdit;
using static Godot.Range;
using Range = Godot.Range;

namespace GodotSharp.BuildingBlocks
{
    public static class UI
    {
        private const float DefaultMin = -9999999999;
        private const float DefaultMax = -DefaultMin;

        #region Label

        public static Label Label(string name, string text)
            => new() { Name = name, Text = text, VerticalAlignment = VerticalAlignment.Center };

        public static Label Label(string prop)
            => Label($"{prop}Label", prop.Humanize(LetterCasing.Title));

        public static Label Blank(string name)
            => Label($"{name}Blank", "");

        #endregion

        #region LineEdit

        public static LineEdit LineEdit(string name, string text = "")
            => new() { Name = name, Text = text, CaretBlink = true };

        public static LineEdit LineEdit(string name, string text, TextChangedEventHandler onTextChanged)
        {
            var edit = LineEdit(name, text);
            edit.TextChanged += onTextChanged;
            return edit;
        }

        public static LineEdit LineEdit(GodotObject source, string prop)
            => LineEdit(prop, (string)source.Get(prop), x => source.Set(prop, x));

        #endregion

        #region CheckButton

        public static CheckButton CheckButton(string name, bool pressed = false)
            => new() { Name = name, ButtonPressed = pressed };

        public static CheckButton CheckButton(string name, bool pressed, Action<bool> onToggle)
        {
            var edit = CheckButton(name, pressed);
            edit.Pressed += () => onToggle(edit.ButtonPressed);
            return edit;
        }

        public static CheckButton CheckButton(GodotObject source, string prop)
            => CheckButton(prop, (bool)source.Get(prop), x => source.Set(prop, x));

        #endregion

        #region OptionButton

        public static OptionButton OptionButton(string name, params string[] options)
        {
            var edit = new OptionButton() { Name = name };
            SetOptions();
            return edit;

            void SetOptions()
            {
                for (var idx = 0; idx < options.Length; ++idx)
                {
                    var option = options[idx];
                    edit.AddItem(option.Humanize(LetterCasing.Title));
                    edit.SetItemMetadata(idx, option);
                }
            }
        }

        public static OptionButton OptionButton(string name, string csvOptions, Action<int> onSelect)
        {
            var edit = OptionButton(name, csvOptions.Split(","));
            edit.ItemSelected += x => onSelect((int)x);
            return edit;
        }

        public static OptionButton OptionButton(string name, string csvOptions, Action<string> onSelect)
        {
            var edit = OptionButton(name, csvOptions.Split(","));
            edit.ItemSelected += x => onSelect(edit.GetItemMetadata((int)x).AsString());
            return edit;
        }

        public static OptionButton OptionButton(GodotObject source, string prop, string hint)
        {
            var edit = OptionButton(prop, hint, (int x) => source.Set(prop, x));
            edit.Selected = (int)source.Get(prop);
            return edit;
        }

        #endregion

        #region SpinBox

        public static SpinBox SpinBox(string name, bool rounded = true)
            => new() { Name = name, Rounded = rounded, MinValue = DefaultMin, MaxValue = DefaultMax, UpdateOnTextChanged = true };

        public static SpinBox SpinBox(string name, bool rounded, ValueChangedEventHandler onValueChanged)
        {
            var edit = SpinBox(name, rounded);
            edit.ValueChanged += onValueChanged;
            return edit;
        }

        public static SpinBox SpinBox(GodotObject source, string prop, bool rounded, string hint)
        {
            var value = (float)source.Get(prop);
            var edit = SpinBox(prop, rounded, x => source.Set(prop, x));
            edit.SetRangeFromHint(hint);
            edit.Value = value;
            return edit;
        }

        #endregion

        #region Slider

        public static DecoratedSlider Slider(string name, bool rounded = false)
        {
            var edit = App.InstantiateScene<DecoratedSlider>(name);
            edit.Slider.Rounded = rounded;
            edit.Slider.MinValue = DefaultMin;
            edit.Slider.MaxValue = DefaultMax;
            return edit;
        }

        public static DecoratedSlider Slider(string name, bool rounded, ValueChangedEventHandler onValueChanged)
        {
            var edit = Slider(name, rounded);
            edit.Slider.ValueChanged += onValueChanged;
            return edit;
        }

        public static DecoratedSlider Slider(GodotObject source, string prop, bool rounded, string hint = null)
        {
            var value = (float)source.Get(prop);
            var edit = Slider(prop, rounded, x => source.Set(prop, x));
            edit.Slider.SetRangeFromHint(hint);
            edit.Slider.Value = value;
            return edit;
        }

        #endregion

        #region Utils

        private static void SetRangeFromHint<T>(this T edit, string hint) where T : Range
        {
            if (string.IsNullOrWhiteSpace(hint)) return;

            var dblCount = -1;
            foreach (var part in hint.Split(","))
            {
                if (part is "or_greater") edit.AllowGreater = true;
                else if (part is "or_less") edit.AllowLesser = true;
                else if (float.TryParse(part, out var value)) SetValue(value);
                else throw new NotImplementedException($"Unrecognised {part} in {hint}");
            }

            void SetValue(float value)
            {
                switch (++dblCount)
                {
                    case 0: edit.MinValue = value; break;
                    case 1: edit.MaxValue = value; break;
                    case 2: edit.Step = value; break;
                    default: throw new NotImplementedException($"Unrecognised {value} in {hint}");
                }
            }
        }

        public static IEnumerable<(Label Label, Control Value)> GetEditControls(this Resource source)
        {
            var tt = source as ICodeComments;

            foreach (var pInfo in source.GetPropertyList())
            {
                var usage = pInfo["usage"].As<PropertyUsageFlags>();
                if (usage.HasFlag(PropertyUsageFlags.ScriptVariable))
                {
                    var name = pInfo["name"].AsString();
                    var classType = pInfo["class_name"].AsStringName();
                    var godotType = pInfo["type"].As<Variant.Type>();
                    var hintType = pInfo["hint"].As<PropertyHint>();
                    var hint = pInfo["hint_string"].AsString();
                    var tooltip = tt?.GetComment(name);

                    if (hintType is PropertyHint.Enum)
                        yield return (LabelTT(), OptionButton(source, name, hint));
                    else if (hintType is PropertyHint.Range)
                        yield return (LabelTT(), Slider(source, name, rounded: godotType is Variant.Type.Int, hint));
                    else if (godotType is Variant.Type.Int)
                        yield return (LabelTT(), SpinBox(source, name, rounded: true, hint));
                    else if (godotType is Variant.Type.Bool)
                        yield return (LabelTT(), CheckButton(source, name));
                    else if (godotType is Variant.Type.Float)
                        yield return (LabelTT(), SpinBox(source, name, rounded: false, hint));
                    else if (godotType is Variant.Type.String or Variant.Type.StringName)
                        yield return (LabelTT(), LineEdit(source, name));

                    Label LabelTT()
                    {
                        var label = Label(name);
                        label.TooltipText = tooltip;
                        label.MouseFilter = Control.MouseFilterEnum.Pass;
                        return label;
                    }
                }
            }
        }

        public static IEnumerable<Resource> GetSubResources(this Resource source)
        {
            foreach (var pInfo in source.GetPropertyList())
            {
                var usage = pInfo["usage"].As<PropertyUsageFlags>();
                if (usage.HasFlag(PropertyUsageFlags.ScriptVariable))
                {
                    var name = pInfo["name"].AsString();
                    var classType = pInfo["class_name"].AsStringName();
                    var godotType = pInfo["type"].As<Variant.Type>();

                    if (godotType is Variant.Type.Object && classType == "Resource")
                        yield return (Resource)source.Get(name);
                }
            }
        }

        public static void SetMenuItems(this PopupMenu source, params string[] menuItems)
        {
            while (source.ItemCount is not 0)
                source.RemoveItem(0);

            for (var idx = 0; idx < menuItems.Length; ++idx)
            {
                var item = menuItems[idx];
                source.AddItem(GetTitle(item));
                source.SetItemMetadata(idx, item);
            }

            static string GetTitle(string item)
                => (item.EndsWith("...") ? item : item.Split('.', 2).First()).Humanize(LetterCasing.Title);
        }

        public static void OnItemSelected(this PopupMenu source, Action<int> onItemSelected)
            => source.IndexPressed += idx => onItemSelected((int)idx);

        public static void OnItemSelected(this PopupMenu source, Action<string> onItemSelected)
            => source.IndexPressed += idx => onItemSelected(source.GetItemMetadata((int)idx).AsString());

        #endregion
    }
}
