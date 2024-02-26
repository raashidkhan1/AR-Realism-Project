using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Perception.Spatial;
#endif
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.UI;

namespace ThesisARImplementation {
    public class ImageTest : MonoBehaviour {
        private ImageCapture _imageCapture;

        private ImageCaptureUtility _imageCaptureUtility;

        private HololensRmStream _rmStream;

        /*public RawImage rawRGBImage;*/
        
        public RawImage rawDepthImage;

        /*public RawImage rawRGBDImage;*/
        
        public GameObject camera;

        private Texture2D rgbdTexture;

        private ImageProcessing _imageProcessing;

        private Vector3 cameraPosition;

        private Texture2D rgb;
        private Texture2D depth;

        /*private byte[] rgbFrame;
        private byte[] depthFrame;*/

        private FrameData rgbFrame;
        private FrameData depthFrame;

        private bool _isRunning = false;
        private Task<(byte[], long)> depthDataTask = null;
        private bool isCaptured = false;
        private void Awake() {
            _imageCapture = new ImageCapture();
            _imageCaptureUtility = new ImageCaptureUtility();
            _rmStream = camera.gameObject.GetComponent<HololensRmStream>();
            _imageProcessing = new ImageProcessing();
        }

        // Start is called before the first frame update
        async void Start() {
#if ENABLE_WINMD_SUPPORT
            try {
                await _imageCaptureUtility.InitializeMediaFrameReaderAsync(160, 120);
                Debug.Log("Media Capture started");
                _isRunning = true;
            }
            catch (Exception e) {
                Debug.Log("Failed initializing media capture");
            }
#endif
        }


        async void Update() {
            // Fill rgb data 
            await ProcessRGBData();
            // Fill depth data
            await ProcessDepthData();
            try {
                /*rgb = _imageProcessing.CreateTextureFromBytes(rgbFrame.Data, 160, 120);
                 depth = _imageProcessing.ConvertDepthToTexture(320, 288, depthFrame.Data);*/
                /*rawRGBImage.texture = rgb;
                rawDepthImage.texture = depth;*/
                // combine
                if (AreSynchronizedFrames()) {
                    rgbdTexture =
                        _imageProcessing.CreateRGBDTexture(rgbFrame.Data, depthFrame.Data, 160, 120, 320, 288);
                    Debug.Log("Sync frames found");

                    rawDepthImage.texture = rgbdTexture;
                    if (!isCaptured) {
                        captureImage(rgbdTexture);
                        Debug.Log("Captured rgbd image");
                        isCaptured = true;
                    }
                }
            }
            catch (Exception e) {
                Debug.Log("Couldn't apply texture"+e.Message);
            }
        }

        async Task ProcessRGBData() {
#if ENABLE_WINMD_SUPPORT
            await Task.Run(async () => {
                try {
                    if (_imageCaptureUtility.IsCapturing) {
                        Debug.Log("Capturing images");
                        using (var videoFrame = _imageCaptureUtility.GetLatestFrame()) {
                            if (videoFrame?.SoftwareBitmap != null) {
                                Debug.Log("Converting bitmap to byte array");
                                rgbFrame = new FrameData() {
                                    Data = _imageCapture.ConvertSoftwareBitmapToByteArray(videoFrame.SoftwareBitmap),
                                    Timestamp = _imageCaptureUtility.GetTimeStamp()
                                };
                                Debug.Log("Bitmap rgbframe size" + rgbFrame.Data.Length);
                            }
                            else if (videoFrame?.Direct3DSurface != null) {
                                Debug.Log("Converting d3dsurface to byte array");
                                SoftwareBitmap softwareBitmap =
                                    await SoftwareBitmap.CreateCopyFromSurfaceAsync(videoFrame.Direct3DSurface);
                                rgbFrame = new FrameData() {
                                    Data = _imageCapture.ConvertSoftwareBitmapToByteArray(softwareBitmap),
                                    Timestamp = _imageCaptureUtility.GetTimeStamp()
                                };
                                Debug.Log("Bitmap rgbframe size" + rgbFrame.Data.Length);
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Debug.Log("Exception in frame capture");
                    Console.WriteLine(e.Message);
                }
            });

#endif
        }


        async Task getRGBData() {
            
        }
        /*
        async Task ProcessDepthData() {
#if ENABLE_WINMD_SUPPORT
            try {
                (byte[] dataResult, long timestamp) depthDataResult = (new byte[] { }, 0);
                if (depthDataTask == null) {
                    Debug.Log("starting depth");
                    depthDataTask = _rmStream.ProcessDepthData();
                }
                else if (depthDataTask.IsCompleted) {
                    if (depthDataTask.Status == TaskStatus.RanToCompletion) {
                        depthDataResult = depthDataTask.Result;
                    }
                    else {
                        // Handle any errors or cancellations here
                        Debug.Log("Depth data task failed");
                    }

                    depthDataTask = null; // Reset for next call
                }

                // using depthdataresult
                if (depthDataResult.dataResult.Length>0) {
                    Debug.Log("depth frame size " + depthDataResult.dataResult.Length);
                    depthFrame = new FrameData() {
                        Data = depthDataResult.dataResult,
                        Timestamp = depthDataResult.timestamp
                    };
                }

            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
#endif
        }
        */


        async Task ProcessDepthData() {
#if ENABLE_WINMD_SUPPORT
            try {
                var depthDataResult = await _rmStream.ProcessDepthData();

                if (depthDataResult.Item1.Length > 0) {
                    depthFrame = new FrameData() {
                        Data = depthDataResult.Item1,
                        Timestamp = depthDataResult.Item2
                    };
                }
                else {
                    Debug.Log("ProcessDData: Received empty depth data.");
                }
            }
            catch (Exception e) {
                Debug.Log($"ProcessDData: Error processing depth data: {e.Message}");
            }
#endif
        }

        private bool AreSynchronizedFrames() {
            // Assuming rgbFrame and depthFrame are already populated
            if (rgbFrame != null && depthFrame != null) {
                // Check if the timestamps are within an acceptable range
                long timeDifference = Math.Abs(rgbFrame.Timestamp - depthFrame.Timestamp);

                Debug.Log("AreSynchronizedFrames: TimeDifference: " + timeDifference);
                // Define a threshold for how close the timestamps need to be
                long acceptableThreshold = TimeSpan.FromMilliseconds(100).Ticks;

                if (timeDifference <= acceptableThreshold) {
                    // Timestamps are close enough, process the synchronized frames
                    return true;
                }
            }

            return false;
        }

        private void captureImage(Texture2D rgbd) {
            byte[] file = rgbd.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, "rgbd_image.png");
            File.WriteAllBytes(path, file);

        }

        async void OnDestroy() {
            _isRunning = false;
            if (_imageCaptureUtility != null) {
                await _imageCaptureUtility.StopMediaFrameReaderAsync();
            }
        }
    }
}