using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;
using Unity.Barracuda;

#if ENABLE_WINMD_SUPPORT
using Windows.Graphics.Imaging;
using Windows.Media;
#endif

namespace ThesisARImplementation {
    public class FrameData {
        public byte[] Data { get; set; }
        public long Timestamp { get; set; }
    }

    public class LightEstimation : MonoBehaviour {
        public Light dominantLight;

        public GameObject camera;

        private ImageCapture _imageCapture;

        private ImageCaptureUtility _imageCaptureUtility;

        private HololensRmStream _rmStream;

        public NNModel deepRelightModel;

        private Model runtimeModel;

        private IWorker worker;

        private Vector3 lightCoordinates;
        private float lightIntensity = 0.0f;

        private ImageProcessing _imageProcessing;
        private bool _isRunning = false;


        private FrameData rgbFrame;
        private FrameData depthFrame;

        private Vector3 cameraPosition;
        private Quaternion cameraRotation;

        private Vector3 resultCoordinates = Vector3.zero;
        private float resultIntensity;

        private bool frameCaptureSuccess = false;

        private Task<(byte[], long)> depthDataTask = null;

        private bool framesProcessed = false;

        private List<Vector3> estimatedLightDirections;

        // update with experiment results
        private float neighborhoodThreshold = 0.1f;
        private float smoothChangeThreshold = 0.1f;
        private int temporalThreshold = 4;

        private Vector3 smoothedDirection = Vector3.zero;

        private Quaternion cameraPose = Quaternion.identity;
        
        public float intensityFactor = 0.01f;
        private void Awake() {
            // initialize required objects, set values
            _imageCapture = new ImageCapture();
            _imageCaptureUtility = new ImageCaptureUtility();
            _rmStream = camera.gameObject.GetComponent<HololensRmStream>();
            _imageProcessing = new ImageProcessing();
            estimatedLightDirections = new List<Vector3>();
        }

        private async void Start() {
            // initialize the image stream for rgb, load model
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
            runtimeModel = ModelLoader.Load(deepRelightModel);
            if (runtimeModel == null) {
                Debug.Log("model load failed");
            }
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
        }

        private async void Update() {
#if ENABLE_WINMD_SUPPORT
            // Fill rgb data 
            await ProcessRGBData();
            // Fill depth data
            await ProcessDepthData();
            //process ths data
            if (AreSynchronizedFrames()) {
                Debug.Log("Update: Got Sync Frames");
                Texture2D inputImage = _imageProcessing.CreateRGBDTexture(rgbFrame.Data, depthFrame.Data, 160, 120, 320, 288);
                Debug.Log("Update: Converted to RGBD Texture");
                framesProcessed = true;
                // infer from the model
                Debug.Log("Update: Inferring from model");
                float[] result = await EstimateLightParameters(inputImage);
                if (result.Length == 0) {
                    Debug.Log("Got nothing from estimation");
                    return;
                }
                Debug.Log("Update: got result from estimate function"+result[0]+","+result[1]+", intensity"+result[2]);
                
                // check for temporal smoothing

                //assuming radius as 1
                resultCoordinates = getCartesianCoordinates(1, result[0], result[1]);
                resultIntensity = result[2];
                
                Debug.Log("Update: result coordinates "+resultCoordinates);
                
                Debug.Log("Update: checking smoothing");
                SmoothingLightDirection(resultCoordinates);


                if (smoothedDirection != Vector3.zero) {
                    Debug.Log("Update: Got smoothed direction"+smoothedDirection);

                    Debug.Log("Update: getting camera pose");
                    // Transforming to world space by adding camera pose
                    /*cameraPose = _imageCaptureUtility.GetCameraEulerAngles();*/

                    // if we got the camera rotation, add to smoothed
                    if (cameraPose != Quaternion.identity) {
                        Debug.Log("Update: camera rotation applied");
                        var cameraRotatedDirection = cameraPose * Quaternion.Euler(smoothedDirection);

                        // update light coordinates and intensity
                        if (resultCoordinates != Vector3.zero && resultIntensity != 0.0f) {
                            Debug.Log("Update: Updating light parameters");
                            UpdateLightParameters(cameraRotatedDirection, resultIntensity, intensityFactor);
                        }
                    }
                    else {
                        if (resultCoordinates != Vector3.zero && resultIntensity != 0.0f) {
                            Debug.Log("Update: Updating light parameters");
                            UpdateLightParameters(Quaternion.Euler(smoothedDirection), resultIntensity, intensityFactor);
                        }
                    }

                }

            }

            if (framesProcessed && rgbFrame != null) {
                rgbFrame = null;
            }

            if (framesProcessed && depthFrame != null) {
                depthFrame = null;
            }

            framesProcessed = false;
#endif
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

        private async Task<float[]> EstimateLightParameters(Texture2D rgbdImage) {
            Tensor input = new Tensor(rgbdImage, 4);
            worker.Execute(input);
            Tensor angleOutput = worker.PeekOutput("angle_output");
            Tensor intensityOutput = worker.PeekOutput("intensity_output");
            float[] asyncResult = new float[3];
            var angleArray = angleOutput.ToReadOnlyArray();
            asyncResult[0] = angleArray[0]; // phi
            asyncResult[1] = angleArray[1]; // theta
        
            var intensityArray = intensityOutput.ToReadOnlyArray();
            asyncResult[2] = intensityArray[0]; // intensity
            
            angleOutput.Dispose();
            intensityOutput.Dispose();
            
            input.Dispose();
            
            return asyncResult;
        
            
        }


        private void UpdateLightParameters(Quaternion direction, float intensity, float factor) {
            dominantLight.transform.rotation = direction;
            intensity = intensity * factor;
            dominantLight.intensity = intensity;
        }

        private Vector3 getCartesianCoordinates(float radius, float theta, float phi) {
            float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
            float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = radius * Mathf.Cos(theta);
            Debug.Log("Cartesian coordinates from light estimation "+(x, y, z));
            return new Vector3(x, y, z);
        }

        public static (double theta, double phi) CartesianToSpherical(Vector3 position) {
            double x = position.x;
            double y = position.y;
            double z = position.z;

            double r = Math.Sqrt(x * x + y * y + z * z);

            // Ensure r is not zero to avoid division by zero
            if (r == 0) {
                return (0, 0);
            }

            double theta = Math.Atan2(y, x); // Azimuthal angle
            double phi = Math.Acos(z / r); // Polar angle

            // Convert radians to degrees if necessary
            theta = theta * (180.0 / Math.PI);
            phi = phi * (180.0 / Math.PI);

            return (theta, phi);
        }


        private bool AreSynchronizedFrames() {
            // Assuming rgbFrame and depthFrame are already populated
            if (rgbFrame != null && depthFrame != null) {
                // Check if the timestamps are within an acceptable range
                long timeDifference = Math.Abs(rgbFrame.Timestamp - depthFrame.Timestamp);

                Debug.Log("TimeDifference:" + timeDifference);
                // Define a threshold for how close the timestamps need to be
                long acceptableThreshold = 10000; // adjust

                if (timeDifference <= acceptableThreshold) {
                    // Timestamps are close enough, process the synchronized frames
                    return true;
                }
            }

            return false;
        }

        private void SmoothingLightDirection(Vector3 currentEstimatedLightDirection) {
            if (currentEstimatedLightDirection == null || estimatedLightDirections == null) {
                Debug.Log("estimated light directions has issue or the current estimated one");
                return;
            }
            
            if (estimatedLightDirections.Count >= temporalThreshold) {
                estimatedLightDirections.RemoveAt(0);
            }

            estimatedLightDirections.Add(currentEstimatedLightDirection);
            Debug.Log("SmoothingLightDirection: Added current estimation "+estimatedLightDirections);
            if (estimatedLightDirections.Count == temporalThreshold) {
                int count = estimatedLightDirections.Count;
                Vector3 firstDerivative = currentEstimatedLightDirection -
                                          estimatedLightDirections[count - 2];
                Vector3 secondDerivative = firstDerivative -
                                           (estimatedLightDirections[count - 2] - estimatedLightDirections[count - 3]);

                bool isOutlier = firstDerivative.magnitude > neighborhoodThreshold &&
                                 secondDerivative.magnitude > smoothChangeThreshold;

                if (!isOutlier) {
                    foreach (Vector3 direction in estimatedLightDirections) {
                        smoothedDirection += direction;
                    }

                    // mean of all last inliers
                    smoothedDirection /= estimatedLightDirections.Count;
                    Debug.Log("SmoothingLightDirection: updated smoothed direction");
                }
                else {
                    smoothedDirection = Vector3.zero;
                }
            }
        }

        private async void OnDestroy() {
            _isRunning = false;
            if (_imageCaptureUtility != null) {
                await _imageCaptureUtility.StopMediaFrameReaderAsync();
            }
        }
    }
}