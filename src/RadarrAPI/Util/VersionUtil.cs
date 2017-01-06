using System;

namespace RadarrAPI.Util
{
    public static class VersionUtil
    {

        public static bool IsValid(string version)
        {
            Version v;
            return Version.TryParse(version, out v);
        }

    }
}
