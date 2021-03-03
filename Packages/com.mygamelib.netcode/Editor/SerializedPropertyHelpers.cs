using UnityEditor;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    public static class SerializedPropertyHelpers
    {
        public static void PropertyFieldWithDefaultText(this SerializedProperty prop, GUIContent label,
            string defaultText)
        {
            GUI.SetNextControlName(label.text);
            var rt = GUILayoutUtility.GetRect(label, GUI.skin.textField);

            EditorGUI.PropertyField(rt, prop, label);
            if (string.IsNullOrEmpty(prop.stringValue) && GUI.GetNameOfFocusedControl() != label.text &&
                Event.current.type == EventType.Repaint)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    rt.xMin += EditorGUIUtility.labelWidth;
                    GUI.skin.textField.Draw(rt, new GUIContent(defaultText), false, false, false, false);
                }
            }
        }
    }
}