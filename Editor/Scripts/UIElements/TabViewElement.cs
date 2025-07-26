using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LDtkLevelManagerEditor
{
    public class TabViewElement : VisualElement
    {
        public static string LastUsedTab = "Main";

        private const string TemplateName = "TabViewTemplate";
        private const string TabButtonClassName = "tab-button";
        private const string TabButtonSelectedClassName = "tab-button--selected";

        private TemplateContainer _containerMain;
        private VisualElement _containerButtons;
        private VisualElement _containerViews;

        private string _activeTab;

        private Dictionary<string, VisualElement> _buttons = new();
        private Dictionary<string, VisualElement> _views = new();

        public TabViewElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").Instantiate();

            _containerButtons = _containerMain.Q<VisualElement>("container-tab-buttons");
            _containerViews = _containerMain.Q<VisualElement>("container-views");

            Add(_containerMain);
        }

        public void AddTab(string name, VisualElement view)
        {
            GenerateButton(name);
            view.SetEnabled(false);
            _views.Add(name, view);

            if (name == LastUsedTab)
            {
                SelectTab(name);
            }
        }

        public void SelectTab(string name)
        {
            if (!string.IsNullOrEmpty(_activeTab))
            {
                DismissTab(_activeTab);
            }

            _activeTab = name;

            ActivateTab(_activeTab);
        }

        private void GenerateButton(string name)
        {
            VisualElement buttonElement = new();
            Label label = new()
            {
                text = name
            };

            buttonElement.Add(label);
            buttonElement.AddToClassList(TabButtonClassName);
            _containerButtons.Add(buttonElement);
            _buttons.Add(name, buttonElement);

            buttonElement.RegisterCallback<ClickEvent>(e => SelectTab(name));
        }

        private void ActivateTab(string name)
        {
            if (_buttons.TryGetValue(name, out VisualElement buttonElement))
            {
                buttonElement.AddToClassList(TabButtonSelectedClassName);
            }

            if (_views.TryGetValue(name, out VisualElement viewElement))
            {
                viewElement.SetEnabled(true);
                _containerViews.Add(viewElement);
            }

            LastUsedTab = name;
        }

        private void DismissTab(string name)
        {
            if (_buttons.TryGetValue(name, out VisualElement buttonElement))
            {
                buttonElement.RemoveFromClassList(TabButtonSelectedClassName);
            }

            if (_views.TryGetValue(name, out VisualElement viewElement))
            {
                viewElement.SetEnabled(false);
                _containerViews.Remove(viewElement);
            }
        }
    }
}