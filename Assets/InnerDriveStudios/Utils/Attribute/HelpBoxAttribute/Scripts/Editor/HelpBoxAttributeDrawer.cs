using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// Custom decorator drawer for <see cref="HelpBoxAttribute"/> using UI Toolkit.
/// 
/// This implementation targets Unity's modern inspector pipeline (UI Toolkit),
/// and does not rely on IMGUI. It renders a help box above a serialized field,
/// allowing for dynamic height and proper text wrapping without manual layout calculations.
/// 
/// Unlike IMGUI-based drawers, UI Toolkit elements automatically size themselves
/// based on their content, making this implementation robust for long or multiline text.
/// </summary>
[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
public class HelpBoxAttributeDrawer : DecoratorDrawer
{
    /// <summary>
    /// Creates the UI Toolkit visual representation of the help box.
    /// 
    /// This method is called by Unity when using the UI Toolkit-based inspector
    /// (default in Unity 2022.2+). It replaces the need for GetHeight() and OnGUI().
    /// </summary>
    /// <returns>
    /// A <see cref="VisualElement"/> containing a configured <see cref="HelpBox"/>.
    /// </returns>
    public override VisualElement CreatePropertyGUI()
    {
        // Safely cast the attribute instance
        var helpBoxAttribute = attribute as HelpBoxAttribute;

        // Fallback: return an empty container if something went wrong
        if (helpBoxAttribute == null)
            return new VisualElement();

        // Create the UI Toolkit HelpBox element
        var helpBox = new HelpBox(
            helpBoxAttribute.text ?? string.Empty,
            GetMessageType(helpBoxAttribute.messageType)
        );

        // Ensure long text wraps correctly instead of overflowing
        helpBox.style.whiteSpace = WhiteSpace.Normal;

        // Add a bit of vertical spacing so the help box integrates nicely
        // with surrounding inspector fields
        helpBox.style.marginTop = 2;
        helpBox.style.marginBottom = 2;

        return helpBox;
    }

    /// <summary>
    /// Converts the custom <see cref="HelpBoxMessageType"/> enum
    /// to UI Toolkit's <see cref="UnityEngine.UIElements.HelpBoxMessageType"/>.
    /// </summary>
    /// <param name="type">The message type defined in the attribute.</param>
    /// <returns>The corresponding UI Toolkit message type.</returns>
    private static UnityEngine.UIElements.HelpBoxMessageType GetMessageType(HelpBoxMessageType type)
    {
        switch (type)
        {
            case HelpBoxMessageType.Info:
                return UnityEngine.UIElements.HelpBoxMessageType.Info;

            case HelpBoxMessageType.Warning:
                return UnityEngine.UIElements.HelpBoxMessageType.Warning;

            case HelpBoxMessageType.Error:
                return UnityEngine.UIElements.HelpBoxMessageType.Error;

            default:
                return UnityEngine.UIElements.HelpBoxMessageType.None;
        }
    }
}