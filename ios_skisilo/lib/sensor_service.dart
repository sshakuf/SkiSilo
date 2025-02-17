import 'dart:async';
import 'dart:convert';
import 'dart:io';

class SensorService {
  final String unityIp;
  final int unityPort;
  RawDatagramSocket? _socket;
  
  SensorService({
    required this.unityIp,
    required this.unityPort,
  });

  Future<void> sendData(Map<String, double> data) async {
    try {
      _socket ??= await RawDatagramSocket.bind(InternetAddress.anyIPv4, 0);
      
      // Format string to match Python simulator: "left_x,left_y,left_z,right_x,right_y,right_z"
      final dataString = "${data['left_x']},${data['left_y']},${data['left_z']},"
                        "${data['right_x']},${data['right_y']},${data['right_z']}";
      
      final bytes = utf8.encode(dataString);
      _socket!.send(bytes, InternetAddress(unityIp), unityPort);
    } catch (e) {
      print('Error sending data: $e');
    }
  }

  void dispose() {
    _socket?.close();
  }
}