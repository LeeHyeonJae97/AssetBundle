#if UNITY_EDITOR
using UnityEditor;

public static class EditorUtils
{
    public static SerializedProperty FindPropertyRelative(this SerializedProperty serializedProperty, string relativePropertyPath, bool isBackingField)
    {
        if (isBackingField)
        {
            return serializedProperty.FindPropertyRelative($"<{relativePropertyPath}>k__BackingField");
        }
        else
        {
            return serializedProperty.FindPropertyRelative(relativePropertyPath);
        }
    }
}
#endif
