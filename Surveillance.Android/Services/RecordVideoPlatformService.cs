using Android.App;
using Surveillance.Droid.Activities;
using Surveillance.Services;
using System;
using static Surveillance.Services.IRecordVideoPlatformService;

namespace Surveillance.Droid.Services
{
    public sealed class RecordVideoPlatformService : IRecordVideoPlatformService
    {
        public async void GoToRecordVideoPage()
        {
            var isOk = await RecordVideoUtils.DynamicPermissions(() =>
            {
                MainApplication.CurrentContext.StartActivity(typeof(RecordVideoActivity));
            });
            if (!isOk)
            {
                GoToAppSettings();
            }
        }

        public void GoToAppSettings()
        {
            MainApplication.CurrentContext.GoToAppSettings();
        }

        readonly Lazy<string> mOutputDirPath = new Lazy<string>(() =>
        {
            var outPutDirInfo = Application.Context.GetExternalFilesDir(OutputDirName);
            if (outPutDirInfo == null) throw new NullReferenceException(nameof(outPutDirInfo));
            return outPutDirInfo.CanonicalPath;
        });

        public string OutputDirPath => mOutputDirPath.Value;
    }
}