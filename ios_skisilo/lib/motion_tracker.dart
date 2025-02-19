import 'package:flutter/material.dart';
import 'package:sensors_plus/sensors_plus.dart';
import 'dart:async';
import 'dart:math' as math;
import 'dart:convert';
import 'sensor_service.dart';

class MotionTracker extends StatefulWidget {
  @override
  _MotionTrackerState createState() => _MotionTrackerState();
}

class _MotionTrackerState extends State<MotionTracker> {
  late SensorService sensorService;
  bool isStreaming = false;

  bool isTrackingLocation =
      false; // For controlling whether position is reported

  // Motion data
  double roll = 0.0;
  double pitch = 0.0;
  double yaw = 0.0;
  double posX = 0.0;
  double posY = 0.0;
  double posZ = 0.0;

  // Velocity variables for integration
  double velX = 0.0;
  double velY = 0.0;
  double velZ = 0.0;
  DateTime? lastAccelTime;

  // Sensor calibration values
  double gyroXOffset = 0.0;
  double gyroYOffset = 0.0;
  double gyroZOffset = 0.0;

  // Calibration samples
  List<double> gyroXSamples = [];
  List<double> gyroYSamples = [];
  List<double> gyroZSamples = [];
  bool isCalibrating = false;
  static const int CALIBRATION_SAMPLES = 2;

  // Thresholds for motion detection
  static const double GYRO_THRESHOLD = 0.02;
  static const double ACCEL_THRESHOLD = 0.5;

  // Sensor subscriptions
  List<StreamSubscription<dynamic>> _streamSubscriptions = [];
  StreamSubscription<GyroscopeEvent>? gyroSubscription;
  StreamSubscription<UserAccelerometerEvent>? userAccelSubscription;
  DateTime? lastUpdateTime;

  @override
  void initState() {
    super.initState();
    sensorService = SensorService(unityIp: '192.168.0.18', unityPort: 5005);
    calibrateSensors();
  }

  Future<void> calibrateSensors() async {
    setState(() {
      isCalibrating = true;
      gyroXSamples.clear();
      gyroYSamples.clear();
      gyroZSamples.clear();
    });

    // Use the gyroscope to collect calibration samples.
    gyroSubscription = gyroscopeEvents.listen((GyroscopeEvent event) {
      if (gyroXSamples.length < CALIBRATION_SAMPLES) {
        gyroXSamples.add(event.x);
        gyroYSamples.add(event.y);
        gyroZSamples.add(event.z);
      } else if (isCalibrating) {
        gyroXOffset =
            gyroXSamples.reduce((a, b) => a + b) / CALIBRATION_SAMPLES;
        gyroYOffset =
            gyroYSamples.reduce((a, b) => a + b) / CALIBRATION_SAMPLES;
        gyroZOffset =
            gyroZSamples.reduce((a, b) => a + b) / CALIBRATION_SAMPLES;

        gyroSubscription?.cancel();
        setState(() {
          isCalibrating = false;
        });
        initTracking();
      }
    });
  }

  void initTracking() {
    // Orientation tracking using accelerometerEvents (including gravity)
    _streamSubscriptions.add(
      accelerometerEvents.listen((AccelerometerEvent event) {
        if (!isStreaming) return;

        setState(() {
          double newPitch = math.atan2(
            -event.x,
            math.sqrt(event.y * event.y + event.z * event.z),
          );
          double newRoll = math.atan2(event.y, event.z);

          // Convert to degrees.
          newPitch = newPitch * (180 / math.pi);
          newRoll = newRoll * (180 / math.pi);

          if ((newPitch - pitch).abs() > ACCEL_THRESHOLD) {
            pitch = newPitch;
          }
          if ((newRoll - roll).abs() > ACCEL_THRESHOLD) {
            roll = newRoll;
          }
          sendData();
        });
      }),
    );

    // Position tracking using userAccelerometerEvents (gravity removed)
    userAccelSubscription = userAccelerometerEvents.listen((
      UserAccelerometerEvent event,
    ) {
      if (!isStreaming) return;
      final now = DateTime.now();
      double dt =
          lastAccelTime == null
              ? 0.01
              : now.difference(lastAccelTime!).inMicroseconds / 1000000.0;
      lastAccelTime = now;

      setState(() {
        // Integrate acceleration to update velocity.
        velX += event.x * dt;
        velY += event.y * dt;
        velZ += event.z * dt;

        // Integrate velocity to update position.
        posX += velX * dt;
        posY += velY * dt;
        posZ += velZ * dt;

        sendData();
      });
    });

    // Gyroscope for yaw tracking.
    _streamSubscriptions.add(
      gyroscopeEvents.listen((GyroscopeEvent event) {
        if (!isStreaming) return;

        final now = DateTime.now();
        if (lastUpdateTime != null) {
          final dt = now.difference(lastUpdateTime!).inMicroseconds / 1000000.0;

          double correctedZ = event.z - gyroZOffset;
          if (correctedZ.abs() > GYRO_THRESHOLD) {
            setState(() {
              yaw += correctedZ * (180 / math.pi) * dt;
              // Normalize yaw to the range -180 to 180.
              while (yaw > 180) yaw -= 360;
              while (yaw < -180) yaw += 360;
              sendData();
            });
          }
        }
        lastUpdateTime = now;
      }),
    );
  }

  void sendData() {
    final data = {
      'legs': {
        'left': {'pitch': pitch, 'yaw': yaw, 'roll': roll},
        'right': {'pitch': pitch, 'yaw': yaw, 'roll': roll},
      },
      'position': {
        'x': isTrackingLocation ? posX : 0.0,
        'y': isTrackingLocation ? posY : 0.0,
        'z': isTrackingLocation ? posZ : 0.0,
      },
    };

    sensorService.sendJsonData(data);
  }

  void resetMotion() {
    setState(() {
      roll = 0.0;
      pitch = 0.0;
      yaw = 0.0;
      posX = 0.0;
      posY = 0.0;
      posZ = 0.0;
      // Reset velocities and timing for accurate integration.
      velX = 0.0;
      velY = 0.0;
      velZ = 0.0;
      lastUpdateTime = null;
      lastAccelTime = null;
      calibrateSensors();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Motion Tracker'),
        actions: [
          if (isCalibrating)
            Center(
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: Text('Calibrating...'),
              ),
            ),
          IconButton(
            icon: Icon(
              isTrackingLocation ? Icons.location_on : Icons.location_off,
              color: isTrackingLocation ? Colors.green : null,
            ),
            onPressed:
                isCalibrating
                    ? null
                    : () {
                      setState(() {
                        isTrackingLocation = !isTrackingLocation;
                        if (!isTrackingLocation) {
                          posX = 0.0;
                          posY = 0.0;
                          posZ = 0.0;
                        }
                        sendData();
                      });
                    },
          ),
          IconButton(
            icon: Icon(isStreaming ? Icons.stop : Icons.play_arrow),
            onPressed:
                isCalibrating
                    ? null
                    : () {
                      setState(() {
                        isStreaming = !isStreaming;
                        if (isStreaming) {
                          lastUpdateTime = DateTime.now();
                          lastAccelTime = DateTime.now();
                        }
                      });
                    },
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Orientation',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text('Roll: ${roll.toStringAsFixed(2)}°'),
                    Text('Pitch: ${pitch.toStringAsFixed(2)}°'),
                    Text('Yaw: ${yaw.toStringAsFixed(2)}°'),
                  ],
                ),
              ),
            ),
            SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Position',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text('X: ${posX.toStringAsFixed(2)}'),
                    Text('Y: ${posY.toStringAsFixed(2)}'),
                    Text('Z: ${posZ.toStringAsFixed(2)}'),
                  ],
                ),
              ),
            ),
            Spacer(),
            ElevatedButton.icon(
              onPressed: isCalibrating ? null : resetMotion,
              icon: Icon(Icons.refresh),
              label: Text('Reset & Calibrate'),
              style: ElevatedButton.styleFrom(
                minimumSize: Size(double.infinity, 50),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    gyroSubscription?.cancel();
    userAccelSubscription?.cancel();
    for (final subscription in _streamSubscriptions) {
      subscription.cancel();
    }
    super.dispose();
  }
}
