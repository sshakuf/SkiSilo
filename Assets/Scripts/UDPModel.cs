using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

public class UDPModel : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    public static event Action<Quaternion, Quaternion> OnDataReceived;

    private void Start()
    {
        Application.runInBackground = true;
        udpClient = new UdpClient(5005);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 5005);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                lock (_mainThreadActions)
                {
                    _mainThreadActions.Enqueue(() => ProcessData(message));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    private void ProcessData(string message)
    {
        string[] values = message.Split(',');
        if (values.Length == 6)
        {
            float leftX = float.Parse(values[0]);
            float leftY = float.Parse(values[1]);
            float leftZ = float.Parse(values[2]);
            float rightX = float.Parse(values[3]);
            float rightY = float.Parse(values[4]);
            float rightZ = float.Parse(values[5]);

            Quaternion leftRotation = Quaternion.Euler(leftX, leftY, leftZ);
            Quaternion rightRotation = Quaternion.Euler(rightX, rightY, rightZ);

            OnDataReceived?.Invoke(leftRotation, rightRotation);
        }
    }

    private void Update()
    {
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    private void OnApplicationQuit()
    {
        isRunning = false;
        receiveThread.Abort();
        udpClient.Close();
    }
}