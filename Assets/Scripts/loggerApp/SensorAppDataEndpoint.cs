// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Net;
// using System.Text;
// using System.Threading;
// using UnityEngine;

// public class SensorDataEndpoint : MonoBehaviour
// {
//     [System.Serializable]
//     public class ServerConfig
//     {
//         public string ServerUrl = "http://0.0.0.0";
//         public int ServerPort = 8080;
//         public string EndpointPath = "/";
//     }
    
//     [Header("Server Settings")]
//     [SerializeField] private string serverUrl = "http://0.0.0.0";
//     [SerializeField] private int serverPort = 8080;
//     [SerializeField] private string endpointPath = "/";
    
//     // Thread-safe queue for storing sensor events
//     private readonly Queue<SensorEvent> eventQueue = new Queue<SensorEvent>();
//     private readonly object queueLock = new object();
    
//     // Thread control
//     private Thread processingThread;
//     private bool isRunning = false;
//     private HttpListener httpListener;
    
//     // Events
//     public event Action<int, string, int, string> OnEventsReceived;
//     public static event Action<Quaternion, Vector3, Quaternion, Vector3> OnMotionDataReceived;

//     [System.Serializable]
//     public class SensorValues
//     {
//         public float x;
//         public float y;
//         public float z;
        
//         // Additional fields for orientation
//         public float yaw;
//         public float pitch;
//         public float roll;
//         public float qx;
//         public float qy;
//         public float qz;
//         public float qw;

//         public override string ToString()
//         {
//             if (yaw != 0 || pitch != 0 || roll != 0 || qx != 0 || qy != 0 || qz != 0 || qw != 0)
//             {
//                 return $"yaw: {yaw}, pitch: {pitch}, roll: {roll}, quat: ({qx}, {qy}, {qz}, {qw})";
//             }
            
//             return $"x: {x}, y: {y}, z: {z}";
//         }
        
//         // For manual parsing from Dictionary
//         public static SensorValues FromDictionary(Dictionary<string, object> dict)
//         {
//             SensorValues values = new SensorValues();
            
//             if (dict.ContainsKey("x") && dict["x"] is double xVal)
//                 values.x = (float)xVal;
                
//             if (dict.ContainsKey("y") && dict["y"] is double yVal)
//                 values.y = (float)yVal;
                
//             if (dict.ContainsKey("z") && dict["z"] is double zVal)
//                 values.z = (float)zVal;
                
//             if (dict.ContainsKey("yaw") && dict["yaw"] is double yawVal)
//                 values.yaw = (float)yawVal;
                
//             if (dict.ContainsKey("pitch") && dict["pitch"] is double pitchVal)
//                 values.pitch = (float)pitchVal;
                
//             if (dict.ContainsKey("roll") && dict["roll"] is double rollVal)
//                 values.roll = (float)rollVal;
                
//             if (dict.ContainsKey("qx") && dict["qx"] is double qxVal)
//                 values.qx = (float)qxVal;
                
//             if (dict.ContainsKey("qy") && dict["qy"] is double qyVal)
//                 values.qy = (float)qyVal;
                
//             if (dict.ContainsKey("qz") && dict["qz"] is double qzVal)
//                 values.qz = (float)qzVal;
                
//             if (dict.ContainsKey("qw") && dict["qw"] is double qwVal)
//                 values.qw = (float)qwVal;
                
//             return values;
//         }
//     }
    
//     [System.Serializable]
//     public class SensorEvent
//     {
//         public string name;
//         public long time;
//         public SensorValues values;
        
//         public override string ToString()
//         {
//             return $"Event: {name}, Time: {ConvertUnixTimeToDateTime(time)}, Values: {values}";
//         }
        
//         private DateTime ConvertUnixTimeToDateTime(long unixTime)
//         {
//             // Convert nanoseconds to milliseconds (assuming the timestamp is in nanoseconds)
//             long milliseconds = unixTime / 1000000;
            
//             // Unix epoch
//             DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
//             // Add the milliseconds to the epoch
//             return epoch.AddMilliseconds(milliseconds);
//         }
//     }
    
//     [System.Serializable]
//     public class SensorPayload
//     {
//         public int messageId;
//         public string sessionId;
//         public string deviceId;
//         public List<SensorEvent> payload;
//     }

//     private void Start()
//     {
//         StartServer();
//     }

//     private void OnDestroy()
//     {
//         StopServer();
//     }

//     private void OnApplicationQuit()
//     {
//         StopServer();
//     }
    
//     // Set server configuration method
//     public void SetServerConfig(ServerConfig config)
//     {
//         serverUrl = config.ServerUrl;
//         serverPort = config.ServerPort;
//         endpointPath = config.EndpointPath;
        
//         // If the server is already running, restart it with the new config
//         if (httpListener != null && httpListener.IsListening)
//         {
//             StopServer();
//             StartServer();
//         }
//     }

//     private void StartServer()
//     {
//         // Start HTTP server
//         try
//         {
//             httpListener = new HttpListener();
//             string prefix = $"{serverUrl}:{serverPort}{endpointPath}";
//             httpListener.Prefixes.Add(prefix);
//             httpListener.Start();
            
//             Debug.Log($"Server started at {prefix}");
            
//             // Start processing request asynchronously
//             httpListener.BeginGetContext(OnRequestReceived, httpListener);
            
//             // Start the processing thread
//             isRunning = true;
//             processingThread = new Thread(ProcessEvents);
//             processingThread.IsBackground = true;
//             processingThread.Start();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Failed to start server: {e.Message}");
//         }
//     }

//     private void StopServer()
//     {
//         isRunning = false;
        
//         // Stop the processing thread
//         if (processingThread != null && processingThread.IsAlive)
//         {
//             processingThread.Join(1000); // Wait up to 1 second for the thread to terminate
            
//             if (processingThread.IsAlive)
//             {
//                 processingThread.Abort(); // Force abort if it doesn't terminate gracefully
//             }
            
//             processingThread = null;
//         }
        
//         // Stop the HTTP server
//         if (httpListener != null && httpListener.IsListening)
//         {
//             httpListener.Stop();
//             httpListener.Close();
//             httpListener = null;
            
//             Debug.Log("Server stopped");
//         }
//     }

//     private void OnRequestReceived(IAsyncResult result)
//     {
//         HttpListener listener = (HttpListener)result.AsyncState;
        
//         try
//         {
//             // Get the context
//             HttpListenerContext context = listener.EndGetContext(result);
            
//             // Continue listening for the next request
//             listener.BeginGetContext(OnRequestReceived, listener);
            
//             // Process the current request
//             ProcessRequest(context);
//         }
//         catch (Exception e)
//         {
//             if (isRunning)
//             {
//                 Debug.LogError($"Error handling request: {e.Message}");
//             }
//         }
//     }

//     private void ProcessRequest(HttpListenerContext context)
//     {
//         try
//         {
//             // Check if it's a POST request
//             if (context.Request.HttpMethod != "POST")
//             {
//                 SendResponse(context, 405, "Method Not Allowed");
//                 return;
//             }
            
//             // Read the request body
//             string requestBody;
//             using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
//             {
//                 requestBody = reader.ReadToEnd();
//             }
            
//             // Parse the JSON data using Unity's built-in JSON utilities
//             SensorPayload sensorData = JsonUtility.FromJson<SensorPayload>(requestBody);
            
//             string deviceId = "unknown";
//             string sessionId = "unknown";
//             int eventsAdded = 0;
//             string lastEventInfo = "None";
            
//             // Add sensor events to the queue
//             lock (queueLock)
//             {
//                 if (sensorData != null && sensorData.payload != null)
//                 {
//                     deviceId = sensorData.deviceId;
//                     sessionId = sensorData.sessionId;
//                     eventsAdded = sensorData.payload.Count;
                    
//                     foreach (SensorEvent sensorEvent in sensorData.payload)
//                     {
//                         eventQueue.Enqueue(sensorEvent);
//                         lastEventInfo = sensorEvent.name;
//                     }
                    
//                     Debug.Log($"Received {eventsAdded} events from device {deviceId}, session {sessionId}");
//                 }
//                 else
//                 {
//                     // If parsing failed with JsonUtility, try manual parsing
//                     Debug.LogWarning("Standard JSON parsing failed, attempting manual parsing.");
//                     ParseJsonManually(requestBody, out deviceId, out sessionId, out eventsAdded, out lastEventInfo);
//                 }
//             }
            
//             // Notify about the received events
//             if (OnEventsReceived != null)
//             {
//                 // Use Unity's main thread to invoke the event
//                 UnityMainThreadDispatcher.Instance().Enqueue(() => {
//                     OnEventsReceived?.Invoke(eventsAdded, deviceId, eventQueue.Count, lastEventInfo);
//                 });
//             }
            
//             // Send a successful response
//             SendResponse(context, 200, "OK");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error processing request: {e.Message}");
//             SendResponse(context, 500, "Internal Server Error");
//         }
//     }

//     private void ParseJsonManually(string jsonString, out string deviceId, out string sessionId, out int eventsAdded, out string lastEventInfo)
//     {
//         deviceId = "unknown";
//         sessionId = "unknown";
//         eventsAdded = 0;
//         lastEventInfo = "None";
        
//         try
//         {
//             // Parse JSON manually since Unity's JsonUtility doesn't handle all JSON formats well
//             Dictionary<string, object> jsonDict = MiniJSON.Json.Deserialize(jsonString) as Dictionary<string, object>;
            
//             if (jsonDict == null)
//             {
//                 Debug.LogError("Failed to parse JSON manually");
//                 return;
//             }
            
//             deviceId = jsonDict.ContainsKey("deviceId") ? jsonDict["deviceId"] as string : "unknown";
//             sessionId = jsonDict.ContainsKey("sessionId") ? jsonDict["sessionId"] as string : "unknown";
            
//             if (jsonDict.ContainsKey("payload") && jsonDict["payload"] is List<object> payloadList)
//             {
//                 foreach (object eventObj in payloadList)
//                 {
//                     if (eventObj is Dictionary<string, object> eventDict)
//                     {
//                         SensorEvent sensorEvent = new SensorEvent();
                        
//                         if (eventDict.ContainsKey("name"))
//                         {
//                             sensorEvent.name = eventDict["name"] as string;
//                             lastEventInfo = sensorEvent.name;
//                         }
                            
//                         if (eventDict.ContainsKey("time") && eventDict["time"] is long timeVal)
//                             sensorEvent.time = timeVal;
                            
//                         if (eventDict.ContainsKey("values") && eventDict["values"] is Dictionary<string, object> valuesDict)
//                             sensorEvent.values = SensorValues.FromDictionary(valuesDict);
                        
//                         eventQueue.Enqueue(sensorEvent);
//                         eventsAdded++;
//                     }
//                 }
                
//                 Debug.Log($"Manually parsed and added {eventsAdded} events from device {deviceId}, session {sessionId}");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error during manual JSON parsing: {e.Message}");
//         }
//     }

//     private void SendResponse(HttpListenerContext context, int statusCode, string statusDescription)
//     {
//         context.Response.StatusCode = statusCode;
//         context.Response.StatusDescription = statusDescription;
        
//         string responseString = $"{{ \"status\": \"{statusCode}\", \"message\": \"{statusDescription}\" }}";
//         byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        
//         context.Response.ContentLength64 = buffer.Length;
//         context.Response.ContentType = "application/json";
        
//         context.Response.OutputStream.Write(buffer, 0, buffer.Length);
//         context.Response.OutputStream.Close();
//     }

//     private void ProcessEvents()
//     {
//         List<SensorEvent> batchEvents = new List<SensorEvent>();
//         DateTime lastProcessTime = DateTime.Now;
        
//         while (isRunning)
//         {
//             SensorEvent eventToProcess = null;
//             bool shouldProcessBatch = false;
            
//             lock (queueLock)
//             {
//                 if (eventQueue.Count > 0)
//                 {
//                     eventToProcess = eventQueue.Dequeue();
//                     batchEvents.Add(eventToProcess);
//                 }
                
//                 // Process batch if we have enough events or enough time has passed
//                 if (batchEvents.Count >= 20 || (batchEvents.Count > 0 && (DateTime.Now - lastProcessTime).TotalMilliseconds > 100))
//                 {
//                     shouldProcessBatch = true;
//                 }
//             }
            
//             // If it's time to process the batch
//             if (shouldProcessBatch && batchEvents.Count > 0)
//             {
//                 // Process the events as a batch to get motion data
//                 Quaternion leftRotation, rightRotation;
//                 Vector3 leftAcceleration, rightAcceleration;
                
//                 if (MotionDataConverter.ExtractMotionData(batchEvents.ToArray(), 
//                     out leftRotation, out leftAcceleration, 
//                     out rightRotation, out rightAcceleration))
//                 {
//                     // Invoke the event on the main thread
//                     UnityMainThreadDispatcher.Instance().Enqueue(() => {
//                         OnMotionDataReceived?.Invoke(leftRotation, leftAcceleration, rightRotation, rightAcceleration);
//                     });
                    
//                     Debug.Log($"Processed batch of {batchEvents.Count} events, generated motion data");
//                 }
                
//                 // Log the events
//                 foreach (var sensorEvent in batchEvents)
//                 {
//                     Debug.Log(sensorEvent.ToString());
                    
//                     // Additional logging based on event type
//                     switch (sensorEvent.name)
//                     {
//                         case "accelerometer":
//                         case "accelerometeruncalibrated":
//                             // Process accelerometer data
//                             Debug.Log($"Processing accelerometer: {sensorEvent.values}");
//                             break;
                        
//                         case "gyroscope":
//                         case "gyroscopeuncalibrated":
//                             // Process gyroscope data
//                             Debug.Log($"Processing gyroscope: {sensorEvent.values}");
//                             break;
                        
//                         case "orientation":
//                             // Process orientation data
//                             Debug.Log($"Processing orientation: {sensorEvent.values}");
//                             break;
//                     }
//                 }
                
//                 // Clear the batch and reset timer
//                 batchEvents.Clear();
//                 lastProcessTime = DateTime.Now;
//             }
            
//             // If no events to process, sleep for a bit
//             if (eventToProcess == null)
//             {
//                 Thread.Sleep(10);
//             }
//         }
//     }
// }

// // Helper class to execute actions on the main Unity thread
// public class UnityMainThreadDispatcher : MonoBehaviour
// {
//     private static readonly Queue<Action> _executionQueue = new Queue<Action>();
//     private static UnityMainThreadDispatcher _instance = null;
    
//     public static UnityMainThreadDispatcher Instance()
//     {
//         if (_instance == null)
//         {
//             GameObject go = new GameObject("UnityMainThreadDispatcher");
//             _instance = go.AddComponent<UnityMainThreadDispatcher>();
//             DontDestroyOnLoad(go);
//         }
//         return _instance;
//     }
    
//     private void Update()
//     {
//         lock(_executionQueue)
//         {
//             while (_executionQueue.Count > 0)
//             {
//                 _executionQueue.Dequeue()?.Invoke();
//             }
//         }
//     }
    
//     public void Enqueue(Action action)
//     {
//         lock(_executionQueue)
//         {
//             _executionQueue.Enqueue(action);
//         }
//     }
// }