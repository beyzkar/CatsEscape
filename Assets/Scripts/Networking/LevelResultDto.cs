using System;

namespace CatsEscape.Networking
{
    [Serializable]
    public class LevelResultDto
    {
        public int levelNumber;
        public string levelResult; // "completed" | "failed" | "abandoned"
        public int xpEarned;
        public int fishSpawnCount;
        public int potionSpawnCount;
        public int heartsGained;
        public int heartsLost;
        public DeviceInfoProvider.DeviceInfo deviceInfo;

        // Timing fields
        public string startedAt;
        public string completedAt;
        public string abandonedAt;
        public float durationSeconds;
    }
}
