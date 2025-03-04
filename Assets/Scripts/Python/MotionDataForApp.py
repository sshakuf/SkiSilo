import socket
import threading
import json
import time
import queue
from http.server import HTTPServer, BaseHTTPRequestHandler

# Configuration constants
ACC_SCALE_FACTOR = 10.0  # Scale factor for acceleration values
MAX_ANGLE = 180.0         # Maximum angle value for pitch/yaw/roll

# UDP Handler
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
            self.wfile.write(json.dumps({
                'status': 'error', 
                'message': 'Unsupported endpoint'
            }).encode())
            return
        
        content_length = int(self.headers['Content-Length'])
        post_data = self.rfile.read(content_length)
        
        try:
            json_data = json.loads(post_data.decode('utf-8'))
            num_events = len(json_data.get('payload', []))
            print(f"Received data on {self.path} with {num_events} events")
            
            # Attach leg info to each event and queue them
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
            self.wfile.write(json.dumps({
                'status': 'error', 
                'message': str(e)
            }).encode())

def process_events():
    """
    Process events from the queue and forward them via UDP.
    Each UDP message now contains a JSON object with a 'legs' key,
    containing either the left or right leg's data, including gravity.
    """
    while True:
        try:
            # Collect a batch of events
            events_batch = []
            while not event_queue.empty() and len(events_batch) < 10:
                events_batch.append(event_queue.get_nowait())
            
            if not events_batch:
                time.sleep(0.001)
                continue
            
            # Initialize data dictionaries for both legs with nested structure
            _data = {
                "yaw": 0.0, 
                "pitch": 0.0, 
                "roll": 0.0,
                "acc": {
                    "x": 0.0, 
                    "y": 0.0, 
                    "z": 0.0
                },
                "gravity": {
                    "x": 0.0, 
                    "y": 0.0, 
                    "z": 0.0
                }
            }
            
            _updated = False
            _gravity_updated = False
            
            for event in events_batch:
                leg = event.get('leg')
                event_name = event.get('name', '')
                values = event.get('values', {})
                
                if event_name == 'accelerometer':
                    # Set acceleration values in the nested structure
                    _data["acc"]["x"] = values.get('x', 0) * ACC_SCALE_FACTOR
                    _data["acc"]["y"] = values.get('y', 0) * ACC_SCALE_FACTOR
                    _data["acc"]["z"] = values.get('z', 0) * ACC_SCALE_FACTOR
                    _updated = True
                    
                elif event_name == 'gravity':
                    # Set gravity values in the nested structure
                    _data["gravity"]["x"] = values.get('x', 0)
                    _data["gravity"]["y"] = values.get('y', 0)
                    _data["gravity"]["z"] = values.get('z', 0)
                    _gravity_updated = True
                    
                elif event_name == 'orientation':
                    pitch = values.get('pitch', 0)
                    yaw = values.get('yaw', 0)
                    roll = values.get('roll', 0)
                    
                    # Convert radians to degrees if values are small (likely radians)
                    if abs(pitch) < 3.15 and abs(yaw) < 3.15 and abs(roll) < 3.15:
                        pitch = pitch * (180.0 / 3.14159)
                        yaw = yaw * (180.0 / 3.14159)
                        roll = roll * (180.0 / 3.14159)
                    
                    _data["pitch"] = max(-MAX_ANGLE, min(MAX_ANGLE, pitch))
                    _data["yaw"] = max(-MAX_ANGLE, min(MAX_ANGLE, yaw))
                    _data["roll"] = max(-MAX_ANGLE, min(MAX_ANGLE, roll))
                    _updated = True
            
            # Send UDP data only for the updated leg with the nested structure
            if (_updated or _gravity_updated) and leg:
                udp_message = {"legs": {leg: _data}}
                udp_handler.send_data(udp_message)
                #print(f"Sent {leg} UDP: {udp_message}")
                
            time.sleep(0.001)

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