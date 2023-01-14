using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.DebugUI
{
    internal class DebugUIImpl : IDebugUI, IDebugTree
    {
        public bool IsVisible { get; private set; }

        public IEnumerable<string> Categories => _tree.Keys;
        public IEnumerable<IDebugItem> GetItems(string category)
        {
            return _tree[category].AsEnumerable<IDebugItem>();
        }


        IView _view;
        Input _previousInputState = new Input();
        Dictionary<string, SortedSet<Item>> _tree = new Dictionary<string, SortedSet<Item>>();
        bool _treeDirty = false;
        bool _treeValuesDirty = false;

        // Selection: category (horizontal)
        KeyValuePair<string, SortedSet<Item>> _currentSet;
        WrappingNavigator _horizontalNav = new WrappingNavigator(0);

        // Selection: items (vertical)
        List<int> _selectableItems = new List<int>();
        WrappingNavigator _verticalNav = new WrappingNavigator(0);
        int _currentItemIndex = -1;

        // Selection: item value (value)
        ValueNavigator _valueNav = new ValueNavigator(null);


        public void SetView(IView view)
        {
            _view = view;
            _view.UpdateView(this);
        }

        public void Show()
        {
            Debug.Assert(IsVisible == false);

            if (_view == null)
            {
                Debug.LogError("DebugUI can not be shown as there is no IView configured.");
            }
            else
            {
                IsVisible = true;
                OnShow?.Invoke();
                _view.UpdateView(this);
                _view.Show();
            }
        }

        public event Action OnShow;

        public void Hide()
        {
            Debug.Assert(IsVisible == true);
            IsVisible = false;

            _view?.Hide();
        }

        public void Update(Input inputState)
        {
            if (!IsVisible)
                return;

            // Update all items
            foreach (var pair in _tree)
            {
                var set = pair.Value;
                foreach (var item in set)
                {
                    var valueBefore = item.DisplayValue;
                    item.OnUpdate();
                    if (string.Compare(valueBefore, item.DisplayValue) != 0)
                        _treeValuesDirty |= true;
                }
            }

            // Update navigation
            UpdateNavigation(inputState, _previousInputState);

            // Update UI
            _treeValuesDirty |= !_previousInputState.Equals(inputState); // Consider tree dirty on input change
            if (_treeDirty || _treeValuesDirty)
            {
                //Debug.Log("Updating DebugUI");
                if (_treeDirty)
                    OnSelectCategory(_horizontalNav.Position); // Refresh category in case new elements were added
                _view?.UpdateView(this);
                _treeDirty = false;
                _treeValuesDirty = false;
            }

            _previousInputState = inputState;

        }

        void OnSelectCategory(int categoryIndex)
        {
            //Debug.Log($"OnSelectCategory({categoryIndex})");
            _currentSet = _tree.ElementAt(categoryIndex);
            
            _selectableItems.Clear();
            {
                int index = 0;
                foreach (var item in _currentSet.Value)
                {
                    if (item.HasActivation || item.HasSwitching)
                        _selectableItems.Add(index);
                    ++index;
                }
            }

            var selectableIndex = _selectableItems.Count > 0 ? _selectableItems[0] : -1;
            OnSelectItem(selectableIndex);

            if (_horizontalNav.Count != _tree.Keys.Count)
                _horizontalNav.Reset(_tree.Keys.Count);
            _verticalNav.Reset(_selectableItems.Count);
        }

        void OnSelectItem(int itemIndex)
        {
            //Debug.Log($"OnSelectItem({itemIndex})");
            _currentItemIndex = itemIndex;

            if (itemIndex == -1)
            {
                _valueNav.Reset(null);
            }
            else
            {
                var item = _currentSet.Value.ElementAt(_currentItemIndex);
                _valueNav.Reset(item);
            }
        }

        public void UpdateNavigation(Input current, Input previous)
        {
            if (string.IsNullOrWhiteSpace(_currentSet.Key))
            {
                if (_tree.Count > 0)
                {
                    // Pick the first category if none were selected
                    OnSelectCategory(0);
                }
                else
                {
                    return;
                }
            }

            // Horizontal
            int horizontalDirection = current.categoryLeft ? -1 : current.categoryRight ? 1 : 0;
            if (_horizontalNav.Move(horizontalDirection))
            {
                OnSelectCategory(_horizontalNav.Position);
            }

            // Vertical
            int verticalDirection = current.up ? -1 : current.down ? 1 : 0;
            if (_verticalNav.Move(verticalDirection))
            {
                var index = _selectableItems[_verticalNav.Position];
                OnSelectItem(index);
            }

            // Value
            if (_currentItemIndex != -1)
            {
                if (_valueNav.Item.HasSwitching)
                {
                    int valueDirection = current.left ? -1 : current.right ? 1 : 0;
                    _valueNav.Move(valueDirection);
                }

                if (_valueNav.Item.HasActivation)
                {
                    if (current.ok && !previous.ok)
                        _valueNav.Item.OnActivate();
                }
            }

            _view?.UpdateSelection(_currentSet.Key, _currentItemIndex);
        }

        public void SetExtraInfoText(string text)
        {
            _view?.UpdateExtraInfo(text);
        }

        public void Print(string message, Verbosity verbosity)
        {
            _view?.PushLog(message, verbosity);

            // Trigger a callback in case users want to further handle the message
            OnPrint?.Invoke(message, verbosity);
        }

        public event Action<string, Verbosity> OnPrint;

        private void Register(Item item, string category)
        {
            SortedSet<Item> set;
            if (!_tree.TryGetValue(category, out set))
            {
                set = new SortedSet<Item>(new Item.PriorityComparer());
                _tree.Add(category, set);
            }

            set.Add(item);
            _treeDirty |= true;
        }

        public void Unregister(IDebugItem item)
        {
            foreach (var pair in _tree)
            {
                if (pair.Value.Remove((Item)item))
                {
                    _treeDirty |= true;
                    if (pair.Value.Count == 0)
                        _tree.Remove(pair.Key);
                    return;
                }
            }

            Debug.LogError($"DebugUI.Unregister failed to find item {item.Label}");
        }

        public IDebugItem RegisterBool(string label, string category, Func<bool> getValue)
        {
            var item = new ItemBoolViewer(label);
            item.GetValue = getValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterInt(string label, string category, Func<int> getValue)
        {
            var item = new ItemIntegerViewer(label);
            item.GetValue = getValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterFloat(string label, string category, Func<double> getValue)
        {
            var item = new ItemFloatViewer(label);
            item.GetValue = getValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterObject(string label, string category, Func<object> getValue)
        {
            var item = new ItemObjectViewer(label);
            item.GetValue = getValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterAction(string label, string category, Action onPress)
        {
            var item = new ItemAction(label);
            item.OnPress = onPress;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterBool(string label, string category, Func<bool> getValue, Action<bool> setValue)
        {
            var item = new ItemBool(label);
            item.GetValue = getValue;
            item.SetValue = setValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterInt(string label, string category, Func<int> getValue, Action<int> setValue, int step = 1, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            Debug.Assert(step > 0, "Step must be a positive value");

            var item = new ItemInteger(label);
            item.GetValue = getValue;
            item.SetValue = setValue;
            item.Step = step;
            item.MinValue = minValue;
            item.MaxValue = maxValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterFloat(string label, string category, Func<double> getValue, Action<double> setValue, double step, double minValue, double maxValue)
        {
            Debug.Assert(step > 0.0f, "Step must be a positive value");

            var item = new ItemFloat(label);
            item.GetValue = getValue;
            item.SetValue = setValue;
            item.Step = step;
            item.MinValue = minValue;
            item.MaxValue = maxValue;

            Register(item, category);

            return item;
        }

        public IDebugItem RegisterEnum(string label, string category, Type enumType, Func<Enum> getValue, Action<Enum> setValue)
        {
            var item = new ItemEnum(label, enumType);
            item.GetValue = getValue;
            item.SetValue = setValue;

            Register(item, category);

            return item;
        }
    }
}
