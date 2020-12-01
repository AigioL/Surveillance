using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;

namespace Surveillance.Droid.Activities
{
    public abstract class BaseActivity : AppCompatActivity, View.IOnClickListener, IContext
    {
        protected abstract int? LayoutResource { get; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (LayoutResource.HasValue)
            {
                SetContentView(LayoutResource.Value);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected void SetOnClickListener(params View[] views)
        {
            foreach (var view in views)
            {
                view?.SetOnClickListener(this);
            }
        }

        protected virtual void OnClick(View view)
        {
        }

        void View.IOnClickListener.OnClick(View view) => OnClick(view);

        Context IContext.Context => this;
    }
}