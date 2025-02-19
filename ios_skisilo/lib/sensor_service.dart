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

  Future<void> sendJsonData(Map<String, dynamic> data) async {
    try {
      _socket ??= await RawDatagramSocket.bind(InternetAddress.anyIPv4, 0);
      
      final jsonString = jsonEncode(data);
      final bytes = utf8.encode(jsonString);
      _socket!.send(bytes, InternetAddress(unityIp), unityPort);
    } catch (e) {
      print('Error sending data: $e');
    }
  }

  void dispose() {
    _socket?.close();
  }
}