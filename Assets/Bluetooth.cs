using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

enum States
{
    None,
    Scan,
    Connect,
    Subscribe,
    Unsubscribe,
    Disconnect,
}

public enum BluetoothDeviceType
{
    heart,
    speed,
    cadence
}

public class Device
{
    public string address;
    public string name;
    public BluetoothDeviceType type;
    public string serviceUUID;
    public string characteristicUUID;
    public bool connected = false;
    public bool subscribed = false;


    public Device(string address, string name)
    {
        this.address = address;
        this.name = name;
    }
}

public class SpeedCadence
{
    public UInt32 cumulativeWheel = 0;
    public UInt16 wheelTime = 0;
    public UInt16 cumulativeCrank = 0;
    public UInt16 crankTime = 0;
}

public class Bluetooth : MonoBehaviour
{

    private float _timeout = 0f;
    private States _state = States.None;
    bool _connected = false;

    
    public Text status;
    public Text log;
    public GameObject devicesUiContainer;
    public GameObject dataUiContainer;
    public GameObject deviceUi;

    private string[] services = new string[] {"1816",  "180D"};
    private const string HEART_RATE_CHARACTERISTIC = "2A37";
    private const string SPEED_CHARACTERISTIC = "2A5B";
    private const float TIMESCALE = 1024.0f;
    private const float TIRESIZE = 2170.0f; // In millimiters. 700x30
    //heart, cadence, speed
    private List<string> characteristics = new List<string>(new string[] {HEART_RATE_CHARACTERISTIC, SPEED_CHARACTERISTIC});
    private Dictionary<string, Device> devices = new Dictionary<string, Device>();
    private Dictionary<string, GameObject> devicesUi = new Dictionary<string, GameObject>();
    private SpeedCadence previousSpeedCadence = new SpeedCadence();

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    // Use this for initialization
    void Start()
    {
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {

            SetState(States.None, 0.1f);

        }, (error) =>
        {

            BluetoothLEHardwareInterface.Log("Error during initialize: " + error);
            PrintLog(error);

            if (error.Contains("Bluetooth LE Not Enabled"))
                BluetoothLEHardwareInterface.BluetoothEnable(true);
        });
    }

    void UpdateDeviceUi()
    {
        foreach (KeyValuePair<string, Device> device in devices)
        {
            string address = device.Value.address;
            //create if doesn't exist
            if (!devicesUi.ContainsKey(address)){
                GameObject newDevice = Instantiate(deviceUi);
                newDevice.transform.SetParent(devicesUiContainer.transform);
                devicesUi.Add(address, newDevice);
            }

            GameObject matchingDeviceUi = devicesUi[address];
            matchingDeviceUi.transform.GetChild(0).GetComponent<Text>().text = device.Value.name;
            matchingDeviceUi.transform.GetChild(1).GetComponent<Text>().text = device.Value.address;
        }
    }

    void RemoveDevice(String deviceAddress)
    {
        devices.Remove(deviceAddress);
        Destroy(devicesUi[deviceAddress]);
        devicesUi.Remove(deviceAddress);
    }

    void PrintLog(string logEntry)
    {
        log.text = log.text + "\n" + logEntry;
    }

    public void Scan()
    {
        SetState(States.Scan, 0.1f);
    }

    public void Complete()
    {
        SetState(States.Connect, 0.1f);
    }

    public void Unsubscribe()
    {
        SetState(States.Unsubscribe, 0.1f);
    }

    void OnApplicationQuit()
    {
        BluetoothLEHardwareInterface.DeInitialize(() =>
        { 
            SetState(States.None, 0.1f);
        });
    }

    string ShortUUID(string uuid)
    {
        if (uuid.Length != 4)
        {
            return uuid.Substring(4, 4);
        }
        else
        {
            return uuid;
        }
    }

    private void SetHeartRate(string value)
    {
        dataUiContainer.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = value;
    }

    private void SetSpeed(SpeedCadence value)
    {
        float speed = 0.0f;
        float wheelTimeDifference = 0.0f;

        if (value.wheelTime >= previousSpeedCadence.wheelTime)
        {
            wheelTimeDifference = (value.wheelTime - previousSpeedCadence.wheelTime) / TIMESCALE;
        }
        else
        {
            // passed the maximum value
            wheelTimeDifference = (UInt16.MaxValue - previousSpeedCadence.wheelTime + value.wheelTime) / TIMESCALE;
        }

        float cumulativeWheelDifference = value.cumulativeWheel - previousSpeedCadence.cumulativeWheel;
        if (value.cumulativeWheel >= previousSpeedCadence.cumulativeWheel)
        {
            cumulativeWheelDifference = value.cumulativeWheel - previousSpeedCadence.cumulativeWheel;
        }
        else
        {
            // passed the maximum value
            cumulativeWheelDifference = UInt32.MaxValue - previousSpeedCadence.cumulativeWheel + value.cumulativeWheel;
        }

        float distance = (cumulativeWheelDifference * TIRESIZE) / 1000.0f; // distance in meters
        if  (distance != 0 && wheelTimeDifference > 0) {
            speed = (wheelTimeDifference == 0) ? 0.0f : distance / wheelTimeDifference; // m/s
        }
        dataUiContainer.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = speed.ToString();
        dataUiContainer.transform.GetChild(3).GetChild(1).GetComponent<Text>().text = distance.ToString();

        previousSpeedCadence.cumulativeWheel = value.cumulativeWheel;
        previousSpeedCadence.wheelTime = value.wheelTime;
    }

    private void SetCadence(SpeedCadence value)
    {

        float crankTimeDifference = 0.0f;
        if (value.wheelTime >= previousSpeedCadence.wheelTime)
        {
            crankTimeDifference = (value.crankTime - previousSpeedCadence.crankTime) / TIMESCALE;
        }
        else
        {
            // passed the maximum value
            crankTimeDifference = (UInt16.MaxValue - previousSpeedCadence.crankTime + value.crankTime) / TIMESCALE;
        }
        
        float cumulativeCrankDifference = 0.0f;
        if (value.cumulativeCrank >= previousSpeedCadence.cumulativeCrank)
        {
            cumulativeCrankDifference = value.cumulativeCrank - previousSpeedCadence.cumulativeCrank;
        }
        else
        {
            // passed the maximum value
            cumulativeCrankDifference = UInt16.MaxValue - previousSpeedCadence.cumulativeWheel + value.cumulativeWheel;
        }

        float cadence = (crankTimeDifference == 0) ? 0.0f : 60.0f * cumulativeCrankDifference / crankTimeDifference; // RPM

        dataUiContainer.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = cadence.ToString();

        previousSpeedCadence.cumulativeCrank = value.cumulativeCrank;
        previousSpeedCadence.crankTime = value.crankTime;
    }

    //string FullUUID(string uuid)
    //{
    //    return "0000" + uuid + "-0000-1000-8000-00805f9b34fb";
    //}

    //Bluetooth LE heart rate
    //https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.heart_rate_measurement.xml
    //2A37
    string ReadHeartRate(byte[] bytes, Device connectedDevice)
    {
        string data = "";
        bool unit8 = true;

        if (bytes != null)
        {
            string bitString = "";

            byte flags = bytes[0];
            bitString += Convert.ToString(flags, 2).PadLeft(8, '0') + " ";
            if (bitString[7] == '1')
            {
                unit8 = false;
            }

            if (unit8)
            {
                data = bytes[1].ToString();
            }
            else
            {
                data = BitConverter.ToUInt16(bytes, 1).ToString();
            }
        }
        return data;
    }

    //Bluetooth LE CSC Measurement
    //https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.csc_measurement.xml
    //2A5B
    //Others 2A5C, 2A5D, 2A55
    SpeedCadence ReadSpeedCadence(byte[] bytes, Device connectedDevice)
    {
        bool wheelRevolution = false;
        bool crankRevolution = false;
        SpeedCadence speedCadence = new SpeedCadence();

        //foreach (byte b in bytes)
        //{
        //    string bitString = "";
        //    bitString += Convert.ToString(b, 2).PadLeft(8, '0') + " ";
        //    PrintLog(connectedDevice.name + ": " + bitString);
        //}

        if (bytes != null)
        {
            string bitString = "";
            byte flags = bytes[0];
            bitString += Convert.ToString(flags, 2).PadLeft(8, '0') + " ";

            if (bitString[7] == '1') wheelRevolution = true;
            if (bitString[6] == '1') crankRevolution = true;

            int offset = 1;
            if (wheelRevolution)
            {
                speedCadence.cumulativeWheel = BitConverter.ToUInt32(bytes, offset);
                offset += sizeof(UInt32);
                speedCadence.wheelTime = BitConverter.ToUInt16(bytes, offset);
                offset += sizeof(UInt16);
            }
            if (crankRevolution)
            {
                speedCadence.cumulativeCrank = BitConverter.ToUInt16(bytes, offset);
                offset += sizeof(UInt16);
                speedCadence.crankTime = BitConverter.ToUInt16(bytes, offset);
            }
        }
        return speedCadence;
    }

        // Update is called once per frame
        void Update()
    {
        UpdateDeviceUi();
        status.text = _state.ToString();
        if (_timeout > 0f)
        {
            _timeout -= Time.deltaTime;

            if (_timeout <= 0f)
            {
                _timeout = 0f;
                switch (_state)
                {
                    case States.None:
                        break;

                    case States.Scan:
                        log.text = "";
                        PrintLog("Scanning ");
                        BluetoothLEHardwareInterface.StopScan();
                        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, null, (address, name, rssi, bytes) =>
                        {
                            PrintLog("Scanning Call");
                            // if your device does not advertise the rssi and manufacturer specific data
                            // then you must use this callback because the next callback only gets called
                            // if you have manufacturer specific data
                            if (name != null && name != "No Name")
                            {
                                if (!devices.ContainsKey(address))
                                {
                                    Device newDevice = new Device(address, name);
                                    devices.Add(address, newDevice);
                                    PrintLog("Scanned:" + newDevice.name + " - " + newDevice.address);
                                }
                            }

                        }, true);
                        break;
                    case States.Connect:

                        if(devices.Count <= 0)
                        {
                            SetState(States.Scan, 0.1f);
                            break;
                        }
                        PrintLog("Connecting");
                        BluetoothLEHardwareInterface.StopScan();
                        // note that the first parameter is the address, not the name. I have not fixed this because
                        // of backwards compatiblity.
                        // also note that I am note using the first 2 callbacks. If you are not looking for specific characteristics you can use one of
                        // the first 2, but keep in mind that the device will enumerate everything and so you will want to have a timeout
                        // large enough that it will be finished enumerating before you try to subscribe or do any other operations.

                        int connected = 0;
                        foreach (KeyValuePair<string, Device> device in devices)
                        {
                            if (!device.Value.connected)
                            {
                                //PrintLog("Trying to connect to: " + device.Value.address);
                                BluetoothLEHardwareInterface.ConnectToPeripheral(device.Value.address, null, null, (address, serviceUUID, characteristicUUID) =>
                                {
                                    Device connectedDevice = devices[address];
                                    string characteristic = ShortUUID(characteristicUUID).ToUpper();

                                    if (characteristics.Contains(characteristic)){
                                        if (characteristic.Equals(characteristics[0]))
                                        {
                                            PrintLog("Connected Heart:" + serviceUUID);
                                            connectedDevice.type = BluetoothDeviceType.heart;
                                        }
                                        else
                                        {
                                            PrintLog("Connected Speed:" + serviceUUID);
                                            connectedDevice.type = BluetoothDeviceType.speed;
                                        }
                                        connectedDevice.serviceUUID = serviceUUID;
                                        connectedDevice.characteristicUUID = characteristicUUID;
                                        connectedDevice.connected = true;
                                    }
                                }, (address) =>
                                {
                                    RemoveDevice(address);
                                });
                            }
                            else
                            {
                                connected++;
                            }
                        }

                        if(connected == devices.Count)
                        {
                            _connected = true;
                            SetState(States.Subscribe, 2.0f);
                        }
                        else
                        {
                            SetState(States.Connect, 2.0f);
                        }
                        break;

                    case States.Subscribe:
                        PrintLog("Subscribing");
                        int subscribed = 0;
                        foreach (KeyValuePair<string, Device> device in devices)
                        {
                            if (!device.Value.subscribed && device.Value.connected)
                            {
                                BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(device.Value.address, device.Value.serviceUUID, device.Value.characteristicUUID, null, (address, characteristicUUID, bytes) =>
                                {
                                    Device connectedDevice = devices[address];
                                    string characteristic = ShortUUID(characteristicUUID).ToUpper();

                                    if (!connectedDevice.subscribed)
                                    {
                                        connectedDevice.subscribed = true;
                                        PrintLog("Subscribed: " + connectedDevice.name);
                                    }

                                    if(characteristic.Equals(HEART_RATE_CHARACTERISTIC))
                                    {
                                        String heartRate = ReadHeartRate(bytes, connectedDevice);
                                        SetHeartRate(heartRate);

                                    }else if (characteristic.Equals(SPEED_CHARACTERISTIC))
                                    {
                                        SpeedCadence speedCadence = ReadSpeedCadence(bytes, connectedDevice);
                                        if (speedCadence.cumulativeWheel != 0)
                                        {
                                            SetSpeed(speedCadence);
                                        }

                                        if (speedCadence.cumulativeCrank != 0)
                                        {
                                            SetCadence(speedCadence);
                                        }
                                    }
                                    
                                });
                            }
                            else
                            {
                                subscribed++; 
                            }
                        }
                        if (subscribed == devices.Count)
                        {
                            SetState(States.None, 0.1f);
                        }
                        else
                        {
                            SetState(States.Subscribe, 1.0f);
                        }
                        break;

                    case States.Unsubscribe:
                        PrintLog("UnSubscribing");
                        foreach (KeyValuePair<string, Device> device in devices)
                        {
                            PrintLog(device.Value.name + ": UnSubscribing");
                            BluetoothLEHardwareInterface.UnSubscribeCharacteristic(device.Value.address, device.Value.serviceUUID, device.Value.characteristicUUID, null);
                        }
                        SetState(States.Disconnect, 4.0f);
                        break;

                    case States.Disconnect:
                        if (_connected)
                        {
                            foreach (KeyValuePair<string, Device> device in devices)
                            {
                                BluetoothLEHardwareInterface.DisconnectPeripheral(device.Value.address, (address) =>
                                {
                                    Device connectedDevice = devices[address];
                                    PrintLog(connectedDevice.name + ": Disconnected");
                                    RemoveDevice(address);
                                });
                            }
                            _connected = false;
                            SetState(States.None, 0.1f);
                        }
                        devices.Clear();
                        PrintLog("Disconnected");
                        break;
                }
            }
        }
    }
}