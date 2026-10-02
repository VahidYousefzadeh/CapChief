namespace CapChief.Windowing;

/// <summary>
/// Defines the events a text box raises for key presses, edits and focus loss.
/// </summary>
public interface ITextBoxEvents
{
    bool OnTextKey(int virtualKey);

    void OnTextFocusLost();

    void OnTextEdited();
}
