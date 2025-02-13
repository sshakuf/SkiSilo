using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class LegController : MonoBehaviour
{
    public Transform leftLeg;
    public Transform rightLeg;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    private Quaternion leftLegRotation = Quaternion.identity;
    private Quaternion rightLegRotation = Quaternion.identity;
    private bool hasNewData = false;

    void Start()
    {
        udpClient = new UdpClient(5005); // Listening on port 5005
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 5005);
        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                ProcessData(message);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    void ProcessData(string message)
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

            // Store new rotations
            leftLegRotation = Quaternion.Euler(leftX, leftY, leftZ);
            rightLegRotation = Quaternion.Euler(rightX, rightY, rightZ);
            hasNewData = true;
        }
    }

    void Update()
    {
        if (hasNewData)
        {
            leftLeg.rotation = leftLegRotation;
            rightLeg.rotation = rightLegRotation;
            hasNewData = false;
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        receiveThread.Abort();
        udpClient.Close();
    }
}