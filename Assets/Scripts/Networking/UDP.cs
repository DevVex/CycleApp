using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;

[Serializable]
public class ClientTransfer
{
    public float speed;
    public float distance;
    public float cadence;
    public string heartRate;
    public string tilt;
}


public class UDP : MonoBehaviour
{
    // receiving Thread
    Thread receiveThread;

    // udpclient object
    UdpClient receiver;
    UdpClient sender;

    // infos
    private DateTime lastReceivedUDPPacket;
    
    public int sendPort;
    public int receivePort;

    IPEndPoint remoteEndPoint;

    public TextMeshProUGUI ipAddressUi;
    public TextMeshProUGUI logUi;


    private string tilt = "";
    private float speed = 0f;
    private float cadence = 0f;
    private float distance = 0f;
    private string heartRate = "";
    private string log = "";

    private float updateSpeed = 1 / 60f;
    private float disconnectTimeCheck = 15f;

    private bool appFound = false;
    private string appIp;


    // start from unity3d
    public void Start()
    {

        init();
        ipAddressUi.text = LocalIPAddress();
        Camera.main.gameObject.GetComponent<IOSGyro_Update>().enabled = true;

    }

    public void Update()
    {
        logUi.text = log;


        if(appIp != null && !appFound)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(appIp), sendPort);
            sender = new UdpClient();
            appFound = true;
            InvokeRepeating("SendUpdates", 0.0f, updateSpeed);
        }

        //Disconnected from companion, start searching
        if (appFound && lastReceivedUDPPacket != null)
        {
            double timeDifference = (DateTime.Now - lastReceivedUDPPacket).TotalSeconds;
            if (timeDifference > disconnectTimeCheck)
            {
                log += "\n Disconnected from app";
                appFound = false;
                appIp = null;
                remoteEndPoint = null;
            }
        }

    }

    // init
    private void init()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // receive thread
    private void ReceiveData()
    {
        receiver = new UdpClient(receivePort);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = receiver.Receive(ref anyIP);

                string text = Encoding.UTF8.GetString(data);

                Debug.Log("Text: " + text);

                log += "\n Recevied: " + text;

                if(appIp == null)
                {
                    log += "\n Found";
                    appIp = anyIP.Address.ToString();
                }

                lastReceivedUDPPacket = DateTime.Now;

            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    private void SendUpdates()
    {
        ClientTransfer clientTransfer = new ClientTransfer();

        Vector3 cameraRot = Camera.main.transform.rotation.eulerAngles;
        clientTransfer.tilt = cameraRot.ToString();
        clientTransfer.speed = Bluetooth.speed;
        clientTransfer.cadence = Bluetooth.cadence;
        clientTransfer.distance = Bluetooth.distance;
        clientTransfer.heartRate = Bluetooth.heartRate;


        Send(JsonUtility.ToJson(clientTransfer));
    }

    private void Send(string message)
    {
        try
        {
            if (remoteEndPoint != null && appFound)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                sender.Send(data, data.Length, remoteEndPoint);
                Debug.Log("Sending: " + message);
            }
        }
        catch (Exception err)
        {
            Debug.Log(err.ToString());
        }
    }

    private void KillThreads()
    {
        if (receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }

        if (receiveThread != null)
            receiveThread.Abort();

        if (sender != null)
            sender.Close();

        if (receiver != null)
            receiver.Close();

    }

    void OnApplicationQuit()
    {
        KillThreads();

    }

    private void OnDestroy()
    {
        KillThreads();
    }

    public string LocalIPAddress()
    {
        return IPManager.GetIP(ADDRESSFAM.IPv4);
    }


}
