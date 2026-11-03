using System;
using UnityEngine;

/// <summary>
/// Runs a layout callback again when the screen size changes (rotation, split screen, Device Simulator).
/// Used by the level camera fit, which is otherwise computed once at level start.
/// </summary>
public class ScreenSizeRefit : MonoBehaviour
{
    private Action _refit;
    private int _width;
    private int _height;

    public void Track(Action refit)
    {
        _refit = refit;
        _width = Screen.width;
        _height = Screen.height;
    }

    public void Stop() => _refit = null;

    private void LateUpdate()
    {
        if (_refit == null || (Screen.width == _width && Screen.height == _height))
        {
            return;
        }

        _width = Screen.width;
        _height = Screen.height;
        _refit();
    }

    private void OnDestroy() => _refit = null;
}
