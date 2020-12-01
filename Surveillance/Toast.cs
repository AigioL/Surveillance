using Surveillance.Services;
using Xamarin.Forms;

namespace Surveillance
{
    public static class Toast
    {
        public const int LENGTH_LONG = IToastPlatformService.LENGTH_LONG;

        public const int LENGTH_SHORT = IToastPlatformService.LENGTH_SHORT;

        public static void Show(string text, int? duration = null)
        {
            var toast = DependencyService.Get<IToastPlatformService>();
            var _duration = duration ?? (text.Length > 9 ? LENGTH_LONG : LENGTH_SHORT);
            toast.Show(text, _duration);
        }
    }
}