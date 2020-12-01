# nullable enable
using Android.App;
using Android.Content.PM;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.Widget;
using Controls;
using System;
using System.IO;
using System.Text;
using R = Surveillance.Droid.Resource;

namespace Surveillance.Droid.Activities
{
    [Activity(LaunchMode = LaunchMode.SingleInstance, Label = "@string/app_name", Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public sealed class RecordVideoActivity : BaseActivity, RecordVideoUtils.IHost
    {
        protected override int? LayoutResource => R.Layout.activity_record_video;

        public bool IsInit { get; set; }

        TextureView? mTextureView;

        public TextureView TextureView
            => mTextureView ?? throw new NullReferenceException(nameof(TextureView));

        public CameraManager? CameraManager { get; set; }

        public CameraDevice? CameraDevice { get; set; }

        public CameraCaptureSession? CameraCaptureSession { get; set; }

        public CameraDevice.StateCallback? CameraDeviceStateCallback { get; set; }

        public CameraCaptureSession.StateCallback? SessionStateCallback { get; set; }

        public CameraCaptureSession.CaptureCallback? SessionCaptureCallback { get; set; }

        public CaptureRequest.Builder? PreviewCaptureRequest { get; set; }

        public CaptureRequest.Builder? RecorderCaptureRequest { get; set; }

        public MediaRecorder MediaRecorder { get; } = new MediaRecorder();

        public string? CurrentSelectCamera { get; set; }

        public Handler? ChildHandler { get; set; }

        public int VideoFrameWidth { get; set; }

        public int VideoFrameHeight { get; set; }

        public int VideoFrameRate { get; set; }

        public string CurrentOutputFilePath { get; set; } = string.Empty;

        public bool RecorderState { get; set; }

        AppCompatButton? btnStart;
        AppCompatButton? btnFinish;
        AppCompatTextView? textView;
        StrokeTextView? textViewStroke;

        public AppCompatButton BtnStart
            => btnStart ?? throw new NullReferenceException(nameof(btnStart));

        public AppCompatButton BtnFinish
            => btnFinish ?? throw new NullReferenceException(nameof(btnFinish));

        public AppCompatTextView TextView
           => textView ?? throw new NullReferenceException(nameof(textView));

        public StrokeTextView TextViewStroke
           => textViewStroke ?? throw new NullReferenceException(nameof(textViewStroke));

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            mTextureView = FindViewById<TextureView>(R.Id.textureView);
            btnStart = FindViewById<AppCompatButton>(R.Id.btnStart);
            btnFinish = FindViewById<AppCompatButton>(R.Id.btnFinish);
            textView = FindViewById<AppCompatTextView>(R.Id.textView);
            textViewStroke = FindViewById<StrokeTextView>(R.Id.textViewStroke);
            BtnFinish.Enabled = false;
            SetOnClickListener(btnStart, btnFinish);
            this.InitVideoSize();
            this.InitRecordVideo();
        }

        protected override void OnClick(View view)
        {
            base.OnClick(view);
            if (view.Id == R.Id.btnStart)
            {
                if (this.StartRecorder())
                {
                    BtnStart.Enabled = false;
                    BtnFinish.Enabled = true;
                    ShowInfo();
                    Toast.Show($"start {File.Exists(CurrentOutputFilePath)} path: {CurrentOutputFilePath}");
                }
            }
            else if (view.Id == R.Id.btnFinish)
            {
                if (this.StopRecorder())
                {
                    BtnStart.Enabled = true;
                    BtnFinish.Enabled = false;
                    Toast.Show($"stop {File.Exists(CurrentOutputFilePath)} path: {CurrentOutputFilePath}");
                }
            }
        }

        public string Text
        {
            set
            {
                TextView.Text = value;
                TextViewStroke.Text = value;
            }
        }

        void ShowInfo()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("FileName: {0}", Path.GetFileName(CurrentOutputFilePath));
            builder.AppendLine();
            builder.AppendFormat("Resolution: {0}x{1}@{2}", VideoFrameWidth, VideoFrameHeight, VideoFrameRate);
            builder.AppendLine();
            Text = builder.ToString();
        }
    }
}
#nullable disable