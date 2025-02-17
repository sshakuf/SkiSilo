import 'package:flutter/material.dart';
import 'package:sensors_plus/sensors_plus.dart';
import 'dart:async';
import 'dart:math' as math;
import 'sensor_service.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  late SensorService sensorService;
  bool isStreaming = false;
  
  // Sensor values
  double roll = 0, pitch = 0, yaw = 0;
  double lastYaw = 0;
  DateTime? lastUpdateTime;
  
  List<StreamSubscription<dynamic>> _streamSubscriptions = [];

  @override
  void initState() {
    super.initState();
    sensorService = SensorService(unityIp: '192.168.0.18', unityPort: 5005);
    initSensors();
  }

  void initSensors() {
    // Accelerometer for roll and pitch
    _streamSubscriptions.add(
      accelerometerEvents.listen((AccelerometerEvent event) {
        setState(() {
          // Calculate roll (rotation around X-axis)
          roll = math.atan2(event.y, event.z) * (180 / math.pi);
          
          // Calculate pitch (rotation around Y-axis)
          pitch = math.atan2(-event.x, 
              math.sqrt(event.y * event.y + event.z * event.z)) * (180 / math.pi);
          
          if (isStreaming) {
            sendData();
          }
        });
      })
    );

    // Gyroscope for yaw integration
    _streamSubscriptions.add(
      gyroscopeEvents.listen((GyroscopeEvent event) {
        setState(() {
          final now = DateTime.now();
          if (lastUpdateTime != null) {
            final dt = now.difference(lastUpdateTime!).inMicroseconds / 1000000; // Convert to seconds
            
            // Integrate gyroscope data for yaw (rotation around Z-axis)
            yaw += event.z * (180 / math.pi) * dt;
            
            // Normalize yaw to -180 to 180 range
            while (yaw > 180) yaw -= 360;
            while (yaw < -180) yaw += 360;
            
            if (isStreaming) {
              sendData();
            }
          }
          lastUpdateTime = now;
        });
      })
    );
  }

  void sendData() {
    // Using the same values for both legs for now
    final data = {
      'left_x': roll,
      'left_y': pitch,
      'left_z': yaw,
      'right_x': roll,
      'right_y': pitch,
      'right_z': yaw,
    };
    sensorService.sendData(data);
  }

  void toggleStreaming() {
    setState(() {
      isStreaming = !isStreaming;
      if (isStreaming) {
        yaw = 0; // Reset yaw when starting streaming
        lastUpdateTime = DateTime.now();
        sendData();
      }
    });
  }

  void resetOrientation() {
    setState(() {
      yaw = 0;
      lastUpdateTime = DateTime.now();
      if (isStreaming) {
        sendData();
      }
    });
  }

  Widget buildOrientationDisplay() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Device Orientation', 
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            SizedBox(height: 16),
            _buildOrientationRow('Roll (X)', roll),
            _buildOrientationRow('Pitch (Y)', pitch),
            _buildOrientationRow('Yaw (Z)', yaw),
          ],
        ),
      ),
    );
  }

  Widget _buildOrientationRow(String label, double value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontSize: 16)),
          Text('${value.toStringAsFixed(1)}°', 
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      theme: ThemeData(
        primarySwatch: Colors.blue,
        cardTheme: CardTheme(
          elevation: 4,
          margin: EdgeInsets.symmetric(vertical: 8),
        ),
      ),
      home: Scaffold(
        appBar: AppBar(
          title: Text('Roll Pitch Yaw Streaming'),
          actions: [
            IconButton(
              icon: Icon(isStreaming ? Icons.stop : Icons.play_arrow),
              onPressed: toggleStreaming,
              tooltip: isStreaming ? 'Stop Streaming' : 'Start Streaming',
            ),
          ],
        ),
        body: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              buildOrientationDisplay(),
              SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: toggleStreaming,
                icon: Icon(isStreaming ? Icons.stop : Icons.play_arrow),
                label: Text(isStreaming ? 'Stop Streaming' : 'Start Streaming'),
                style: ElevatedButton.styleFrom(
                  padding: EdgeInsets.symmetric(vertical: 12),
                ),
              ),
              SizedBox(height: 8),
              ElevatedButton.icon(
                onPressed: resetOrientation,
                icon: Icon(Icons.refresh),
                label: Text('Reset Yaw'),
                style: ElevatedButton.styleFrom(
                  padding: EdgeInsets.symmetric(vertical: 12),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    for (final subscription in _streamSubscriptions) {
      subscription.cancel();
    }
    sensorService.dispose();
    super.dispose();
  }
}