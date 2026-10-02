namespace CapChief.Windowing;

/// <summary>
/// Defines an object that handles a window's messages through the handler interfaces it implements.
/// </summary>
public interface IWindowHandler { }

/// <summary>
/// Defines how a window paints its client area when Windows asks it to.
/// </summary>
public interface IPaintHandler : IWindowHandler
{
    void OnPaint();
}

/// <summary>
/// Defines how a window handles the left mouse button, pointer moves and the cursor shape.
/// </summary>
public interface IMouseHandler : IWindowHandler
{
    void OnMouseDown(Point at, bool shift);

    void OnMouseUp(Point at);

    void OnMouseMove(Point at, bool shift, bool ctrl);

    CursorShape? OnCursorWanted();
}

/// <summary>
/// Defines how a window handles a double click of the left mouse button.
/// </summary>
public interface IDoubleClickHandler : IWindowHandler
{
    void OnDoubleClick(Point at, bool shift);
}

/// <summary>
/// Defines how a window handles a click of the right mouse button.
/// </summary>
public interface IRightClickHandler : IWindowHandler
{
    void OnRightClick();
}

/// <summary>
/// Defines how a window handles the monitors being added, removed or resized.
/// </summary>
public interface IDisplayChangeHandler : IWindowHandler
{
    void OnDisplayChanged();
}

/// <summary>
/// Defines how a window handles the pointer leaving its client area.
/// </summary>
public interface IMouseLeaveHandler : IWindowHandler
{
    void OnMouseLeft();
}

/// <summary>
/// Defines how a window handles a key being pressed while it has the focus.
/// </summary>
public interface IKeyDownHandler : IWindowHandler
{
    bool OnKeyDown(int virtualKey);
}

/// <summary>
/// Defines how a window handles a key being released while it has the focus.
/// </summary>
public interface IKeyUpHandler : IWindowHandler
{
    bool OnKeyUp(int virtualKey);
}

/// <summary>
/// Defines how a window handles the timers it started.
/// </summary>
public interface ITimerHandler : IWindowHandler
{
    void OnTimer(int id);
}

/// <summary>
/// Defines how a window handles the global hotkeys registered to it.
/// </summary>
public interface IHotkeyHandler : IWindowHandler
{
    void OnHotkey(int id);
}

/// <summary>
/// Defines how a window handles becoming the active window.
/// </summary>
public interface IActivateHandler : IWindowHandler
{
    void OnActivated();
}

/// <summary>
/// Defines how a window handles no longer being the active window.
/// </summary>
public interface IDeactivateHandler : IWindowHandler
{
    void OnDeactivated();
}

/// <summary>
/// Defines which part of a window the pointer is over, such as its caption.
/// </summary>
public interface IHitTestHandler : IWindowHandler
{
    HitZone OnHitTest();
}

/// <summary>
/// Defines how a window paints the background behind a text box it hosts.
/// </summary>
public interface IEditBackdropHandler : IWindowHandler
{
    bool OnEditBackdrop(DeviceContext dc, EditRef edit);
}

/// <summary>
/// Defines how a window handles application-defined and registered window messages.
/// </summary>
public interface IAppMessageHandler : IWindowHandler
{
    bool OnAppMessage(uint id, AppSignal signal);
}

/// <summary>
/// Defines how a window handles moving to a monitor with a different DPI.
/// </summary>
public interface IDpiChangeHandler : IWindowHandler
{
    void OnDpiChanged(float scale, Rect suggested);
}

/// <summary>
/// Defines how a window handles being closed and destroyed.
/// </summary>
public interface ICloseHandler : IWindowHandler
{
    void OnClosed();
}

/// <summary>
/// Specifies which part of a window the mouse is over during hit testing.
/// </summary>
public enum HitZone
{
    Default,
    Client,
    Transparent,
}
