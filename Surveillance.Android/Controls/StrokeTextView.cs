using Android.Content;
using Android.Graphics;
using Android.Util;
using AndroidX.AppCompat.Widget;
using PaintStyle = Android.Graphics.Paint.Style;

// ReSharper disable once CheckNamespace
namespace Controls
{
    public sealed class StrokeTextView : AppCompatTextView
    {
        public StrokeTextView(Context context) : base(context)
        {
        }

        public StrokeTextView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public StrokeTextView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        protected override void OnDraw(Canvas canvas)
        {
            Paint.SetStyle(PaintStyle.Stroke);
            Paint.StrokeWidth = 2;
            base.OnDraw(canvas);
        }
    }
}