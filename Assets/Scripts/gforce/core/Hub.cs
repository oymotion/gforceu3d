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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace gf
{
    public enum RetCode
    {
        /// Method returns successfully.
        GF_SUCCESS,
        /// Method returns with a generic error.
        GF_ERROR,
        /// Given parameters are not match required.
        GF_ERROR_BAD_PARAM,
        /// Method call is not allowed by the inner state.
        GF_ERROR_BAD_STATE,
        /// Method is not supported at this time.
        GF_ERROR_NOT_SUPPORT,
        /// Hub is busying on device scan and cannot fulfill the call.
        GF_ERROR_SCAN_BUSY,
        /// Insufficient resource to perform the call.
        GF_ERROR_NO_RESOURCE,
        /// A preset timer is expired.
        GF_ERROR_TIMEOUT,
        /// Target device is busy and cannot fulfill the call.
        GF_ERROR_DEVICE_BUSY,
    };

    public sealed class Hub : IDisposable
    {
        public enum WorkMode
        {
            /// Callbacks are called in the message senders' threads
            /// \remark
            /// 1. Callbacks are called in various threads.
            /// 2. No need to call Hub::run, the method will return immediately in this mode.
            Freerun,
            /// Callbacks are called in the client thread given by method Hub::run
            /// \remark
            /// Client has to call Hub::run to pull messages (represent as callbacks),
            /// otherwise messages are blocking in an inner queue without been handled.
            Polling,
        };

        public enum HubState
        {
            Idle,
            Scanning,
            Connecting,
            Disconnected,
            Unknown
        };

        public enum LogLevel
        {
            GF_LOG_VERBOSE,
            GF_LOG_DEBUG,
            GF_LOG_INFO,
            GF_LOG_WARN,
            GF_LOG_ERROR,
            GF_LOG_FATAL,
            GF_LOG_MAX
        };

        public static Hub Instance
        {
            get
            {
                return instance;
            }
        }

        public delegate void logFn(LogLevel level, string value);
        public delegate bool deviceEnumFn(Device dev);

        public void setClientLogMethod(logFn fun)
        {
            libgforce.gf_set_log_method(fun);
        }

        public RetCode init(uint comport)
        {
            RetCode ret = libgforce.hub_init(comport);
            return ret;
        }

        public RetCode deinit()
        {
            return libgforce.hub_deinit();
        }

        public void setWorkMode(WorkMode newMode)
        {
            libgforce.hub_set_workmode(newMode);
        }

        public WorkMode getWorkMode()
        {
            return libgforce.hub_get_workmode();
        }

        public HubState getStatus()
        {
            return libgforce.hub_get_status();
        }

        public RetCode registerListener(HubListener ls)
        {
            RetCode ret = RetCode.GF_SUCCESS;

            if (mListeners.Add(ls))
            {
                if (mListeners.Count() == 1)
                {
                    mListenerDele = new libgforce.ListenerDelegate();
                    mListenerDele.onScanfinishedFn = new libgforce.onScanfinished(Hub.onScanfinishedImpl);
                    mListenerDele.onStateChangedFn = new libgforce.onStateChanged(Hub.onStateChangedImpl);
                    mListenerDele.onDeviceFoundFn = new libgforce.onDeviceFound(Hub.onDeviceFoundImpl);
                    mListenerDele.onDeviceDiscardFn = new libgforce.onDeviceDiscard(Hub.onDeviceDiscardImpl);
                    mListenerDele.onDeviceConnectedFn = new libgforce.onDeviceConnected(Hub.onDeviceConnectedImpl);
                    mListenerDele.onDeviceDisconnectedFn = new libgforce.onDeviceDisconnected(Hub.onDeviceDisconnectedImpl);
                    mListenerDele.onOrientationDataFn = new libgforce.onOrientationData(Hub.onOrientationDataImpl);
                    mListenerDele.onGestureDataFn = new libgforce.onGestureData(Hub.onGestureDataImpl);
                    mListenerDele.onDeviceStatusChangedFn = new libgforce.onDeviceStatusChanged(Hub.onDeviceStatusChangedImpl);
                    mListenerDele.onExtendedDeviceDataFn = new libgforce.onExtendedDeviceData(Hub.onExtendedDeviceDataImpl);

                    ret = libgforce.hub_register_listener(ref mListenerDele);
                }
            }
            else
            {
                // if already added, do nothing but return GF_SUCCESS
            }
            return ret;
        }

        public RetCode unregisterListener(HubListener ls)
        {
            RetCode ret = RetCode.GF_SUCCESS;

            if (mListeners.Remove(ls))
            {
                if (mListeners.Count() == 0)
                {
                    ret = libgforce.hub_unregister_listener(ref mListenerDele);
                }
            }
            else
            {
                // if not exists
                ret = RetCode.GF_ERROR;
            }
            return ret;
        }

        public RetCode startScan()
        {
            return libgforce.hub_start_scan();
        }

        public RetCode stopScan()
        {
            return libgforce.hub_stop_scan();
        }

        public uint getNumDevices(bool bConnectedOnly)
        {
            return libgforce.hub_get_num_devices(bConnectedOnly);
        }

        public RetCode enumDevices(deviceEnumFn enumFn, bool bConnectedOnly)
        {
            if (null == enumFn)
                return RetCode.GF_ERROR_BAD_PARAM;

            mClientEnumFn = enumFn;
            libgforce.gfDeviceEnumFn gfEnumFn = new libgforce.gfDeviceEnumFn(onDeviceEnum);
            RetCode ret = libgforce.hub_enum_devices(gfEnumFn, bConnectedOnly);
            mClientEnumFn = null;

            return ret;
        }

        public RetCode run(uint ms)
        {
            return libgforce.hub_run(ms);
        }

        private deviceEnumFn mClientEnumFn = null;
        private static readonly Hub instance = new Hub("c# app");

        static Hub()
        {
        }

        private Hub(string identifier)
        {
            libgforce.hub_instance(identifier);
        }


        // Deterministic destructor
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // free IDisposable managed objects
                }

                mListeners.Clear();
                mDevices.Clear();
                mListenerDele = new libgforce.ListenerDelegate();
                // free unmanaged objects
                deinit();

                _disposed = true;
            }
        }


        // Finalizer (non-deterministic)
        ~Hub()
        {
            Dispose(false);
        }

        private bool _disposed = false;


        private static bool onDeviceEnum(IntPtr hDevice)
        {
            if (IntPtr.Zero == hDevice)
                return false;

            if (null == instance.mClientEnumFn)
                return false;

            // try to add new device
            Device d = new Device(hDevice);

            try
            {
                instance.mDevices.Add(hDevice, d);
            }
            catch
            {
                // already exists
                d = (Device)instance.mDevices[hDevice];
            }

            return instance.mClientEnumFn(d);
        }

        private HashSet<HubListener> mListeners = new HashSet<HubListener>();
        private Hashtable mDevices = new Hashtable();

        private libgforce.ListenerDelegate mListenerDele;


        private static void onScanfinishedImpl()
        {
            foreach (HubListener l in Instance.mListeners)
                l.onScanFinished();
        }


        private static void onStateChangedImpl(Hub.HubState state)
        {
            foreach (HubListener l in Instance.mListeners)
                l.onStateChanged(state);
        }


        private static void onDeviceFoundImpl(IntPtr hDevice)
        {
            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            // try to add new device
            Device d = new Device(hDevice);

            try
            {
                instance.mDevices.Add(hDevice, d);
            }
            catch
            {
                // already exists
                d = (Device)instance.mDevices[hDevice];
            }

            foreach (HubListener l in instance.mListeners)
                l.onDeviceFound(d);
        }

        private static void onDeviceDiscardImpl(IntPtr hDevice)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onDeviceDiscard(d);

            // remove data
            instance.mDevices.Remove(hDevice);
        }

        private static void onDeviceConnectedImpl(IntPtr hDevice)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onDeviceConnected(d);
        }

        private static void onDeviceDisconnectedImpl(IntPtr hDevice, int reason)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onDeviceDisconnected(d, reason);
        }

        private static void onOrientationDataImpl(IntPtr hDevice, float w, float x, float y, float z)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onOrientationData(d, w, x, y, z);
        }


        private static void onGestureDataImpl(IntPtr hDevice, uint gest)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onGestureData(d, gest);
        }

        private static void onDeviceStatusChangedImpl(IntPtr hDevice, Device.Status status)
        {
            Device d;
            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            foreach (HubListener l in instance.mListeners)
                l.onDeviceStatusChanged(d, status);
        }

        private static void onExtendedDeviceDataImpl(IntPtr hDevice, Device.DataType type, int dataLen, IntPtr data)
        {
            Device d;

            if (IntPtr.Zero == hDevice)
            {
                return;
            }

            try
            {
                d = (Device)instance.mDevices[hDevice];
            }
            catch
            {
                // cannot find
                return;
            }

            byte[] dataarray = new byte[dataLen];
            Marshal.Copy(data, dataarray, 0, dataLen);

            foreach (HubListener l in instance.mListeners)
                l.onExtendedDeviceData(d, type, dataarray);
        }
    }
}
