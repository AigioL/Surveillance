using Surveillance.Resx;
using Surveillance.Services;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Surveillance.ViewModels
{
    public sealed class SurveillanceViewModel : BaseViewModel
    {
        public static IRecordVideoPlatformService RecordVideoPlatformService => DesignMode.IsDesignModeEnabled ? null : DependencyService.Get<IRecordVideoPlatformService>();

        public SurveillanceViewModel()
        {
            Title = AppResources.Surveillance;
            if (RecordVideoPlatformService != null)
            {
                GoToAppSettingsCommand = new Command(RecordVideoPlatformService.GoToAppSettings);
                GoToRecordVideoPageCommand = new Command(RecordVideoPlatformService.GoToRecordVideoPage);
            }
        }

        public ICommand GoToAppSettingsCommand { get; }

        public ICommand GoToRecordVideoPageCommand { get; }

        string content = GetContent();

        public string Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        static string GetContent()
        {
            var builder = new StringBuilder();

            builder.AppendLine("MainDisplayInfo");
            builder.AppendFormat("Orientation: {0}", DeviceDisplay.MainDisplayInfo.Orientation);
            builder.AppendLine();
            builder.AppendFormat("Rotation: {0}", DeviceDisplay.MainDisplayInfo.Rotation);
            builder.AppendLine();
            builder.AppendFormat("Width: {0}", DeviceDisplay.MainDisplayInfo.Width);
            builder.AppendLine();
            builder.AppendFormat("Height: {0}", DeviceDisplay.MainDisplayInfo.Height);
            builder.AppendLine();
            builder.AppendFormat("Density: {0}", DeviceDisplay.MainDisplayInfo.Density);
            builder.AppendLine();

            builder.AppendLine("DeviceInfo");
            builder.AppendFormat("Model: {0}", DeviceInfo.Model);
            builder.AppendLine();
            builder.AppendFormat("Manufacturer: {0}", DeviceInfo.Manufacturer);
            builder.AppendLine();
            builder.AppendFormat("Name: {0}", DeviceInfo.Name);
            builder.AppendLine();
            builder.AppendFormat("Version: {0}", DeviceInfo.VersionString);
            builder.AppendLine();
            builder.AppendFormat("Platform: {0}", DeviceInfo.Platform);
            builder.AppendLine();
            builder.AppendFormat("Idiom: {0}", DeviceInfo.Idiom);
            builder.AppendLine();
            builder.AppendFormat("DeviceType: {0}", DeviceInfo.DeviceType);
            builder.AppendLine();

            builder.AppendLine("Surveillance");
            builder.AppendFormat("OutputDirPath: {0}", RecordVideoPlatformService?.OutputDirPath);
            builder.AppendLine();

            return builder.ToString();
        }
    }
}