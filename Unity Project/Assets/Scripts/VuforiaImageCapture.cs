using System;
using Vuforia;
using UnityEngine;

namespace ThesisARImplementation {
    public class VuforiaImageCapture: MonoBehaviour {

        private long _timeStamp;

        private CameraDevice _cameraDevice;
        private void Start() {
            VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
            _cameraDevice = VuforiaBehaviour.Instance.CameraDevice;
        }

        private void OnVuforiaStarted() {
            _cameraDevice.SetFrameFormat(PixelFormat.RGBA8888, true);
        }

        public byte[] getImageFromVuforiaCamera() {
            Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGBA8888);
            byte[] result = image.Pixels;
            _timeStamp = DateTime.UtcNow.Ticks;
            return result;
        }

        public long getTimeStamp() {
            return _timeStamp;
        }


        private void OnDestroy() {
            VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
        }
    }
}