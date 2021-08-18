
using gf;
using GForce;
using System.Collections.Generic;

class SimpleListener : UIListener
{
    private Dictionary<string/*address*/, Device> _devicesFound = new Dictionary<string/*address*/, Device>();


    public SimpleListener()
    {

    }

    ~SimpleListener()
    {

    }

    /// 
    /// <param name="device"></param>
    public override void onDeviceConnected(Device device)
    {

    }

    /// 
    /// <param name="device"></param>
    public override void onDeviceDiscard(Device device)
    {
        GForceLogger.Log("[SimpleListener] onDeviceDiscard");

        lock (_devicesFound)
        {
            _devicesFound.Clear();

            Hub.Instance.startScan();
        }
    }

    /// 
    /// <param name="device"></param>
    /// <param name="reason"></param>
    public override void onDeviceDisconnected(Device device, int reason)
    {
        GForceLogger.Log("[SimpleListener] onDeviceDisconnected");

        lock (_devicesFound)
        {
            _devicesFound.Clear();

            Hub.Instance.startScan();
        }
    }

    /// 
    /// <param name="device"></param>
    public override void onDeviceFound(Device device)
    {
        GForceLogger.Log("[SimpleListener] onDeviceFound, device: " + device.getAddress() + ", RSSI: " + device.getRssi());

        lock (_devicesFound)
        {
            if (!_devicesFound.ContainsKey(device.getAddress()))
            {
                _devicesFound.Add(device.getAddress(), device);
            }
        }
    }

    public override void onScanFinished()
    {
        GForceLogger.Log("[SimpleListener] onScanFinished, device found: " + _devicesFound.Count);

        // Connect to device with largest RSSI

        lock (_devicesFound)
        {
            if (_devicesFound.Count == 0)
            {
                Hub.Instance.startScan();
            }
            else
            {
                uint largestRSSI = 0;
                Device deviceToConnect = null;

                foreach (var dev in _devicesFound.Values)
                {
                    if (dev.getRssi() > largestRSSI)
                    {
                        largestRSSI = dev.getRssi();
                        deviceToConnect = dev;
                    }
                }

                if (deviceToConnect != null)
                {
                    GForceLogger.Log("Connecting to device " + deviceToConnect.getAddress());
                    deviceToConnect.connect();
                }
            }
        }
    }

    /// 
    /// <param name="state"></param>
    public override void onStateChanged(Hub.HubState state)
    {

    }


    /// 
    /// <param name="state"></param>
    public override void onDeviceStatusChanged(Device device, Device.Status status)
    {

    }
}
