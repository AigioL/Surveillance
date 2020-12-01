# nullable enable
using Android.App;
using Android.OS;
using Android.Util;
using Android.Widget;
using Surveillance.Services;
using System;
using Xamarin.Essentials;
using AndroidToast = Android.Widget.Toast;
using Environment = System.Environment;

namespace Surveillance.Droid.Services
{
    public sealed class ToastPlatformService : IToastPlatformService
    {
        const string TAG = "Toast";

        AndroidToast? toast;

        void IToastPlatformService.Show(string text, int duration)
        {
            try
            {
                if (MainThread.IsMainThread)
                {
                    Show_();
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(Show_);
                }

                void Show_()
                {
                    var context = Application.Context;
                    var duration2 = (ToastLength)duration;

                    // https://blog.csdn.net/android157/article/details/80267737
                    try
                    {
                        if (toast == null)
                        {
                            toast = AndroidToast.MakeText(context, text, duration2);
                            if (toast == null) throw new NullReferenceException("toast markeText Fail");
                        }
                        else
                        {
                            toast.Duration = duration2;
                        }
                        SetTextAndShow(toast, text);
                    }
                    catch (Exception e)
                    {
                        Log.Error(TAG, $"text: {text}{Environment.NewLine}{e}");
                        // 解决在子线程中调用Toast的异常情况处理
                        Looper.Prepare();
                        var _toast = AndroidToast.MakeText(context, text, duration2)
                            ?? throw new NullReferenceException("toast markeText Fail(2)");
                        SetTextAndShow(_toast, text);
                        Looper.Loop();
                    }

                    static void SetTextAndShow(AndroidToast t, string text)
                    {
                        // 某些定制ROM会更改内容文字，例如MIUI，重新设置可强行指定文本
                        t.SetText(text);
                        t.Show();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(TAG, $"text: {text}{Environment.NewLine}{e}");
            }
        }
    }
}
#nullable disable