using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;

public class UDP_GyroSend : MonoBehaviour {

	private static int localPort;
	
	// prefs
	public string IP;  // define in init
	public int port;  // define in init
    public TMP_InputField ipAddressInputField;
    public TextMeshProUGUI statusUi;
    public TextMeshProUGUI ipAddressUi;

    IPEndPoint remoteEndPoint;
	UdpClient client;
    UdpClient receiver;

    private int remotePort = 19777;
	private float repeatTime = 2.0f;

    private bool hasInitialized = false;
    private bool gyroscopeActive = true;

    private Vector2 touchPos = Vector2.zero;

    void Awake()
    {
        ipAddressInputField.text = IP;
        ipAddressUi.text = LocalIPAddress();
        StartReceivingIP();
    }

    void SetStatus(string text)
    {
        statusUi.text = text;
    }

    public void EnableGyroscope()
    {
        gyroscopeActive = !gyroscopeActive;

        Camera.main.gameObject.GetComponent<IOSGyro_Update>().enabled = gyroscopeActive;
    }

	// start from unity3d
	public void DoSendUpdates()
	{
        SetStatus("Connecting...");

        if (hasInitialized)
        {
            //Reconnect to new IP Addr
            hasInitialized = false;
            CancelInvoke("SendUpdates");
                
            if (client != null)
                client.Close();

        }

        SetupClient();
        hasInitialized = true;
	}

	// init
	public void SetupClient()
	{
        Debug.Log("UDPSend.init()");
        InvokeRepeating("SendUpdates", 0.0f, repeatTime);

        // ----------------------------
        // Send
        // ----------------------------
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddressInputField.text), port);
        client = new UdpClient();

        // status
        Debug.Log("Sending to " + IP + " : " + port);
        Debug.Log("Testing: nc -lu " + IP + " : " + port);
    }

    public void StartReceivingIP()
    {
        try
        {
            if (receiver == null)
            {
                Debug.Log("Starting Receiver");
                receiver = new UdpClient(remotePort);
                receiver.BeginReceive(new AsyncCallback(ReceiveData), null);
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }
    private void ReceiveData(IAsyncResult result)
    {
        Debug.Log("Receive data: ");
        IPEndPoint receiveIPGroup = new IPEndPoint(IPAddress.Any, remotePort);
        byte[] received;
        if (receiver != null)
        {
            Debug.Log("Got Data");
            received = receiver.EndReceive(result, ref receiveIPGroup);
        }
        else
        {
            Debug.Log("No Data");
            return;
        }
        receiver.BeginReceive(new AsyncCallback(ReceiveData), null);
        string receivedString = Encoding.ASCII.GetString(received);
        Debug.Log("Recevied String: " + receivedString);
    }

    // sendData
    private void SendString(string message)
	{
        if (!hasInitialized)
            return;

		try
		{
			byte[] data = Encoding.UTF8.GetBytes(message);
            Debug.Log("Sending: " + message);
			client.Send(data, data.Length, remoteEndPoint);
		}
		catch (Exception err)
		{
			Debug.Log(err.ToString());
		}
	}

	private void SendUpdates(){
		/** Send the latest accelerometer info to the client, the client will then use that for lerping camera position */
		Vector3 cameraPos = Camera.main.transform.position;
		Vector3 cameraRot = Camera.main.transform.rotation.eulerAngles;
		SendString("camR:"+cameraRot.ToString());
	}

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
    }

    public string LocalIPAddress()
    {
        return Network.player.ipAddress;
    }

}
