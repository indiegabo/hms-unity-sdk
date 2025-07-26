using System;
using UnityEngine;

[Serializable]
public class ObservableValue<T>
{
    // Event that will be triggered when the value changes
    public event Action<T> OnValueChanged;

    [SerializeField] private T _value;

    public T Value
    {
        get => _value;
        set
        {
            // Checks if the value has really changed
            if (!Equals(_value, value))
            {
                _value = value;
                // Triggers the event if there are subscribers
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    // Constructor for initialization
    public ObservableValue(T initialValue)
    {
        _value = initialValue;
    }

    // Default constructor
    public ObservableValue() { }
}
