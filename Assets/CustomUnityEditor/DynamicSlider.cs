using System;
using UnityEditor;
using UnityEngine;

namespace NNG.CustomUnityInspector
{
    public class DynamicSliderAttribute : PropertyAttribute
    {
        public object startingField;
        public object lengthField;

        public DynamicSliderAttribute(object startingField, object lengthField)
        {
            this.startingField = startingField;
            this.lengthField = lengthField;
        }
    }

    [CustomPropertyDrawer(typeof(DynamicSliderAttribute))]
    public class DynamicSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            DynamicSliderAttribute sliderAttribute = (DynamicSliderAttribute)attribute;

            float startingValue = GetValue(property.serializedObject.targetObject, sliderAttribute.startingField);
            float lengthValue = GetValue(property.serializedObject.targetObject, sliderAttribute.lengthField);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int intValue = EditorGUI.IntSlider(position, label, (int)startingValue, 0, (int)lengthValue);
                property.intValue = intValue;
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                float floatValue = EditorGUI.Slider(position, label, startingValue, 0f, lengthValue);
                property.floatValue = floatValue;
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use int or float for DynamicSlider fields.");
            }

            EditorGUI.EndProperty();
        }

        private float GetValue(object targetObject, object field)
        {
            if (field is float)
                return (float)field;
            else if (field is int)
                return (int)field;
            else if (field is string)
            {
                System.Reflection.FieldInfo fieldInfo = targetObject.GetType().GetField((string)field);
                Debug.Log(targetObject.ToString());
                if (fieldInfo != null)
                {
                    object value = fieldInfo.GetValue(targetObject);
                    if (value is int)
                        return (int)value;
                    else if (value is float)
                        return (float)value;
                    else
                        Debug.LogWarning("DynamicSlider: Field " + field + " must be int or float.");
                }
                else
                {
                    Debug.LogWarning("DynamicSlider: Field " + field + " not found.");
                }
            }
            else
            {
                Debug.LogWarning("DynamicSlider: Unsupported field type.");
            }

            return 0f;
        }
    }

}