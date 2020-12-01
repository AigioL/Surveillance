using Android.App;
using Android.OS;
using Android.Runtime;
using Surveillance.Droid.Services;
using Surveillance.Services;
using System;
using Xamarin.Forms;
using Application = Android.App.Application;

namespace Surveillance.Droid
{
    [Application]
    public sealed class MainApplication : Application, Application.IActivityLifecycleCallbacks
    {
        public static Activity CurrentContext { get; private set; }

        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            RegisterActivityLifecycleCallbacks(this);
            Xamarin.Essentials.Platform.Init(this);
            ConfigureServices();
            static void ConfigureServices()
            {
                DependencyService.Register<IToastPlatformService, ToastPlatformService>();
                DependencyService.Register<IRecordVideoPlatformService, RecordVideoPlatformService>();
            }
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }

        void IActivityLifecycleCallbacks.OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            CurrentContext = activity;
        }

        void IActivityLifecycleCallbacks.OnActivityDestroyed(Activity activity)
        {
        }

        void IActivityLifecycleCallbacks.OnActivityPaused(Activity activity)
        {
        }

        void IActivityLifecycleCallbacks.OnActivityResumed(Activity activity)
        {
            CurrentContext = activity;
        }

        void IActivityLifecycleCallbacks.OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        void IActivityLifecycleCallbacks.OnActivityStarted(Activity activity)
        {
            CurrentContext = activity;
        }

        void IActivityLifecycleCallbacks.OnActivityStopped(Activity activity)
        {
        }
    }
}