﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Inscribe
{
    public static class Define
    {
        public static string GetExecutingPath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public static FileVersionInfo GetVersion()
        {
            return FileVersionInfo.GetVersionInfo(GetExecutingPath());
        }

        public static string GetFormattedVersion()
        {
            var ver = GetVersion();
            return ver.FileMajorPart + "." + ver.FileMinorPart + "." + ver.FileBuildPart + FileKind(ver.FilePrivatePart);
        }

        private static string FileKind(int value)
        {
            switch (value)
            {
                case 0:
                    return String.Empty;
                case 1:
                    return " rc";
                case 2:
                    return " beta";
                case 3:
                    return " daybreak";
                case 4:
                    return " twilight";
                case 5:
                    return " midnight";
                default:
                    return " Special build " + value;
            }
        }

        public static double GetNumericVersion()
        {
            var lvobj = GetVersion();
            var lvstr = (lvobj.FileMajorPart * 1000 + lvobj.FileMinorPart).ToString() + "." + lvobj.FileBuildPart.ToString();
            return Double.Parse(lvstr);
        }

        public static readonly string ApplicationName = "Krile";

        public static readonly string SettingFileName = "krile.xml";

        public static readonly string KeyAssignDirectory = "assigns";

        public static readonly string PluginDirectory = "plugins";

        public static readonly string FeedbackAppName = "reporter.exe";

        public static readonly string DefaultStatusMessage = "完了";

        public static readonly string RemoteVersionXml = "http://update.starwing.net/k2/update.xml";
    }
}
