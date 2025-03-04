// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Net;
// using System.Net.NetworkInformation;
// using System.Net.Sockets;
// using UnityEngine;

// /// <summary>
// /// Add this script to a GameObject in your scene to handle server setup and monitoring
// /// </summary>
// public class SensorDataServerSetup : MonoBehaviour
// {
//     [SerializeField] private string serverAddress = "http://0.0.0.0";
//     [SerializeField] private int serverPort = 8080;
//     [SerializeField] private string endpoint = "/";
//     [SerializeField] private bool autoUseLocalIP = true;

//     [Header("Server Status")]
//     [SerializeField] private bool showGUI = true;
    
//     private SensorDataEndpoint sensorEndpoint;
//     private string serverStatus = "Stopped";
//     private string lastReceivedData = "None";
//     private int totalEventsReceived = 0;
//     private int eventsInQueue = 0;
//     private string localIPAddress = "Unknown";
    
//     private void Awake()
//     {
//         // Get the local IP address
//         localIPAddress = GetLocalIPAddress();
        
//         // Create the sensor endpoint if it doesn't exist
//         if (sensorEndpoint == null)
//         {
//             GameObject endpointObj = new GameObject("SensorDataEndpoint");
//             endpointObj.transform.SetParent(transform);
//             sensorEndpoint = endpointObj.AddComponent<SensorDataEndpoint>();
            
//             // If auto use local IP is enabled, update the server address
//             if (autoUseLocalIP && !string.IsNullOrEmpty(localIPAddress))
//             {
//                 serverAddress = "http://" + localIPAddress;
                
//                 // Configure the endpoint with the local IP
//                 SensorDataEndpoint.ServerConfig config = new SensorDataEndpoint.ServerConfig
//                 {
//                     ServerUrl = serverAddress,
//                     ServerPort = serverPort,
//                     EndpointPath = endpoint
//                 };
                
//                 sensorEndpoint.SetServerConfig(config);
//             }
            
//             // Subscribe to the OnEventsReceived event
//             sensorEndpoint.OnEventsReceived += OnEventsReceived;
//         }
        
//         // Don't destroy this GameObject when loading new scenes
//         DontDestroyOnLoad(gameObject);
        
//         // Set the server status
//         serverStatus = "Running";
//     }
    
//     private void OnDestroy()
//     {
//         // Unsubscribe from the event
//         if (sensorEndpoint != null)
//         {
//             sensorEndpoint.OnEventsReceived -= OnEventsReceived;
//         }
//     }
    
//     private void OnGUI()
//     {
//         if (!showGUI) return;
        
//         // Define GUI styles for the background and text
//         GUIStyle backgroundStyle = new GUIStyle();
//         backgroundStyle.normal.background = MakeBackgroundTexture(350, 300, Color.black);
        
//         GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
//         labelStyle.fontSize = 18;
//         labelStyle.normal.textColor = Color.white;
//         labelStyle.fontStyle = FontStyle.Bold;
//         labelStyle.margin = new RectOffset(5, 5, 5, 5);
        
//         GUIStyle headerStyle = new GUIStyle(labelStyle);
//         headerStyle.fontSize = 22;
//         headerStyle.alignment = TextAnchor.MiddleCenter;
        
//         GUIStyle ipStyle = new GUIStyle(labelStyle);
//         ipStyle.normal.textColor = Color.green;
        
//         // Draw the background and labels
//         GUI.Box(new Rect(10, 10, 350, 300), "", backgroundStyle);
//         GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        
//         GUILayout.Space(10);
//         GUILayout.Label("Sensor Data Server Status", headerStyle);
//         GUILayout.Space(15);
        
//         GUILayout.Label($"Status: {serverStatus}", labelStyle);
//         GUILayout.Label("Local IP:", labelStyle);
//         GUILayout.Label($"{localIPAddress}", ipStyle);
//         GUILayout.Label($"Full URL: {serverAddress}:{serverPort}{endpoint}", labelStyle);
//         GUILayout.Space(10);
//         GUILayout.Label($"Total Events: {totalEventsReceived}", labelStyle);
//         GUILayout.Label($"Queue Size: {eventsInQueue}", labelStyle);
//         GUILayout.Label($"Last Data: {lastReceivedData}", labelStyle);
        
//         GUILayout.EndArea();
//     }
    
//     /// <summary>
//     /// Callback method to be called from SensorDataEndpoint when events are received
//     /// </summary>
//     public void OnEventsReceived(int count, string deviceId, int queueSize, string lastEventData)
//     {
//         totalEventsReceived += count;
//         eventsInQueue = queueSize;
//         lastReceivedData = lastEventData;
//         serverStatus = "Running";
//     }

//     /// <summary>
//     /// Start the sensor data server
//     /// </summary>
//     public void StartServer()
//     {
//         serverStatus = "Starting...";
//         // Server is started automatically by the SensorDataEndpoint component
//     }

//     /// <summary>
//     /// Stop the sensor data server
//     /// </summary>
//     public void StopServer()
//     {
//         serverStatus = "Stopping...";
//         // Server is stopped automatically when the application quits
//     }
    
//     // Helper method to get the local IP address
//     private string GetLocalIPAddress()
//     {
//         string localIP = "0.0.0.0";
//         try
//         {
//             // First, try to get the IP address from network interfaces
//             NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            
//             foreach (NetworkInterface networkInterface in networkInterfaces)
//             {
//                 // Skip loopback, non-operational, and wireless interfaces (initially)
//                 if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
//                     networkInterface.OperationalStatus != OperationalStatus.Up)
//                 {
//                     continue;
//                 }
                
//                 // Get IP properties
//                 IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                
//                 // Look for IPv4 addresses
//                 foreach (UnicastIPAddressInformation ipInfo in ipProperties.UnicastAddresses)
//                 {
//                     if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork)
//                     {
//                         // This is an IPv4 address, check if it's not a loopback
//                         if (!IPAddress.IsLoopback(ipInfo.Address))
//                         {
//                             string ip = ipInfo.Address.ToString();
                            
//                             // Prefer addresses that start with 192.168 or 10. as these are commonly used for local networks
//                             if (ip.StartsWith("192.168.") || ip.StartsWith("10."))
//                             {
//                                 return ip;
//                             }
                            
//                             // Keep this as a potential candidate
//                             localIP = ip;
//                         }
//                     }
//                 }
//             }
            
//             // If no preferred IP was found, try a different approach
//             if (localIP == "0.0.0.0")
//             {
//                 // Try another method using DNS
//                 string hostName = Dns.GetHostName();
//                 IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
                
//                 foreach (IPAddress address in hostEntry.AddressList)
//                 {
//                     if (address.AddressFamily == AddressFamily.InterNetwork)
//                     {
//                         localIP = address.ToString();
//                         break;
//                     }
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"Error finding local IP address: {ex.Message}");
//         }
        
//         return localIP;
//     }
    
//     // Helper method to create a solid color texture for GUI background
//     private Texture2D MakeBackgroundTexture(int width, int height, Color color)
//     {
//         Color[] pixels = new Color[width * height];
//         for (int i = 0; i < pixels.Length; i++)
//         {
//             pixels[i] = color;
//         }
        
//         Texture2D backgroundTexture = new Texture2D(width, height);
//         backgroundTexture.SetPixels(pixels);
//         backgroundTexture.Apply();
        
//         return backgroundTexture;
//     }
// }