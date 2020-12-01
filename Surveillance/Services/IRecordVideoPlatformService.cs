using System;
using System.IO;

namespace Surveillance.Services
{
    public interface IRecordVideoPlatformService
    {
        void GoToRecordVideoPage();

        void GoToAppSettings();

        public const string OutputDirName = "videos";

        string OutputDirPath { get; }

        string OutputFilePath => Path.Combine(OutputDirPath, $"{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
    }
}