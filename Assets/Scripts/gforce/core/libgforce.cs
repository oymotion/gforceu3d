/*
 * Copyright 2017, OYMotion Inc.
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
 * COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
 * OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF
 * THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 */

using System;
using System.Text;
using System.Runtime.InteropServices;


namespace gf
{
    public enum DataNotifFlags {
			/// Data Notify All Off
			DNF_OFF = 0x00000000,
			/// Accelerate On(C.7)
			DNF_ACCELERATE = 0x00000001,
			/// Gyroscope On(C.8)
			DNF_GYROSCOPE = 0x00000002,
			/// Magnetometer On(C.9)
			DNF_MAGNETOMETER = 0x00000004,
			/// Euler Angle On(C.10)
			DNF_EULERANGLE = 0x00000008,
			/// Quaternion On(C.11)
			DNF_QUATERNION = 0x00000010,
			/// Rotation Matrix On(C.12)
			DNF_ROTATIONMATRIX = 0x00000020,
			/// EMG Gesture On(C.13)
			DNF_EMG_GESTURE = 0x00000040,
			/// EMG Raw Data On(C.14)
			DNF_EMG_RAW = 0x00000080,
			/// HID Mouse On(C.15)
			DNF_HID_MOUSE = 0x00000100,
			/// HID Joystick On(C.16)
			DNF_HID_JOYSTICK = 0x00000200,
			/// Device Status On(C.17)
			DNF_DEVICE_STATUS = 0x00000400
		};


    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void AsyncCallback(System.IntPtr hDevice, uint result);
    public delegate void AsyncStringCallback(System.IntPtr hDevice, uint result, string val);
    public delegate void AsyncUIntCallback(System.IntPtr hDevice, uint result, uint val);


    internal static class libgforce
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        private const string GFORCE_DLL = "gforcecsharp64";
#elif UNITY_ANDROID
        private const string GFORCE_DLL = "gforcecsharp";
#elif WIN64
        private const string GFORCE_DLL = "gforcecsharp64";
#elif WIN32
        private const string GFORCE_DLL = "gforcecsharp32";
#endif
        // listener callbacks
        public delegate void onScanfinished();
        public delegate void onStateChanged(Hub.HubState state);
        public delegate void onDeviceFound(System.IntPtr hDevice);
        public delegate void onDeviceDiscard(System.IntPtr hDevice);
        public delegate void onDeviceConnected(System.IntPtr hDevice);
        public delegate void onDeviceDisconnected(System.IntPtr hDevice, int reason);
        public delegate void onOrientationData(System.IntPtr hDevice, float w, float x, float y, float z);
        public delegate void onGestureData(System.IntPtr hDevice, uint gest);
        public delegate void onDeviceStatusChanged(System.IntPtr hDevice, Device.Status status);
        public delegate void onExtendedDeviceData(System.IntPtr hDevice, Device.DataType type, int dataLen, System.IntPtr data);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct ListenerDelegate
        {
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onScanfinished onScanfinishedFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onStateChanged onStateChangedFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onDeviceFound onDeviceFoundFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onDeviceDiscard onDeviceDiscardFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onDeviceConnected onDeviceConnectedFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onDeviceDisconnected onDeviceDisconnectedFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onOrientationData onOrientationDataFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onGestureData onGestureDataFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onDeviceStatusChanged onDeviceStatusChangedFn;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public onExtendedDeviceData onExtendedDeviceDataFn;
        }

        [DllImport(GFORCE_DLL, EntryPoint = "gf_set_log_method")]
        public static extern void gf_set_log_method(Hub.logFn lf);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_instance")]
        public static extern RetCode hub_instance(string identifier);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_init")]
        public static extern RetCode hub_init(uint comport);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_deinit")]
        public static extern RetCode hub_deinit();

        [DllImport(GFORCE_DLL, EntryPoint = "hub_set_workmode")]
        public static extern void hub_set_workmode(Hub.WorkMode wm);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_get_workmode")]
        public static extern Hub.WorkMode hub_get_workmode();

        [DllImport(GFORCE_DLL, EntryPoint = "hub_get_status")]
        public static extern Hub.HubState hub_get_status();

        [DllImport(GFORCE_DLL, EntryPoint = "hub_register_listener")]
        public static extern RetCode hub_register_listener(ref ListenerDelegate listener);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_unregister_listener")]
        public static extern RetCode hub_unregister_listener(ref ListenerDelegate listener);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_start_scan")]
        public static extern RetCode hub_start_scan();

        [DllImport(GFORCE_DLL, EntryPoint = "hub_stop_scan")]
        public static extern RetCode hub_stop_scan();

        [DllImport(GFORCE_DLL, EntryPoint = "hub_get_num_devices")]
        public static extern uint hub_get_num_devices(bool bConnectedOnly);

        public delegate bool gfDeviceEnumFn(IntPtr hDevice);
        [DllImport(GFORCE_DLL, EntryPoint = "hub_enum_devices")]
        public static extern RetCode hub_enum_devices(gfDeviceEnumFn enumFn, bool bConnectedOnly);

        [DllImport(GFORCE_DLL, EntryPoint = "hub_run")]
        public static extern RetCode hub_run(uint ms);

        // Device methods
        [DllImport(GFORCE_DLL, EntryPoint = "device_get_addr_type")]
        public static extern uint device_get_addr_type(IntPtr hDevice);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_address")]
        public static extern RetCode device_get_address(IntPtr hDevice, [Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder addr, uint buflen);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_name")]
        public static extern RetCode device_get_name(IntPtr hDevice, [Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder name, uint buflen);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_rssi")]
        public static extern uint device_get_rssi(IntPtr hDevice);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_connection_status")]
        public static extern Device.ConnectionStatus device_get_connection_status(IntPtr hDevice);

        [DllImport(GFORCE_DLL, EntryPoint = "device_connect")]
        public static extern RetCode device_connect(IntPtr hDevice);

        [DllImport(GFORCE_DLL, EntryPoint = "device_disconnect")]
        public static extern RetCode device_disconnect(IntPtr hDevice);

        [DllImport(GFORCE_DLL, EntryPoint = "device_set_emg_config")]
        public static extern RetCode device_set_emg_config(IntPtr hDevice, uint sampleRateHz, uint interestedChannels, uint packageDataLength, uint adcResolution, AsyncCallback cb);

        [DllImport(GFORCE_DLL, EntryPoint = "device_set_data_switch")]
        public static extern RetCode device_set_data_switch(IntPtr hDevice, uint notifSwitch, AsyncCallback cb);

        [DllImport(GFORCE_DLL, EntryPoint = "device_enable_data_notification")]
        public static extern RetCode device_enable_data_notification(IntPtr hDevice, uint enable);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_firmware_ver")]
        public static extern RetCode device_get_firmware_ver(IntPtr hDevice, AsyncStringCallback cb);

        [DllImport(GFORCE_DLL, EntryPoint = "device_get_battery_level")]
        public static extern RetCode device_get_battery_level(IntPtr hDevice, AsyncUIntCallback cb);
    }
}
