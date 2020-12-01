namespace Surveillance.Services
{
    public interface IToastPlatformService
    {
        public const int LENGTH_LONG = 1;

        public const int LENGTH_SHORT = 0;

        void Show(string text, int duration);
    }
}