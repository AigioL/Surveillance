using Android.Content;
using Android.Runtime;
using AndroidX.Core.Content;
using System;
using Settings = Android.Provider.Settings;
using Uri = Android.Net.Uri;

// ReSharper disable once CheckNamespace
namespace Surveillance.Droid
{
    public static class ContextExtensions
    {
        /// <inheritdoc cref="Context.GetSystemService(string)"/>
        public static TService GetSystemService<TService>(this Context context) where TService : class, IJavaObject
        {
            var type = typeof(TService);
            var cls = Java.Lang.Class.FromType(type);
            var service = ContextCompat.GetSystemService(context, cls).JavaCast<TService>();
            if (service == null)
                throw new NullReferenceException(
                    $"ContextCompat.GetSystemService return null, type: {type}");
            return service;
        }

        public static void GoToAppSettings(this Context context)
        {
            var intent = new Intent(Settings.ActionApplicationDetailsSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            var uri = Uri.FromParts("package", context.PackageName, null);
            intent.SetData(uri);
            context.StartActivity(intent);
        }

        //public static void OpenDir(this Context context, string dirPath)
        //{
        //    // no work.
        //    var intent = new Intent(Intent.ActionGetContent);
        //    intent.AddFlags(ActivityFlags.NewTask);
        //    intent.AddCategory(Intent.CategoryDefault);
        //    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        //    var data = FileProvider.GetUriForFile(
        //        context,
        //        context.PackageName + ".fileProvider",
        //        new Java.IO.File(dirPath));
        //    intent.SetDataAndType(data, DocumentsContract.Document.MimeTypeDir);
        //    context.StartActivity(Intent.CreateChooser(intent, "Select Explorer"));
        //}
    }
}