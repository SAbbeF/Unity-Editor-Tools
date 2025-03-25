using UnityEditor;
using UnityEngine;

public static class UnityEditorBackgroundColor 
{
    static readonly Color DefaultColor = new Color(0.7843f, 0.7843f, 0.7843f);
    static readonly Color DefaultProColor = new Color(0.2196f, 0.2196f, 0.2196f);

    static readonly Color SelectedColor = new Color(0.22745f, 0.22745f, 0.22745f);
    static readonly Color SelectedProColor = new Color(0.17254f, 0.3647f, 0.5294f);

    static readonly Color SelectedUnFocusedColor = new Color(0.68f, 0.68f, 0.68f);
    static readonly Color SelectedUnFocusedProColor = new Color(0.3f, 0.3f, 0.3f);

    static readonly Color HoveredColor = new Color(0.698f, 0.698f, 0.698f);
    static readonly Color HoveredProColor = new Color(0.2706f, 0.2706f, 0.2706f);

    public static Color Get(bool isSelected, bool isHovered, bool isWindowFocused)
    {
        if (isSelected)
        {
            if (isWindowFocused)
            {
                return EditorGUIUtility.isProSkin ? SelectedProColor : SelectedColor;
            }
            else
            {
                return EditorGUIUtility.isProSkin ? SelectedUnFocusedProColor : SelectedUnFocusedColor;
            }
        }
        else if (isHovered)
        {
            return EditorGUIUtility.isProSkin ? HoveredProColor : HoveredColor;
        }
        else
        {
            return EditorGUIUtility.isProSkin ? DefaultProColor : DefaultColor;
        }
    }
}
