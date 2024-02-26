using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

#if ENABLE_WINMD_SUPPORT
using Windows.UI.Xaml;
using Windows.Graphics.Imaging;
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;
using System.Threading;

namespace ThesisARImplementation {
    public class HololensRmStream : MonoBehaviour {
        
#if ENABLE_WINMD_SUPPORT
            [DllImport("HL2RmStreamUnityPlugin", EntryPoint = "Initialize", CallingConvention = CallingConvention.StdCall)]
            public static extern void InitializeDll();
            
            [DllImport("HL2RmStreamUnityPlugin", EntryPoint = "GetLongThrowDepthData", CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr GetLongThrowDepthData(out int dataSize);
            
            [DllImport("HL2RmStreamUnityPlugin", EntryPoint = "FreeUnmanagedMemory", CallingConvention = CallingConvention.StdCall)]
            public static extern void FreeUnmanagedMemory(IntPtr ptr);
#endif

        private void Start() 
        {
#if ENABLE_WINMD_SUPPORT
               Debug.Log("Initializing DLL");
               InitializeDll();
               Debug.Log("DLL initialized");
#endif   
        }
#if ENABLE_WINMD_SUPPORT
        /*public async Task<byte[]> ProcessDepthData() {
            byte[] data = new byte[]{};
            return await Task.Run(() => {
                int dataSize = 0;
                IntPtr dataPtr = GetLongThrowDepthData(out dataSize);
                Debug.Log("Depth data received from dll "+dataSize);
                
                if (dataPtr != IntPtr.Zero) {
                    // Convert the unmanaged data to a managed array
                    data = new byte[dataSize];
                    Marshal.Copy(dataPtr, data, 0, dataSize);
                    Debug.Log("Inside process depth data");
                    // Free the unmanaged memory after use
                    FreeUnmanagedMemory(dataPtr);
                    Debug.Log("Freed Memory");
                }
                return data;
            });

        }*/
        
        public async Task<(byte[], long)> ProcessDepthData() {
            byte[] rawData = null;
            byte[] depthData = null;
            long timeStamp = 0;
            await Task.Run(() => {
                int dataSize = 0;
                IntPtr dataPtr = GetLongThrowDepthData(out dataSize);
                Debug.Log("ProcessDepthData: Depth data received from dll " + dataSize);
                if (dataPtr != IntPtr.Zero) {
                    Debug.Log("ProcessDepthData: Inside process depth data");
                    // Allocate a byte array to hold the raw data
                    rawData = new byte[dataSize];
                    Marshal.Copy(dataPtr, rawData, 0, dataSize);
                    
                    timeStamp = BitConverter.ToInt64(rawData, 0);
                    Debug.Log("ProcessDepthData: timestamp"+timeStamp);
                    depthData = new byte[rawData.Length - sizeof(long)];
                    Array.Copy(rawData, sizeof(long), depthData, 0, depthData.Length);
                    
                    long _csharptimeStamp = DateTime.UtcNow.Ticks;

                    if (_csharptimeStamp != timeStamp) {
                        Debug.Log("ProcessDepthData: time difference"+Math.Abs(_csharptimeStamp - timeStamp));
                    }
                    
                    // Convert the byte array to a ushort array
                    /*depthData = new ushort[dataSize / 2];
                    for (int i = 0; i < dataSize; i += 2) {
                        depthData[i / 2] = BitConverter.ToUInt16(rawData, i);
                    }*/

                    Debug.Log("ProcessDepthData: Free Memory on C++");
                    // Free the unmanaged memory after use
                    FreeUnmanagedMemory(dataPtr);
                }
            });

            return (depthData, timeStamp);
        }
        
        
#endif       
    }
}