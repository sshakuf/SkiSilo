import socket
import threading
import json
import time
import queue
from http.server import HTTPServer, BaseHTTPRequestHandler

# Configuration constants
ACC_SCALE_FACTOR = 100.0  # Scale factor for acceleration values
MAX_ANGLE = 180.0  # Maximum angle value for pitch/yaw/roll

# UDP Handler (copied from your example)
class UDPHandler:
    """Handles UDP communication for motion data."""
    
    def __init__(self, ip="127.0.0.1", port=5005):
        self.UDP_IP = ip
        self.UDP_PORT = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send_data(self, data):
        """Send JSON data via UDP."""
        json_data = json.dumps(data)
        self.sock.sendto(json_data.encode(), (self.UDP_IP, self.UDP_PORT))
    
    def update_connection(self, ip, port):
        """Update UDP IP and port settings."""
        self.UDP_IP = ip
        self.UDP_PORT = int(port)

# Queue for event processing
event_queue = queue.Queue()

# UDP Handler instance
udp_handler = UDPHandler()

class MotionDataHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        
        try:
            json_data = json.loads(post_data.decode('utf-8'))
            print(f"Received data with {len(json_data.get('payload', []))} events")
            
            # Put all events in the queue
            if 'payload' in json_data:
                for event in json_data['payload']:
                    event_queue.put(event)
            
            # Send a response
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'status': 'success'}).encode())
        
        except Exception as e:
            print(f"Error processing request: {e}")
            self.send_response(400)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'status': 'error', 'message': str(e)}).encode())

def process_events():
    """
    Process events from the queue and forward them to the motion simulator.
    """
    # Motion data structure (copied from your example)
    motion_data = {
        "legs": {
            "left": {
                "pitch": 0, "yaw": 0, "roll": 0,
                "accX": 0, "accY": 0, "accZ": 0
            },
            "right": {
                "pitch": 0, "yaw": 0, "roll": 0,
                "accX": 0, "accY": 0, "accZ": 0
            }
        }
    }

    while True:
        try:
            # Process events in batches
            events_batch = []
            while not event_queue.empty() and len(events_batch) < 10:
                events_batch.append(event_queue.get_nowait())
            
            if not events_batch:
                time.sleep(0.01)  # Sleep a bit if no events
                continue
                
            # Process the batch
            for event in events_batch:
                event_name = event.get('name', '')
                values = event.get('values', {})
                
                if event_name == 'accelerometer':
                    # Scale acceleration values and apply to left leg
                    motion_data["legs"]["left"]["accX"] = values.get('x', 0) * ACC_SCALE_FACTOR
                    motion_data["legs"]["left"]["accY"] = values.get('y', 0) * ACC_SCALE_FACTOR
                    motion_data["legs"]["left"]["accZ"] = values.get('z', 0) * ACC_SCALE_FACTOR
                    
                elif event_name == 'orientation':
                    # Ensure pitch/yaw/roll are within -180 to 180 range
                    pitch = values.get('pitch', 0)
                    yaw = values.get('yaw', 0)
                    roll = values.get('roll', 0)
                    
                    # Convert radians to degrees if values are small (likely radians)
                    if abs(pitch) < 3.15 and abs(yaw) < 3.15 and abs(roll) < 3.15:
                        pitch = pitch * (180.0 / 3.14159)
                        yaw = yaw * (180.0 / 3.14159)
                        roll = roll * (180.0 / 3.14159)
                    
                    # Clamp values to -180 to 180 range
                    motion_data["legs"]["left"]["pitch"] = max(-MAX_ANGLE, min(MAX_ANGLE, pitch))
                    motion_data["legs"]["left"]["yaw"] = max(-MAX_ANGLE, min(MAX_ANGLE, yaw))
                    motion_data["legs"]["left"]["roll"] = max(-MAX_ANGLE, min(MAX_ANGLE, roll))
                    
                elif event_name == 'gyroscope':
                    # Scale gyroscope values and apply to right leg
                    # Convert to degrees if in radians
                    x = values.get('x', 0)
                    y = values.get('y', 0)
                    z = values.get('z', 0)
                    
                    # If values are small (likely radians), convert to degrees
                    if abs(x) < 3.15 and abs(y) < 3.15 and abs(z) < 3.15:
                        x = x * (180.0 / 3.14159)
                        y = y * (180.0 / 3.14159)
                        z = z * (180.0 / 3.14159)
                    
                    # Clamp values to -180 to 180 range
                    motion_data["legs"]["right"]["pitch"] = max(-MAX_ANGLE, min(MAX_ANGLE, x))
                    motion_data["legs"]["right"]["yaw"] = max(-MAX_ANGLE, min(MAX_ANGLE, y))
                    motion_data["legs"]["right"]["roll"] = max(-MAX_ANGLE, min(MAX_ANGLE, z))
            
            # Send the processed data
            udp_handler.send_data(motion_data)
            print(f"Sent motion data: {motion_data}")
            
            time.sleep(0.01)
            # Mark events as processed
            for _ in events_batch:
                event_queue.task_done()
                
        except Exception as e:
            print(f"Error in event processing: {e}")
            time.sleep(0.1)  # Sleep on error

def run_server(server_class=HTTPServer, handler_class=MotionDataHandler, port=8080):
    """
    Run the HTTP server to receive motion data.
    """
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    print(f"Starting HTTP server on port {port}...")
    httpd.serve_forever()

if __name__ == "__main__":
    # Start event processing thread
    processing_thread = threading.Thread(target=process_events, daemon=True)
    processing_thread.start()
    
    # Configure UDP connection (you can modify these settings)
    udp_handler.update_connection("127.0.0.1", 5005)
    
    # Start the server
    run_server()