using Avalonia.Controls;
using Avalonia.Input;
using DynamicData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ScratchDL.GUI
{
    public abstract class DownloadModeUI<T> : IDownloadModeUI where T : DownloadMode
    {
        public readonly T ModeObject;

        DownloadMode IDownloadModeUI.ModeObject => ModeObject;

        public DownloadModeUI(T modeObject) => ModeObject = modeObject;

        public abstract void Setup(StackPanel controlsPanel);

        Type IDownloadModeUI.ModeType => typeof(T);

        void IDownloadModeUI.Setup(StackPanel controlPanel) => Setup(controlPanel);

        protected CheckBox CreateCheckbox(string name, string text, bool defaultState, Action<bool> onValueChanged)
        {
            var checkbox = new CheckBox();
            checkbox.Name = name;
            checkbox.Content = text;
            checkbox.IsChecked = defaultState;
            checkbox.Checked += (sender, e) =>
            {
                onValueChanged(true);
            };

            checkbox.Unchecked += (sender, e) =>
            {
                onValueChanged(false);
            };

            return checkbox;
        }

        protected TextBox CreateTextBox(string name, string defaultText, Action<string> onTextUpdate)
        {
            var textBox = new TextBox();
            textBox.Name = name;
            textBox.Text = defaultText;
            textBox.KeyUp += (sender, e) =>
            {
                onTextUpdate?.Invoke(textBox.Text);
            };

            return textBox;
        }

        protected TextBox CreateNumberTextBox(string name, int defaultNumber, Action<int> onNumberUpdate, int minRange = int.MinValue, int maxRange = int.MaxValue)
        {
            string originalText = defaultNumber.ToString();
            TextBox textBox = null!;
            textBox = CreateTextBox(name, defaultNumber.ToString(),str =>
            {
                if (int.TryParse(textBox.Text, out var result))
                {
                    if (result > maxRange)
                    {
                        result = maxRange;
                    }
                    if (result < minRange)
                    {
                        result = minRange;
                    }
                    originalText = textBox.Text;
                    onNumberUpdate?.Invoke(result);
                }
                else
                {
                    textBox.Text = originalText;
                }
            });
            return textBox;
        }

        protected ComboBox CreateComboBox(string name, IEnumerable<string> values, int defaultIndex, Action<string, int> onSelectionChanged)
        {
            defaultIndex = Math.Clamp(defaultIndex, 0, values.Count() - 1);

            var comboBox = new ComboBox();
            comboBox.Name = name;
            comboBox.Items = values;
            comboBox.SelectedIndex = defaultIndex;
            comboBox.SelectionChanged += (sender, e) =>
            {
                onSelectionChanged((string)comboBox.SelectedItem,comboBox.SelectedIndex);
            };

            return comboBox;
        }

        protected ComboBox CreateEnumComboBox<EnumType>(string name, EnumType defaultValue, Action<EnumType> onSelectionChange) where EnumType : struct, Enum
        {
            return CreateEnumComboBox(name, defaultValue,onSelectionChange,Enum.GetValues<EnumType>());
        }

        protected ComboBox CreateEnumComboBox<EnumType>(string name, EnumType defaultValue, Action<EnumType> onSelectionChange, IEnumerable<EnumType> possibleValues) where EnumType : struct, Enum
        {
            var names = possibleValues.Select(v => v.ToString()).ToArray();
            var values = possibleValues.ToArray();

            var defaultName = defaultValue.ToString();

            var defaultIndex = Array.IndexOf(names, defaultName);

            var comboBox = new ComboBox();
            comboBox.Name = name;
            comboBox.Items = names.Select(n => Helpers.Prettify(n));
            comboBox.SelectedIndex = defaultIndex;
            comboBox.SelectionChanged += (sender, e) =>
            {
                onSelectionChange(values[comboBox.SelectedIndex]);
            };

            return comboBox;
        }

        /*protected EventHandler<KeyEventArgs> GetTextUpdateDelegate(TextBox textBox, Action<string> updateTextDelegate)
        {
            return (sender, e) =>
            {
                updateTextDelegate?.Invoke(textBox.Text);
            };
        }

        protected EventHandler<KeyEventArgs> GetNumberUpdateDelegate(TextBox textBox, Action<int> updateNumberDelegate, int minRange = int.MinValue, int maxRange = int.MaxValue)
        {
            var originalText = textBox.Text;
            return (sender, e) =>
            {
                if (int.TryParse(textBox.Text,out var result))
                {
                    if (result > maxRange)
                    {
                        result = maxRange;
                    }
                    if (result < minRange)
                    {
                        result = minRange;
                    }
                    originalText = textBox.Text;
                    updateNumberDelegate?.Invoke(result);
                }
                else
                {
                    textBox.Text = originalText;
                }
            };
        }*/
    }
}
