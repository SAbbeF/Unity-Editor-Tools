using UnityEngine;
using UnityEditor;

public class MoveComponentTool 
{
    const string MenuMoveToTop = "CONTEXT/Component/Move To Top";
    const string MenuMoveToBottom = "CONTEXT/Component/Move To Bottom";

    [MenuItem(MenuMoveToTop, priority = 501)]
    public static void MoveComponentToTopMenuItem(MenuCommand command)
    {
        while (UnityEditorInternal.ComponentUtility.MoveComponentUp((Component)command.context));
    }

    [MenuItem(MenuMoveToTop, validate = true)]
    public static bool MoveComponentToTopMenuItemValidate(MenuCommand command)
    {
        Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == ((Component)command.context))
            {
                if (i == 1)
                    return false;
            }
        }
        return true;
    }

    [MenuItem(MenuMoveToBottom, priority = 502)]
    public static void MoveComponentToBottomMenuItem(MenuCommand command)
    {
        while (UnityEditorInternal.ComponentUtility.MoveComponentDown((Component)command.context));
    }

    [MenuItem(MenuMoveToBottom, validate = true)]
    public static bool MoveComponentToBottomMenuItemValidate(MenuCommand command)
    {
        Component[] components = ((Component)command.context).gameObject.GetComponents<Component>();

        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == ((Component)command.context))
            {
                if (i == (components.Length - 1))
                    return false;
            }
        }
        return true;
    }
}
