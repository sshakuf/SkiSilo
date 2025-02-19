import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'motion_tracker.dart'; // Make sure this matches your file name

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  // Lock orientation to portrait
  SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]).then((
    _,
  ) {
    runApp(MyApp());
  });
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Motion Tracker App',
      theme: ThemeData(
        primarySwatch: Colors.blue,
        useMaterial3: true, // Using Material 3 design
        brightness: Brightness.light,
      ),
      darkTheme: ThemeData(
        primarySwatch: Colors.blue,
        useMaterial3: true,
        brightness: Brightness.dark,
      ),
      home: MotionTracker(),
      debugShowCheckedModeBanner: false,
    );
  }
}
