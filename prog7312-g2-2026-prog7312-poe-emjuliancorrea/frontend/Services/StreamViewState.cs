using Smart_X.frontend.Models;

namespace Smart_X.frontend.Services;

public static class StreamViewState
{
    public static bool Live { get; set; } = true;

    public static string SearchText { get; set; } = string.Empty;

    public static StreamSnapshot? LastSnapshot { get; private set; }

    public static DateTime LastSnapshotAt { get; private set; }

    public static void Remember(StreamSnapshot snapshot)
    {
        LastSnapshot = snapshot;
        LastSnapshotAt = DateTime.Now;
    }
}
