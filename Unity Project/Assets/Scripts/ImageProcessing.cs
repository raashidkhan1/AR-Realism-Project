using System;
using UnityEngine;

namespace ThesisARImplementation {
    public class ImageProcessing {

        public Texture2D rgbdTexture;

        public ImageProcessing() {
            rgbdTexture = new Texture2D(160, 120, TextureFormat.RGBA32, false);
        }
        public Texture2D CreateTextureFromBytes(byte[] bgraData, int width, int height) {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int length = width * height;
            Color32[] colors = new Color32[length];

            for (int i = 0; i < length; i++) {
                int idx = i * 4; // 4 bytes per pixel: BGRA
                int x = i % width;
                int y = i / width;
                // Flip the texture by modifying the y-coordinate
                colors[(height - 1 - y) * width + x] = new Color32(bgraData[idx + 2], bgraData[idx + 1], bgraData[idx], 255); // Convert BGRA to RGBA
            }

            texture.SetPixels32(colors);
            texture.Apply();

            return texture;
        }


        private void NormalizeDepthTexture(Texture2D texture) {
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                float gray = pixels[i].grayscale;
                pixels[i] = new Color(gray, gray, gray, 1);
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private Texture2D CombineTextures(Texture2D rgbTexture, Texture2D depthTexture) {
            int width = rgbTexture.width;
            int height = rgbTexture.height;
            Texture2D combinedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // resize depth texture----

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Color rgbColor = rgbTexture.GetPixel(x, y);
                    Color depthColor = depthTexture.GetPixel(x, y);

                    // Assuming depth data is stored in the red channel;
                    Color combinedColor = new Color(rgbColor.r, rgbColor.g, rgbColor.b, depthColor.r);
                    combinedTexture.SetPixel(x, y, combinedColor);
                }
            }

            combinedTexture.Apply();
            return combinedTexture;
        }

        Texture2D ResizeDepth(Texture2D depthTexture) {
            Texture2D resizedDepthTexture = new Texture2D(160, 120, TextureFormat.R16, false);

            for (int y = 0; y < resizedDepthTexture.height; y++) {
                for (int x = 0; x < resizedDepthTexture.width; x++) {
                    // Calculate the corresponding position in the original texture
                    int originalX = (int)(x * ((float)depthTexture.width / resizedDepthTexture.width));
                    int originalY = (int)(y * ((float)depthTexture.height / resizedDepthTexture.height));

                    // Get the color from the original texture and set it to the new texture
                    Color color = depthTexture.GetPixel(originalX, originalY);
                    resizedDepthTexture.SetPixel(x, y, color);
                }
            }

            resizedDepthTexture.Apply();
            return resizedDepthTexture;

        }

        /*public Texture2D ConvertDepthToTexture(int originalWidth, int originalHeight, ushort[] depthData) {
            int width = originalWidth;
            int height = originalHeight;
            Texture2D depthTexture = new Texture2D(width, height, TextureFormat.R16, false);

            try {
                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        ushort depthValue = depthData[y * width + x];
                        float depthNormalized = (float)depthValue / ushort.MaxValue;
                        Color color = new Color(depthNormalized, depthNormalized, depthNormalized, 1);
                        // Flip the texture by modifying the y-coordinate
                        depthTexture.SetPixel(x, height - 1 - y, color);
                    }
                }
                depthTexture.Apply();
            }
            catch (Exception e) {
                Console.WriteLine("Exception in conversion"+e.Message);
            }

            return depthTexture;
        }*/
        
        /*public Texture2D CreateRGBDTexture(byte[] bgraData, byte[] depthBytes, int width, int height) {
            int length = width * height;
            Color32[] colors = new Color32[length];

            for (int i = 0; i < length; i++) {
                int rgbIdx = i * 4; // 4 bytes per pixel: BGRA
                int depthIdx = i * 2; // 2 bytes per depth pixel

                // Extracting depth value and normalizing it
                ushort depthValue = BitConverter.ToUInt16(depthBytes, depthIdx);
                float depthNormalized = depthValue / (float)ushort.MaxValue;

                // Flip the texture by modifying the y-coordinate
                int x = i % width;
                int y = i / width;
                int flippedIndex = (height - 1 - y) * width + x;

                // Combine RGB and Depth into RGBA
                colors[flippedIndex] = new Color32(bgraData[rgbIdx + 2], bgraData[rgbIdx + 1], bgraData[rgbIdx], (byte)(depthNormalized * 255));
            }

            rgbdTexture.SetPixels32(colors);
            rgbdTexture.Apply();

            return rgbdTexture;
        }*/

        /*public Texture2D CreateRGBDTexture(byte[] bgraData, byte[] depthBytes, int rgbWidth, int rgbHeight,
            int depthWidth, int depthHeight) {
            Texture2D rgbdTexture = new Texture2D(rgbWidth, rgbHeight, TextureFormat.RGBA32, false);
            Color32[] colors = new Color32[rgbWidth * rgbHeight];

            for (int y = 0; y < rgbHeight; y++) {
                for (int x = 0; x < rgbWidth; x++) {
                    int rgbIdx = (y * rgbWidth + x) * 4; // 4 bytes per pixel: BGRA
                    // Calculate corresponding coordinates in the depth image
                    int depthX = (int)(x * ((float)depthWidth / rgbWidth));
                    int depthY = (int)(y * ((float)depthHeight / rgbHeight));
                    int depthIdx = (depthY * depthWidth + depthX) * 2; // 2 bytes per depth pixel

                    // Extract depth value and normalize it
                    ushort depthValue = BitConverter.ToUInt16(depthBytes, depthIdx);
                    float depthNormalized = depthValue / (float)ushort.MaxValue;

                    // Flip the texture by modifying the y-coordinate
                    int flippedIndex = (rgbHeight - 1 - y) * rgbWidth + x;

                    // Combine RGB and Depth into RGBA
                    colors[flippedIndex] = new Color32(bgraData[rgbIdx + 2], bgraData[rgbIdx + 1], bgraData[rgbIdx],
                        (byte)(depthNormalized * 255));
                }
            }

            rgbdTexture.SetPixels32(colors);
            rgbdTexture.Apply();

            return rgbdTexture;
        }*/

        public Texture2D CreateRGBDTexture(byte[] bgraData, byte[] depthBytes, int rgbWidth, int rgbHeight,
            int depthWidth, int depthHeight) {
            Texture2D rgbdTexture = new Texture2D(rgbWidth, rgbHeight, TextureFormat.RGBA32, false);
            Color32[] colors = new Color32[rgbWidth * rgbHeight];

            // Determine the scaling factors considering the different resolutions and potential FoV differences
            float scaleX = (float)depthWidth / rgbWidth;
            float scaleY = (float)depthHeight / rgbHeight;
            float offsetX = (depthWidth - scaleX * rgbWidth) / 2f; // Centering the depth data in the RGB field of view
            float offsetY = (depthHeight - scaleY * rgbHeight) / 2f;

            for (int y = 0; y < rgbHeight; y++) {
                for (int x = 0; x < rgbWidth; x++) {
                    int rgbIdx = (y * rgbWidth + x) * 4; // 4 bytes per pixel: BGRA

                    // Calculate corresponding coordinates in the depth image
                    int depthX = (int)(x * scaleX + offsetX);
                    int depthY = (int)(y * scaleY + offsetY);
                    int depthIdx = (depthY * depthWidth + depthX) * 2; // 2 bytes per depth pixel

                    // Extract depth value and normalize it
                    ushort depthValue = BitConverter.ToUInt16(depthBytes, depthIdx);

                    // Adjust the normalization factor based on the observed depth range
                    // This factor should be set to the maximum depth value that you expect to receive from the sensor
                    float maxDepth = 4000.0f; // Example: 4 meters, adjust this based on your sensor's range
                    float depthNormalized = depthValue / maxDepth;

                    // Clamp the value to ensure it's within [0, 1]
                    depthNormalized = Mathf.Clamp01(depthNormalized);

                    // Convert the normalized depth to a byte value for the alpha channel
                    byte depthByte = (byte)(depthNormalized * 255);

                    // Flip the texture by modifying the y-coordinate
                    int flippedIndex = (rgbHeight - 1 - y) * rgbWidth + x;

                    // Combine RGB and Depth into RGBA
                    colors[flippedIndex] = new Color32(bgraData[rgbIdx + 2], bgraData[rgbIdx + 1], bgraData[rgbIdx],
                        depthByte);
                }
            }

            rgbdTexture.SetPixels32(colors);
            rgbdTexture.Apply();

            return rgbdTexture;
        }

        public Texture2D CreateRGBDTexture2(byte[] rgbaData, byte[] depthBytes, int rgbWidth, int rgbHeight,
            int depthWidth, int depthHeight) {
            Texture2D rgbdTexture = new Texture2D(rgbWidth, rgbHeight, TextureFormat.RGBA32, false);
            Color32[] colors = new Color32[rgbWidth * rgbHeight];

            // Determine the scaling factors considering the different resolutions and potential FoV differences
            float scaleX = (float)depthWidth / rgbWidth;
            float scaleY = (float)depthHeight / rgbHeight;
            float offsetX = (depthWidth - scaleX * rgbWidth) / 2f; // Centering the depth data in the RGB field of view
            float offsetY = (depthHeight - scaleY * rgbHeight) / 2f;

            for (int y = 0; y < rgbHeight; y++) {
                for (int x = 0; x < rgbWidth; x++) {
                    int rgbIdx = (y * rgbWidth + x) * 4; // 4 bytes per pixel: RGBA

                    // Calculate corresponding coordinates in the depth image
                    int depthX = (int)(x * scaleX + offsetX);
                    int depthY = (int)(y * scaleY + offsetY);
                    int depthIdx = (depthY * depthWidth + depthX) * 2; // 2 bytes per depth pixel

                    // Extract depth value and normalize it
                    ushort depthValue = BitConverter.ToUInt16(depthBytes, depthIdx);

                    // Adjust the normalization factor based on the observed depth range
                    float maxDepth = 4000.0f; // Example: 4 meters, adjust this based on your sensor's range
                    float depthNormalized = depthValue / maxDepth;

                    // Clamp the value to ensure it's within [0, 1]
                    depthNormalized = Mathf.Clamp01(depthNormalized);

                    // Convert the normalized depth to a byte value for the alpha channel
                    byte depthByte = (byte)(depthNormalized * 255);

                    // Flip the texture by modifying the y-coordinate
                    int flippedIndex = (rgbHeight - 1 - y) * rgbWidth + x;

                    // Combine RGB and Depth into RGBA
                    colors[flippedIndex] = new Color32(rgbaData[rgbIdx], rgbaData[rgbIdx + 1], rgbaData[rgbIdx + 2],
                        depthByte);
                }
            }

            rgbdTexture.SetPixels32(colors);
            rgbdTexture.Apply();

            return rgbdTexture;
        }

        
        
        public Texture2D ConvertDepthToTexture(int originalWidth, int originalHeight, byte[] depthBytes) {
            int width = originalWidth;
            int height = originalHeight;
            Texture2D depthTexture = new Texture2D(width, height, TextureFormat.R16, false);

            try {
                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        // Calculate the index for the depthBytes array
                        int index = (y * width + x) * 2; // 2 bytes per depth pixel

                        // Convert two bytes to one ushort (assuming little endian)
                        ushort depthValue = BitConverter.ToUInt16(depthBytes, index);
                        float depthNormalized = (float)depthValue / ushort.MaxValue;
                        Color color = new Color(depthNormalized, depthNormalized, depthNormalized, 1);
                        // Flip the texture by modifying the y-coordinate
                        depthTexture.SetPixel(x, height - 1 - y, color);
                    }
                }

                depthTexture.Apply();
            }
            catch (Exception e) {
                Debug.Log(e.Message);
                // Depending on your environment, you might need to use Debug.Log or Console.WriteLine
                // Debug.LogError("Exception in conversion: " + e.Message);
            }

            return depthTexture;
        }
        

        byte[] ResizeTexture(byte[] frameData, int originalHeight, int originalWidth, int targetWidth,
            int targetHeight) {
            Texture2D texture = new Texture2D(originalWidth, originalHeight, TextureFormat.RGBA32, false);
            ;
            texture.LoadRawTextureData(frameData);
            texture.Apply();
            RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, texture.format, false);
            resizedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            resizedTexture.Apply();
            RenderTexture.active = null;
            rt.Release();

            return resizedTexture.GetRawTextureData();
        }


        /*Texture2D ResizeDepth(Texture2D depthTexture) {
            Texture2D resizedDepthTexture = new Texture2D(160, 120, TextureFormat.R16, false);

            for (int y = 0; y < resizedDepthTexture.height; y++) {
                for (int x = 0; x < resizedDepthTexture.width; x++) {
                    // Calculate the corresponding position in the original texture
                    int originalX = (int)(x * ((float)depthTexture.width / resizedDepthTexture.width));
                    int originalY = (int)(y * ((float)depthTexture.height / resizedDepthTexture.height));

                    // Get the color from the original texture and set it to the new texture
                    Color color = depthTexture.GetPixel(originalX, originalY);
                    resizedDepthTexture.SetPixel(x, y, color);
                }
            }

            resizedDepthTexture.Apply();
            return resizedDepthTexture;
        }*/

    }
}