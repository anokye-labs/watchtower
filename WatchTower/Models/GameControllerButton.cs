namespace WatchTower.Models;

/// <summary>
/// Represents standard game controller buttons.
/// Follows the standard gamepad button layout (Xbox/PlayStation compatible).
/// </summary>
public enum GameControllerButton
{
    /// <summary>Bottom face button (A on Xbox, Cross on PlayStation)</summary>
    A = 0,

    /// <summary>Right face button (B on Xbox, Circle on PlayStation)</summary>
    B = 1,

    /// <summary>Left face button (X on Xbox, Square on PlayStation)</summary>
    X = 2,

    /// <summary>Top face button (Y on Xbox, Triangle on PlayStation)</summary>
    Y = 3,

    /// <summary>Back/Select button</summary>
    Back = 4,

    /// <summary>Guide/Home button</summary>
    Guide = 5,

    /// <summary>Start button</summary>
    Start = 6,

    /// <summary>Left stick click</summary>
    LeftStick = 7,

    /// <summary>Right stick click</summary>
    RightStick = 8,

    /// <summary>Left shoulder button</summary>
    LeftShoulder = 9,

    /// <summary>Right shoulder button</summary>
    RightShoulder = 10,

    /// <summary>D-Pad Up</summary>
    DPadUp = 11,

    /// <summary>D-Pad Down</summary>
    DPadDown = 12,

    /// <summary>D-Pad Left</summary>
    DPadLeft = 13,

    /// <summary>D-Pad Right</summary>
    DPadRight = 14
}
