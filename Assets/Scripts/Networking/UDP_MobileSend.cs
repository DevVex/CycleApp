
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDP_MobileSend : MonoBehaviour
{
	private static int localPort;
	
	// prefs
	private string IP;  // define in init
	public int port;  // define in init

    public Text IPAddressLbl;
    public Text PortLbl;
    public Text playerNumLbl;
    public Text statusLbl;


    public float speed = 0.1F;
    public Vector2 touchPos = Vector2.zero;


	// "connection" things
	IPEndPoint remoteEndPoint;
	UdpClient client;


	// gui
	string strMessage="";
	//string newIPAddr="127.0.0.1";
	//string newPortNum="2067";
	
    /*
	// call it from shell (as program)
	private static void Main()
	{
        UDP_MobileSend sendObj = new UDP_MobileSend();
		sendObj.init();
		
		// testing via console
		// sendObj.inputFromConsole();
		
		// as server sending endless
		sendObj.sendEndless(" endless infos \n");
		
	}*/

	// start from unity3d
	public void DoConnect()
	{
		init();
	}

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
            
            touchPos = touchDeltaPosition;
            statusLbl.text = "touch pos: " + touchPos;
            sendString("tPos:"+touchPos);
           // transform.Translate(-touchDeltaPosition.x * speed, -touchDeltaPosition.y * speed, 0);
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            touchPos = Input.mousePosition;
            // if (Physics.Raycast(ray))
            // Instantiate(particle, transform.position, transform.rotation) as GameObject;
            statusLbl.text = "touch pos: " + touchPos;

            sendString("tPos:" + touchPos);
            //strMessage=GUI.TextField(new Rect(40,420,140,20),strMessage);
        }
    }
	
    /*
	// OnGUI
	void OnGUI()
	{
		//IP Address Input
		GUI.Label(new Rect(50, 220, 100, 20), "IP Address");
		newIPAddr=GUI.TextField(new Rect(40,250,140,20),newIPAddr);

		//Porn Number Input
		GUI.Label(new Rect(50, 280, 100, 20), "Port Num");
		newPortNum=GUI.TextField(new Rect(40,300,140,20),newPortNum);

		//Set the IP and port when button pressed
		if (GUI.Button(new Rect(190,350,40,20),"SetIP"))
		{
			initIP(newIPAddr,newPortNum);
		}      
	

		Rect rectObj=new Rect(40,380,200,400);
		GUIStyle style = new GUIStyle();
		style.alignment = TextAnchor.UpperLeft;
		GUI.Box(rectObj,"# UDPSend-Data\n"+IP+ " "+port+" #\n"
		        + "shell> nc -lu "+IP+ "   "+port+" \n"
		        ,style);
		
		// ------------------------
		// send it
		// ------------------------
		strMessage=GUI.TextField(new Rect(40,420,140,20),strMessage);
		if (GUI.Button(new Rect(190,420,40,20),"send"))
		{
			sendString(strMessage+"\n");
		}      
	}*/

	// init
	public void initIP(string newIP, string newPort)
	{
		int portVal = 0;
		// Endpunkt definieren, von dem die Nachrichten gesendet werden.
		print("UDPSend.init()");
		
		// define
		IP=newIP;

		int.TryParse(newPort,out portVal);
		port=portVal;
		
		// ----------------------------
		// Senden
		// ----------------------------
		remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
		client = new UdpClient();
		
		// status
		print("Sending to "+IP+" : "+port);
		print("Testing: nc -lu "+IP+" : "+port);
		
	}



	
	// init
	public void init()
	{
		print("UDPSend.init()");
		

		// define
		IP=IPAddressLbl.text;
        port = int.Parse(PortLbl.text);
		
		// ----------------------------
		// Senden
		// ----------------------------
		remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
		client = new UdpClient();
		
		// status
		print("Sending to "+IP+" : "+port);
		print("Testing: nc -lu "+IP+" : "+port);
		
	}
	
	// inputFromConsole
	private void inputFromConsole()
	{
		try
		{
			string text;
			do
			{
				text = Console.ReadLine();
				
				// Den Text zum Remote-Client senden.
				if (text != "")
				{
					
					// Daten mit der UTF8-Kodierung in das Binärformat kodieren.
					byte[] data = Encoding.UTF8.GetBytes(text);
					
					// Den Text zum Remote-Client senden.
					client.Send(data, data.Length, remoteEndPoint);
				}
			} while (text != "");
		}
		catch (Exception err)
		{
			print(err.ToString());
		}
		
	}
	
	// sendData
	private void sendString(string message)
	{
		try
		{
			//if (message != "")
			//{
			
			// Daten mit der UTF8-Kodierung in das Binärformat kodieren.
			byte[] data = Encoding.UTF8.GetBytes(message);
			
			// Den message zum Remote-Client senden.
			client.Send(data, data.Length, remoteEndPoint);
			//}
		}
		catch (Exception err)
		{
			print(err.ToString());
		}
	}
	
	
	// endless test
	private void sendEndless(string testStr)
	{
		do
		{
			sendString(testStr);
			
			
		}
		while(true);
		
	}

	/*
	void OnApplicationQuit () {
		
		if ( receiveThread!= null)
			receiveThread.Abort();
		
		client.Close(); 
	}
	*/

	
	
}