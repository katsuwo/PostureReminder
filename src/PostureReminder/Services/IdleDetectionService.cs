using PostureReminder.Native;

namespace PostureReminder.Services;

public class IdleDetectionService
{
    private NativeMethods.POINT _lastCursorPos;
    private DateTime _lastActivityTime = DateTime.UtcNow;

    // Ignore mouse movements smaller than this (pixels)
    private const int MoveThreshold = 10;

    public IdleDetectionService()
    {
        NativeMethods.GetCursorPos(out _lastCursorPos);
    }

    public TimeSpan GetIdleTime()
    {
        NativeMethods.GetCursorPos(out var current);
        int dx = current.X - _lastCursorPos.X;
        int dy = current.Y - _lastCursorPos.Y;

        if (dx * dx + dy * dy >= MoveThreshold * MoveThreshold)
        {
            _lastActivityTime = DateTime.UtcNow;
            _lastCursorPos = current;
        }

        return DateTime.UtcNow - _lastActivityTime;
    }
}
