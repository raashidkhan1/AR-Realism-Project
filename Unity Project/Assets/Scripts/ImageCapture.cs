using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
#endif


namespace ThesisARImplementation
{
    public class ImageCapture
    {
        private byte[] frameData = null;
#if ENABLE_WINMD_SUPPORT
        private MediaCapture _mediaCapture;
        private List<MediaFrameReader> _sourceReaders = new List<MediaFrameReader>();
        private SoftwareBitmap colorFrame=null;

        public async void InitializeMediaCaptureAsync()
        {
            if(_mediaCapture!=null){
                return;
            }
        
            var allGroups = await MediaFrameSourceGroup.FindAllAsync();
            int selectedGroupIndex = -1;
            for (int i = 0; i < allGroups.Count; i++) {
                var group = allGroups[i];
                Debug.Log(group.DisplayName + ", " + group.Id);
                // for HoloLens 2
                if (group.DisplayName == "QC Back Camera") {
                    selectedGroupIndex = i;
                    Debug.Log( ": Selected group " + i + " on HoloLens 2");
                    break;
                }
            }
            MediaStreamType mediaStreamType = MediaStreamType.Photo;
            _mediaCapture = new MediaCapture();
            string deviceId = allGroups[selectedGroupIndex].Id;
            IReadOnlyList<MediaCaptureVideoProfile> profileList = MediaCapture.FindKnownVideoProfiles(deviceId, KnownVideoProfile.HighQualityPhoto);

            var settings = new MediaCaptureInitializationSettings
            {
                /*SourceGroup = sourceGroup,*/
                VideoDeviceId = deviceId,
                VideoProfile = profileList[0],
                // This media capture can share streaming with other apps.
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,

                // Only stream video and don't initialize audio capture devices.
                StreamingCaptureMode = StreamingCaptureMode.Video,

                // Set to CPU to ensure frames always contain CPU SoftwareBitmap images
                // instead of preferring GPU D3DSurface images.
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };
        
            await _mediaCapture.InitializeAsync(settings);
            Debug.Log(": MediaCapture is successfully initialized in ExclusiveControl mode for HoloLens 2.");
            mediaStreamType = MediaStreamType.Photo;
        
            var mediaFrameSourceVideo = _mediaCapture.FrameSources.Values.Single(x => x.Info.MediaStreamType == mediaStreamType);
            int targetVideoWidth = 160;
            int targetVideoHeight = 120;
            float targetVideoFrameRate = 30.0f;
        
            MediaFrameFormat targetResFormat = null;
            float framerateDiffMin = 60f;
            foreach (var f in mediaFrameSourceVideo.SupportedFormats.OrderBy(x => x.VideoFormat.Width * x.VideoFormat.Height)) {
                if (f.VideoFormat.Width == targetVideoWidth && f.VideoFormat.Height == targetVideoHeight ) {
                    if (targetResFormat == null) {
                        targetResFormat = f;
                        framerateDiffMin = Mathf.Abs(f.FrameRate.Numerator / f.FrameRate.Denominator - targetVideoFrameRate);
                    }
                    else if (Mathf.Abs(f.FrameRate.Numerator / f.FrameRate.Denominator - targetVideoFrameRate) < framerateDiffMin) {
                        targetResFormat = f;
                        framerateDiffMin = Mathf.Abs(f.FrameRate.Numerator / f.FrameRate.Denominator - targetVideoFrameRate);
                    }
                }
            }
        
            if (targetResFormat == null) {
                targetResFormat = mediaFrameSourceVideo.SupportedFormats[0];
                Debug.Log(": Unable to choose the selected format, fall back");
                targetResFormat = mediaFrameSourceVideo.SupportedFormats.OrderBy(x => x.VideoFormat.Width * x.VideoFormat.Height).FirstOrDefault();
            }
        
            await mediaFrameSourceVideo.SetFormatAsync(targetResFormat);
        
            // Set up frame readers, register event handlers and start streaming.
            MediaFrameReader frameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSourceVideo, targetResFormat.Subtype);

            frameReader.FrameArrived += FrameReader_FrameArrived;
            _sourceReaders.Add(frameReader); 
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame != null)
                {
                    var sourceKind = frame.SourceKind;

                    if (sourceKind == MediaFrameSourceKind.Color)
                    {
                        // Capture Color frame
                        colorFrame = frame.VideoMediaFrame.SoftwareBitmap;
                        if (colorFrame != null) 
                        {
                            Debug.Log("it works");
                            frameData = ConvertSoftwareBitmapToByteArray(colorFrame);
                        }
                        
                    }
                }
            }
        }
        
        public byte[] ConvertSoftwareBitmapToByteArray(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var bytes = new byte[4 * softwareBitmap.PixelWidth * softwareBitmap.PixelHeight];
            softwareBitmap.CopyToBuffer(bytes.AsBuffer());
            return bytes;
        }

#endif
        public byte[] GetColorFrame() {
            return frameData;
        }

    }
}