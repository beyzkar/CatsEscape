using UnityEngine;

namespace CatsEscape.Networking
{
    public static class DeviceInfoProvider
    {
        [System.Serializable]
        public class DeviceInfo
        {
            public string model;
            public string operatingSystem;
            public string language;
            public string country;
        }

        public static DeviceInfo GetCurrentDeviceInfo()
        {
            return new DeviceInfo
            {
                model = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                language = Application.systemLanguage.ToString(),
                country = GetCountryFromLocale()
            };
        }

        private static string GetCountryFromLocale()
        {
            // Inference based on language (simplistic but works for basic needs)
            // On Android/iOS you could use native plugins for more accuracy
            return Application.systemLanguage.ToString(); 
        }
    }
}
