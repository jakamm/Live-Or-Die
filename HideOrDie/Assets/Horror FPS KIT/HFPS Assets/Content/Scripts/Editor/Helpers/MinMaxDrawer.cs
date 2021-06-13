using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MinMaxAttribute))]
public class MinMaxSliderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var minMaxAttribute = (MinMaxAttribute)attribute;
        var propertyType = property.propertyType;

        label.tooltip = minMaxAttribute.MinValue.ToString("F2") + " to " + minMaxAttribute.MaxValue.ToString("F2");

        Rect controlRect = EditorGUI.PrefixLabel(position, label);
        Rect[] splittedRect = SplitRect(controlRect, 3);

        if (propertyType == SerializedPropertyType.Vector2)
        {
            EditorGUI.BeginChangeCheck();

            Vector2 sliderValue = property.vector2Value;
            EditorGUI.MinMaxSlider(splittedRect[1], ref sliderValue.x, ref sliderValue.y, minMaxAttribute.MinValue, minMaxAttribute.MaxValue);

            sliderValue.x = EditorGUI.FloatField(splittedRect[0], float.Parse(sliderValue.x.ToString("F2")));
            sliderValue.x = Mathf.Clamp(sliderValue.x, minMaxAttribute.MinValue, Mathf.Min(minMaxAttribute.MaxValue, sliderValue.y));

            sliderValue.y = EditorGUI.FloatField(splittedRect[2], float.Parse(sliderValue.y.ToString("F2")));
            sliderValue.y = Mathf.Clamp(sliderValue.y, Mathf.Max(minMaxAttribute.MinValue, sliderValue.x), minMaxAttribute.MaxValue);

            if (EditorGUI.EndChangeCheck())
            {
                property.vector2Value = sliderValue;
            }
        }
        else if (propertyType == SerializedPropertyType.Vector2Int)
        {
            EditorGUI.BeginChangeCheck();

            Vector2Int sliderValue = property.vector2IntValue;
            float minVal = sliderValue.x;
            float maxVal = sliderValue.y;

            EditorGUI.MinMaxSlider(splittedRect[1], ref minVal, ref maxVal, minMaxAttribute.MinValue, minMaxAttribute.MaxValue);

            sliderValue.x = EditorGUI.IntField(splittedRect[0], Mathf.FloorToInt(minVal));
            sliderValue.x = Mathf.FloorToInt(Mathf.Clamp(sliderValue.x, minMaxAttribute.MinValue, Mathf.Min(minMaxAttribute.MaxValue, sliderValue.y)));

            sliderValue.y = EditorGUI.IntField(splittedRect[2], Mathf.FloorToInt(maxVal));
            sliderValue.y = Mathf.FloorToInt(Mathf.Clamp(sliderValue.y, Mathf.Max(minMaxAttribute.MinValue, sliderValue.x), minMaxAttribute.MaxValue));

            if (EditorGUI.EndChangeCheck())
            {
                property.vector2IntValue = sliderValue;
            }
        }
    }

    Rect[] SplitRect(Rect rectToSplit, int n)
    {
        Rect[] rects = new Rect[n];

        for (int i = 0; i < n; i++)
        {
            rects[i] = new Rect(rectToSplit.position.x + (i * rectToSplit.width / n), rectToSplit.position.y, rectToSplit.width / n, rectToSplit.height);
        }

        int padding = (int)rects[0].width - 40;
        int space = 5;

        rects[0].width -= padding + space;
        rects[2].width -= padding + space;

        rects[1].x -= padding;
        rects[1].width += padding * 2;

        rects[2].x += padding + space;

        return rects;
    }
}