using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DynamicData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ScratchDL.GUI
{
    public abstract class ProgramOptionView<T> : IProgramOptionView where T : ProgramOption
    {
        public readonly T ModeObject;

        ProgramOption IProgramOptionView.ModeObject => ModeObject;

        public ProgramOptionView(T modeObject) => ModeObject = modeObject;

        public abstract void Setup(StackPanel controlsPanel);

        Type IProgramOptionView.ModeType => typeof(T);

        void IProgramOptionView.Setup(StackPanel controlPanel) => Setup(controlPanel);

        public virtual string Column1 => "ID";
        public virtual string Column2 => "Name";
        public virtual string Column3 => "Creator";

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

        protected CheckBox CreateAndAddCheckbox(string propertyName, StackPanel controlsPanel)
        {
            var name = Helpers.Prettify(propertyName + "Field");
            var text = Helpers.Prettify(propertyName);
            var property = ObtainProperty<bool>(propertyName);

            var result = CreateCheckbox(name, text, GetMemberValue<bool>(property), v => SetMemberValue(property, v));
            controlsPanel.Children.Add(result);
            return result;
        }

        private MemberInfo ObtainProperty<FieldType>(string propertyName)
        {
            MemberInfo? property = ModeObject.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !((PropertyInfo)property).PropertyType.IsAssignableTo(typeof(FieldType)))
            {
                property = ModeObject.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            }

            if (property == null)
            {
                throw new Exception($"The property or field {propertyName} does not exist or is private on type {ModeObject.GetType().FullName}");
            }

            if (property is FieldInfo field && !field.FieldType.IsAssignableTo(typeof(FieldType)))
            {
                throw new Exception($"The property or field {propertyName} cannot be casted to type {typeof(FieldType).FullName}");
            }

            return property;
        }

        private FieldType GetMemberValue<FieldType>(MemberInfo member)
        {
            if (member is PropertyInfo property)
            {
                return (FieldType)property.GetValue(ModeObject)!;
            }
            else if (member is FieldInfo field)
            {
                return (FieldType)field.GetValue(ModeObject)!;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void SetMemberValue<FieldType>(MemberInfo member, FieldType value)
        {
            if (member is PropertyInfo property)
            {
                property.SetValue(ModeObject,value);
            }
            else if (member is FieldInfo field)
            {
                field.SetValue(ModeObject, value);
            }
            else
            {
                throw new NotSupportedException();
            }
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

        protected (StackPanel panel, TextBox textbox) CreateTextBox(string name, string label, string defaultText, Action<string> onTextUpdate)
        {
            var textBox = new TextBox();
            textBox.Name = name;
            textBox.Text = defaultText;
            textBox.KeyUp += (sender, e) =>
            {
                onTextUpdate?.Invoke(textBox.Text);
            };
            textBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

            var panel = new StackPanel();
            panel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            panel.Orientation = Avalonia.Layout.Orientation.Horizontal;
            var labelBlock = new TextBlock();
            labelBlock.Text = label;
            labelBlock.Padding = new Avalonia.Thickness(5,0,5,0);
            labelBlock.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            panel.Children.Add(labelBlock);
            panel.Children.Add(textBox);
            return (panel, textBox);
        }

        protected (StackPanel panel, TextBox textbox) CreateAndAddTextBox(string propertyName, StackPanel controlsPanel)
        {
            var name = Helpers.Prettify(propertyName + "Field");
            var text = Helpers.Prettify(propertyName);
            var property = ObtainProperty<string>(propertyName);

            var result = CreateTextBox(name, text, GetMemberValue<string>(property), v => SetMemberValue(property, v));
            controlsPanel.Children.Add(result.panel);
            return result;
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

        protected TextBox CreateAndAddNumberTextBox(string propertyName, StackPanel controlsPanel)
        {
            var name = Helpers.Prettify(propertyName + "Field");
            var text = Helpers.Prettify(propertyName);
            var property = ObtainProperty<int>(propertyName);

            var result = CreateNumberTextBox(name, GetMemberValue<int>(property), v => SetMemberValue(property, v));
            controlsPanel.Children.Add(result);
            return result;
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
                onSelectionChanged((string)comboBox.SelectedItem!,comboBox.SelectedIndex);
            };

            return comboBox;
        }

        protected ComboBox CreateEnumComboBox<EnumType>(string name, EnumType defaultValue, Action<EnumType> onSelectionChange) where EnumType : struct, Enum
        {
            return CreateEnumComboBox(name, defaultValue,onSelectionChange,Enum.GetValues<EnumType>());
        }

        protected ComboBox CreateEnumComboBox<EnumType>(string name, EnumType defaultValue, Action<EnumType> onSelectionChange, IEnumerable<EnumType> possibleValues) where EnumType : struct, Enum
        {
            return CreateEnumComboBox(name, defaultValue, onSelectionChange, possibleValues);
        }

        protected ComboBox CreateEnumComboBox(string name, Enum defaultValue, Action<Enum> onSelectionChange, IEnumerable<Enum> possibleValues)
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

        protected ComboBox CreateAndAddEnumComboBox(string propertyName, StackPanel controlsPanel)
        {
            var name = Helpers.Prettify(propertyName + "Field");
            var text = Helpers.Prettify(propertyName);
            var property = ObtainProperty<Enum>(propertyName);

            var enumValue = GetMemberValue<Enum>(property);


            var array = Enum.GetValues(enumValue.GetType());
            var enumArray = new Enum[array.Length];
            for (int i = 0; i < enumArray.Length; i++)
            {
                enumArray[i] = (Enum)array.GetValue(i)!;
            }

            var result = CreateEnumComboBox(name, enumValue, v => SetMemberValue(property, v), enumArray);
            controlsPanel.Children.Add(result);
            return result;
        }
    }
}
