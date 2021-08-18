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
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

using GForce;
using Gesture = gf.Device.Gesture;
using Quaternion = UnityEngine.Quaternion;
using gf;

public class GestureOnBall : MonoBehaviour
{
    private bool initilized = false;
    private UIListener simpleUIListener;
    private GForceListener gForceListener;
    private GForceDevice gForceDevice;


    public Quaternion quaternion; // for display in editor only
    public uint lastGesture = 0;

    public Material relaxMaterial;
    public Material shootMaterial;
    public Material otherMaterial;
    public Material undefinedMaterial;

    private DateTime relaxStart = DateTime.Now;
    private const int RelaxDelay = 300;

    private static string firmwareVer;  // We will use firmwareVer in callback

    public long lastBatteryUpdateTime = 0;


    // Use this for initialization
    void Start()
    {
        if (!initilized)
        {
            // Create UI releated events listener, here we use a simple listerner without GUI
            simpleUIListener = new SimpleListener();

            // Create a GForceListener to receive all device releated events
            gForceListener = new GForceListener();
            
            // Created a gForce device
            gForceDevice = new GForceDevice();

            // Register UI listener, multiple instances of UIListener could be registered
            gForceListener.RegisterUIListener(simpleUIListener);

            // Register device, multiple devices could be registered
            // But you must implement connecting to multi devices in your subclass of UIListener 
            gForceListener.RegisterGForceDevice(gForceDevice);

            // Run the whole gForce system
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                GForceHub.instance.Prepare();
            }));

            string[] strs = new string[] {
                "android.permission.BLUETOOTH",
                "android.permission.BLUETOOTH_ADMIN",
                //"android.permission.ACCESS_COARSE_LOCATION",
                "android.permission.ACCESS_FINE_LOCATION"
            };

            strs.ToList().ForEach(s =>
            {
                Permission.RequestUserPermission(s);
                Debug.Log("add RequestUserPermission: " + s);
            });
#else
            GForceHub.instance.Prepare();
#endif

            // Register GForceListener to receive & dispatch events
            Hub.Instance.registerListener(gForceListener);

            // Start can. SimpleListener will process events like scan finished, device found, device connected, etc.
            Hub.Instance.startScan();

            // Set data notification switch
            gForceDevice.SetDataSwitch((uint)(DataNotifFlags.DNF_DEVICE_STATUS | DataNotifFlags.DNF_QUATERNION | DataNotifFlags.DNF_EMG_GESTURE));

            initilized = true;
        }

        GetComponent<Renderer>().material = relaxMaterial;
    }

    // Update is called once per frame
    void Update()
    {
        if (gForceDevice != null && gForceDevice.GetDevice() != null)
        {
            // Tick gForce device. Each device registered to GForceListener should be ticked.
            gForceDevice.TickGForce();

            if (firmwareVer == null)
            {
                firmwareVer = "";   // Waiting for result

                gForceDevice.GetDevice().getFirmwareVersion((Device device, uint resp, string firmwareVer) => {
                    Debug.Log("Firmware version for device " + device.getName() + ": " + firmwareVer);
                    GestureOnBall.firmwareVer = firmwareVer;
                });
            }

            if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - lastBatteryUpdateTime > 5000/*ms*/)
            {
                lastBatteryUpdateTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                gForceDevice.GetDevice().getBatteryLevel((Device device, uint resp, uint level) => {
                    Debug.Log("Battery level for device " + device.getName() + ": " + level);
                });
            }


            var quat = gForceDevice.GetQuaternion();
            quaternion.Set(quat.X, quat.Y, quat.Z, quat.W);

            transform.rotation = quaternion;

            // Check if the gesture has changed since last update.
            if (gForceDevice.GetGesture() != lastGesture)
            {
                lastGesture = gForceDevice.GetGesture();

                switch (lastGesture)
                {
                    case (uint)Gesture.Relax:
                        // seems Relax is always coming fast, so setup a delay timer,
                        // only when Relax keeps for a while, then render relaxMaterial.
                        //GetComponent<Renderer>().material = relaxMaterial;
                        relaxStart = DateTime.Now;
                        break;

                    case (uint)Gesture.Shoot:
                        GetComponent<Renderer>().material = shootMaterial;
                        break;

                    case (uint)Gesture.Undefined:
                        GetComponent<Renderer>().material = undefinedMaterial;
                        break;

                    default:
                        GetComponent<Renderer>().material = otherMaterial;
                        break;
                }
            }
            else
            {
                if ((uint)Gesture.Relax == lastGesture && GetComponent<Renderer>().material != relaxMaterial)
                {
                    TimeSpan ts = DateTime.Now - relaxStart;

                    if (ts.Milliseconds > RelaxDelay)
                        GetComponent<Renderer>().material = relaxMaterial;
                }
            }
        }
    }


    void OnApplicationQuit()
    {
        Debug.Log("Application ending after " + Time.time + " seconds");

        if (initilized)
        {
            // Terminate the gForce system
            GForceHub.instance.Terminate();
            initilized = false;
        }
    }
}
