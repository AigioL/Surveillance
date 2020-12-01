// https://www.cnblogs.com/guanxinjing/p/11009192.html
# nullable enable
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Surveillance.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

// ReSharper disable once CheckNamespace
namespace Surveillance.Droid
{
    public static class RecordVideoUtils
    {
        const string TAG = "RecordVideo";

        public interface IHost : IContext
        {
            bool IsInit { get; set; }

            TextureView? TextureView { get; }

            CameraManager? CameraManager { get; set; }

            CameraDevice? CameraDevice { get; set; }

            CameraCaptureSession? CameraCaptureSession { get; set; }

            CameraDevice.StateCallback? CameraDeviceStateCallback { get; set; }

            CameraCaptureSession.StateCallback? SessionStateCallback { get; set; }

            CameraCaptureSession.CaptureCallback? SessionCaptureCallback { get; set; }

            CaptureRequest.Builder? PreviewCaptureRequest { get; set; }

            CaptureRequest.Builder? RecorderCaptureRequest { get; set; }

            MediaRecorder MediaRecorder { get; }

            string? CurrentSelectCamera { get; set; }

            Handler? ChildHandler { get; set; }

            string OutputFilePath => DependencyService.Get<IRecordVideoPlatformService>().OutputFilePath;

            void OnError(string position, Exception e, bool @throw = true)
            {
                var msg = $"position: {position}{System.Environment.NewLine}{e}";
                OnError(msg);
                if (@throw) throw e;
            }

            void OnError(string msg)
            {
                Toast.Show(msg);
                Log.Error(TAG, msg);
            }

            int VideoFrameWidth { get; set; }

            int VideoFrameHeight { get; set; }

            int VideoFrameRate { get; set; }

            string CurrentOutputFilePath { get; set; }

            bool RecorderState { get; set; }
        }

        public static void InitVideoSize(this IHost host)
        {
            var camcorderProfile = new[] { CamcorderQuality.Q1080p, CamcorderQuality.High }
                .Select(x =>
                {
                    try
                    {
                        return CamcorderProfile.Get(x);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .FirstOrDefault(x => x != null);
            if (camcorderProfile != null)
            {
                host.VideoFrameWidth = camcorderProfile.VideoFrameWidth;
                host.VideoFrameHeight = camcorderProfile.VideoFrameHeight;
                host.VideoFrameRate = camcorderProfile.VideoFrameRate;
            }
            else
            {
                host.VideoFrameHeight = DeviceDisplay.MainDisplayInfo.Height.Floor();
                host.VideoFrameWidth = DeviceDisplay.MainDisplayInfo.Width.Floor();
                host.VideoFrameRate = 30;
            }
        }

        public static void InitRecordVideo(this IHost host)
        {
            if (!host.IsInit)
            {
                host.InitChildHandler();
                host.InitSurfaceTextureListener();
                host.InitCameraDeviceStateCallback();
                host.InitSessionStateCallback();
                host.InitSessionCaptureCallback();
                host.IsInit = true;
            }
        }

        public static async Task<bool> DynamicPermissions(Action action)
        {
            var status = await Permissions.CheckStatusAsync<Permission>();
            if (status == PermissionStatus.Granted)
            {
                action();
                return true;
            }
            else
            {
                if (Permissions.ShouldShowRationale<Permission>())
                {
                    // Prompt the user with additional information as to why the permission is needed
                    Toast.Show(Resx.AppResources.RequestRecordVideoPermissionAdditionalInformation);
                }

                status = await Permissions.RequestAsync<Permission>();
                if (status == PermissionStatus.Granted)
                {
                    action();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 初始化子线程，用于操作Camera2
        /// </summary>
        /// <param name="host"></param>
        static void InitChildHandler(this IHost host)
        {
            if (host.ChildHandler != null) return;
            var handlerThread = new HandlerThread("RecordVideoCamera2");
            handlerThread.Start();
            var handlerThreadLooper = handlerThread.Looper;
            if (handlerThreadLooper == null) throw new NullReferenceException(nameof(handlerThreadLooper));
            host.ChildHandler = new Handler(handlerThreadLooper);
        }

        /// <summary>
        /// 初始化 <see cref="CameraManager"/>
        /// </summary>
        /// <param name="host"></param>
        static void InitCameraManager(this IHost host)
        {
            if (host.CameraManager != null) return;
            host.CameraManager = host.Context.GetSystemService<CameraManager>();
        }

        /// <summary>
        /// 初始化 <see cref="TextureView"/> 纹理生成监听
        /// </summary>
        /// <param name="host"></param>
        static void InitSurfaceTextureListener(this IHost host)
        {
            if (host.TextureView == null) return;
            host.TextureView.SurfaceTextureListener = new SurfaceTextureListener(host);
        }

        /// <summary>
        /// 选择摄像头
        /// </summary>
        /// <param name="host"></param>
        /// <param name="lensFacing"></param>
        static void SelectCamera(this IHost host, LensFacing lensFacing = LensFacing.Back)
        {
            if (host.CameraManager == null) throw new NullReferenceException(nameof(host.CameraManager));
            var cameraIds = host.CameraManager.GetCameraIdList(); // 获取当前设备全部的摄像头ID集合
            if (cameraIds == null || !cameraIds.Any())
                throw new NotSupportedException("The current device is missing camera hardware.");
            foreach (var cameraId in cameraIds) // 遍历所有的摄像头
            {
                // 获取当前遍历的摄像头描述特征
                var cameraCharacteristics = host.CameraManager.GetCameraCharacteristics(cameraId);
                var facing = cameraCharacteristics
                    .Get(CameraCharacteristics.LensFacing)?.JavaCast<Java.Lang.Integer>();
                if (facing != null && facing.IntValue() == (int)lensFacing)
                {
                    host.CurrentSelectCamera = cameraId;
                    break;
                }
            }
            if (host.CurrentSelectCamera == null)
                throw new NotSupportedException($"Failed to select camera, lensFacing: {lensFacing}");
        }

        /// <summary>
        /// 开启摄像头
        /// </summary>
        /// <param name="host"></param>
        static void OpenCamera(this IHost host)
        {
            try
            {
                if (host.CameraManager == null)
                    throw new NullReferenceException(nameof(host.CameraManager));
                if (host.CurrentSelectCamera == null)
                    throw new NullReferenceException(nameof(host.CurrentSelectCamera));
                if (host.CameraDeviceStateCallback == null)
                    throw new NullReferenceException(nameof(host.CameraDeviceStateCallback));
                if (host.ChildHandler == null)
                    throw new NullReferenceException(nameof(host.ChildHandler));
                host.CameraManager.OpenCamera(
                    host.CurrentSelectCamera,
                    host.CameraDeviceStateCallback,
                    host.ChildHandler);
            }
            catch (Exception e)
            {
                host.OnError(nameof(OpenCamera), e);
            }
        }

        sealed class SurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
        {
            readonly IHost host;

            public SurfaceTextureListener(IHost host)
            {
                this.host = host;
            }

            void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable(SurfaceTexture? surface, int width, int height)
            {
                // 当可以使用纹理时，选择摄像头工作并预览画面
                host.InitCameraManager();
                host.SelectCamera();
                host.OpenCamera();
            }

            void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged(SurfaceTexture? surface, int width, int height)
            {
                // 纹理尺寸变化
            }

            bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed(SurfaceTexture? surface)
            {
                // 纹理被销毁
                return false;
            }

            void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated(SurfaceTexture? surface)
            {
                // 纹理更新
            }
        }

        static void InitCameraDeviceStateCallback(this IHost host)
        {
            host.CameraDeviceStateCallback = new CameraDeviceStateCallback(host);
        }

        sealed class CameraDeviceStateCallback : CameraDevice.StateCallback
        {
            readonly IHost host;

            public CameraDeviceStateCallback(IHost host)
            {
                this.host = host;
            }

            public override void OnOpened(CameraDevice camera)
            {
                // 摄像头被打开
                try
                {
                    host.CameraDevice = camera;
                    if (host.TextureView != null)
                    {
                        var surfaceTexture = host.TextureView.SurfaceTexture;
                        if (surfaceTexture == null)
                            throw new NullReferenceException(nameof(surfaceTexture));
                        surfaceTexture.SetDefaultBufferSize(host.VideoFrameWidth, host.VideoFrameHeight);
                        var previewSurface = new Surface(surfaceTexture);
                        host.PreviewCaptureRequest = camera.CreateCaptureRequest(CameraTemplate.Preview);
                        var CONTROL_AF_MODE = CaptureRequest.ControlAfMode
                            ?? throw new NullReferenceException(nameof(CaptureRequest.ControlAfMode));
                        var CONTROL_AF_MODE_CONTINUOUS_PICTURE = (int)ControlAFMode.ContinuousPicture;
                        host.PreviewCaptureRequest.Set(CONTROL_AF_MODE, CONTROL_AF_MODE_CONTINUOUS_PICTURE);
                        host.PreviewCaptureRequest.AddTarget(previewSurface);
                        if (host.SessionStateCallback == null)
                            throw new NullReferenceException(nameof(host.SessionStateCallback));
                        var outputs = new List<Surface> { previewSurface };
                        camera.CreateCaptureSession(outputs, host.SessionStateCallback, host.ChildHandler);
                    }
                }
                catch (Exception e)
                {
                    host.OnError(nameof(OnOpened), e);
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                // 摄像头断开
            }

            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                host.OnError($"CameraDeviceStateCallback cameraId: {camera.Id}, error: {error}");
            }
        }

        static void InitSessionStateCallback(this IHost host)
        {
            host.SessionStateCallback = new CameraCaptureSessionStateCallback(host);
        }

        sealed class CameraCaptureSessionStateCallback : CameraCaptureSession.StateCallback
        {
            readonly IHost host;

            public CameraCaptureSessionStateCallback(IHost host)
            {
                this.host = host;
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                host.CameraCaptureSession = session;
                try
                {
                    // 执行重复获取数据请求，一直获取数据呈现预览画面
                    if (host.PreviewCaptureRequest == null)
                        throw new NullReferenceException(nameof(host.PreviewCaptureRequest));
                    host.CameraCaptureSession.SetRepeatingRequest(
                        host.PreviewCaptureRequest.Build(),
                        host.SessionCaptureCallback,
                        host.ChildHandler);
                }
                catch (Exception e)
                {
                    host.OnError(nameof(OnConfigured), e);
                }
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                throw new NotImplementedException();
            }
        }

        static void InitSessionCaptureCallback(this IHost host)
        {
            host.SessionCaptureCallback = new CameraCaptureSessionCaptureCallback(host);
        }

        sealed class CameraCaptureSessionCaptureCallback : CameraCaptureSession.CaptureCallback
        {
            readonly IHost host;

            public CameraCaptureSessionCaptureCallback(IHost host)
            {
                this.host = host;
            }

            public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
            {
                base.OnCaptureStarted(session, request, timestamp, frameNumber);
            }

            public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
            {
                base.OnCaptureProgressed(session, request, partialResult);
            }

            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                base.OnCaptureCompleted(session, request, result);
            }

            public override void OnCaptureFailed(CameraCaptureSession session, CaptureRequest request, CaptureFailure failure)
            {
                base.OnCaptureFailed(session, request, failure);
                host.OnError($"{nameof(OnCaptureFailed)} CaptureFailureReason: {failure.Reason}");
            }
        }

        static void ConfigMediaRecorder(this IHost host)
        {
            host.MediaRecorder.SetAudioSource(AudioSource.Mic); // 设置音频来源仅主麦克风
            host.MediaRecorder.SetVideoSource(VideoSource.Surface); // Camera2
            host.MediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
            host.MediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
            VideoEncoder videoEncoder;
            if (DeviceInfo.DeviceType != DeviceType.Virtual && Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                videoEncoder = VideoEncoder.Hevc;
            }
            else
            {
                videoEncoder = VideoEncoder.H264;
            }
            host.MediaRecorder.SetVideoEncoder(videoEncoder);
            host.MediaRecorder.SetVideoEncodingBitRate(1048576); // 码率
            host.MediaRecorder.SetVideoFrameRate(host.VideoFrameRate);
            host.MediaRecorder.SetVideoSize(host.VideoFrameWidth, host.VideoFrameHeight);
            host.MediaRecorder.SetAudioSamplingRate(44); // 设置音频采样率为44
            host.MediaRecorder.SetAudioEncodingBitRate(64); // 设置音频比特率为64
            host.MediaRecorder.SetAudioChannels(1); // 设置录制的音频通道数
            var rotation = DeviceDisplay.MainDisplayInfo.Rotation;
            switch (rotation)
            {
                case DisplayRotation.Rotation0:
                    host.MediaRecorder.SetOrientationHint(90);
                    break;
                case DisplayRotation.Rotation90:
                    host.MediaRecorder.SetOrientationHint(180);
                    break;
                case DisplayRotation.Rotation180:
                    host.MediaRecorder.SetOrientationHint(270);
                    break;
                case DisplayRotation.Rotation270:
                    host.MediaRecorder.SetOrientationHint(0);
                    break;
            }
            if (host.TextureView != null)
            {
                var surface = new Surface(host.TextureView.SurfaceTexture);
                host.MediaRecorder.SetPreviewDisplay(surface);
            }
            host.CurrentOutputFilePath = host.OutputFilePath;
            host.MediaRecorder.SetOutputFile(host.CurrentOutputFilePath);
            try
            {
                host.MediaRecorder.Prepare();
            }
            catch (Exception e)
            {
                host.OnError(nameof(MediaRecorder.Prepare), e);
            }
        }

        static void Config(this IHost host)
        {
            if (host.CameraCaptureSession != null)
            {
                try
                {
                    host.CameraCaptureSession.StopRepeating(); // 停止预览，准备切换到录制视频
                    host.CameraCaptureSession.Close(); // 关闭预览的会话，需要重新创建录制视频的会话
                    host.CameraCaptureSession = null;
                }
                catch (Exception e)
                {
                    host.OnError(nameof(Config), e);
                }
            }
            host.ConfigMediaRecorder();
            if (host.TextureView != null)
            {
                var surfaceTexture = host.TextureView.SurfaceTexture;
                if (surfaceTexture == null)
                    throw new NullReferenceException(nameof(surfaceTexture));
                surfaceTexture.SetDefaultBufferSize(host.VideoFrameWidth, host.VideoFrameHeight);
                var previewSurface = new Surface(surfaceTexture);
                var recorderSurface = host.MediaRecorder.Surface;
                if (recorderSurface == null)
                    throw new NullReferenceException(nameof(recorderSurface));
                if (host.CameraDevice == null)
                    throw new NullReferenceException(nameof(host.CameraDevice));
                try
                {
                    host.PreviewCaptureRequest = host.CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                    var CONTROL_AF_MODE = CaptureRequest.ControlAfMode
                           ?? throw new NullReferenceException(nameof(CaptureRequest.ControlAfMode));
                    var CONTROL_AF_MODE_CONTINUOUS_PICTURE = (int)ControlAFMode.ContinuousPicture;
                    host.PreviewCaptureRequest.Set(CONTROL_AF_MODE, CONTROL_AF_MODE_CONTINUOUS_PICTURE);
                    host.PreviewCaptureRequest.AddTarget(previewSurface);
                    host.PreviewCaptureRequest.AddTarget(recorderSurface);
                    if (host.SessionStateCallback == null)
                        throw new NullReferenceException(nameof(host.SessionStateCallback));
                    var outputs = new List<Surface> { previewSurface, recorderSurface };
                    host.CameraDevice.CreateCaptureSession(outputs, host.SessionStateCallback, host.ChildHandler);
                }
                catch (Exception e)
                {
                    host.OnError(nameof(Config), e);
                }
            }
        }

        sealed class Permission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
            {
                (Android.Manifest.Permission.Camera, true),
                (Android.Manifest.Permission.RecordAudio, true),
                (Android.Manifest.Permission.ReadExternalStorage, true),
                (Android.Manifest.Permission.WriteExternalStorage, true)
            };
        }

        public static bool StartRecorder(this IHost host)
        {
            if (host.IsInit)
            {
                if (!host.RecorderState)
                {
                    host.Config();
                    host.MediaRecorder.Start();
                    host.RecorderState = true;
                    return true;
                }
            }
            return false;
        }

        public static bool StopRecorder(this IHost host)
        {
            if (host.IsInit && host.RecorderState)
            {
                host.MediaRecorder.Stop();
                host.MediaRecorder.Reset();
                host.RecorderState = false;
                return true;
            }
            return false;
        }
    }
}
#nullable disable