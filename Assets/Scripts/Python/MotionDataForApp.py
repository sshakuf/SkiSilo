import socket
import threading
import json
import time
import queue
from http.server import HTTPServer, BaseHTTPRequestHandler

# Configuration constants
ACC_SCALE_FACTOR = 100.0  # Scale factor for acceleration values
MAX_ANGLE = 180.0         # Maximum angle value for pitch/yaw/roll

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
        # Determine which leg based on the URL path
        if self.path == '/left':
            leg = 'left'
        elif self.path == '/right':
            leg = 'right'
        else:
            self.send_response(404)
            self.send_header('Content-type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({'status': 'error', 'message': 'Unsupported endpoint'}).encode())
            return
        
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        
        try:
            json_data = json.loads(post_data.decode('utf-8'))
            num_events = len(json_data.get('payload', []))
            print(f"Received data on {self.path} with {num_events} events")
            
            # Add the leg info to each event and queue them
            if 'payload' in json_data:
                for event in json_data['payload']:
                    event['leg'] = leg
                    event_queue.put(event)
            
            # Send a success response
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
    The events are grouped by the leg (left/right) indicated by the HTTP endpoint.
    """
    while True:
        try:
            # Process events in batches
            events_batch = []
            while not event_queue.empty() and len(events_batch) < 10:
                events_batch.append(event_queue.get_nowait())
            
            if not events_batch:
                time.sleep(0.01)
                continue
            
            # Initialize data dictionaries for left and right legs
            left_motion_data = {
                "pitch": 0, "yaw": 0, "roll": 0,
                "accX": 0, "accY": 0, "accZ": 0
            }
            right_motion_data = {
                "pitch": 0, "yaw": 0, "roll": 0,
                "accX": 0, "accY": 0, "accZ": 0
            }
            left_updated = False
            right_updated = False
            
            for event in events_batch:
                leg = event.get('leg')
                event_name = event.get('name', '')
                values = event.get('values', {})
                
                if leg == 'left':
                    if event_name == 'accelerometer':
                        # Scale acceleration values for left leg
                        left_motion_data["accX"] = values.get('x', 0) * ACC_SCALE_FACTOR
                        left_motion_data["accY"] = values.get('y', 0) * ACC_SCALE_FACTOR
                        left_motion_data["accZ"] = values.get('z', 0) * ACC_SCALE_FACTOR
                        left_updated = True
                    elif event_name == 'orientation':
                        pitch = values.get('pitch', 0)
                        yaw = values.get('yaw', 0)
                        roll = values.get('roll', 0)
                        
                        # Convert radians to degrees if values are small
                        if abs(pitch) < 3.15 and abs(yaw) < 3.15 and abs(roll) < 3.15:
                            pitch = pitch * (180.0 / 3.14159)
                            yaw = yaw * (180.0 / 3.14159)
                            roll = roll * (180.0 / 3.14159)
                        
                        # Clamp values to -180 to 180
                        left_motion_data["pitch"] = max(-MAX_ANGLE, min(MAX_ANGLE, pitch))
                        left_motion_data["yaw"] = max(-MAX_ANGLE, min(MAX_ANGLE, yaw))
                        left_motion_data["roll"] = max(-MAX_ANGLE, min(MAX_ANGLE, roll))
                        left_updated = True
                
                elif leg == 'right':
                    if event_name == 'gyroscope':
                        x = values.get('x', 0)
                        y = values.get('y', 0)
                        z = values.get('z', 0)
                        
                        # Convert to degrees if in radians
                        if abs(x) < 3.15 and abs(y) < 3.15 and abs(z) < 3.15:
                            x = x * (180.0 / 3.14159)
                            y = y * (180.0 / 3.14159)
                            z = z * (180.0 / 3.14159)
                        
                        # Clamp values to -180 to 180
                        right_motion_data["pitch"] = max(-MAX_ANGLE, min(MAX_ANGLE, x))
                        right_motion_data["yaw"] = max(-MAX_ANGLE, min(MAX_ANGLE, y))
                        right_motion_data["roll"] = max(-MAX_ANGLE, min(MAX_ANGLE, z))
                        right_updated = True
            
            # Send UDP data only for the updated leg(s)
            if left_updated:
                udp_data_left = {"leg": "left", "data": left_motion_data}
                udp_handler.send_data(udp_data_left)
                print(f"Sent left motion data: {udp_data_left}")
            
            if right_updated:
                udp_data_right = {"leg": "right", "data": right_motion_data}
                udp_handler.send_data(udp_data_right)
                print(f"Sent right motion data: {udp_data_right}")
            
            time.sleep(0.01)
            # Mark events as processed
            for _ in events_batch:
                event_queue.task_done()
                
        except Exception as e:
            print(f"Error in event processing: {e}")
            time.sleep(0.1)

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
    
    # Configure UDP connection (modify these settings if needed)
    udp_handler.update_connection("127.0.0.1", 5005)
    
    # Start the HTTP server
    run_server()