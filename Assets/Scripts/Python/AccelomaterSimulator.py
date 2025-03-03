import socket
import tkinter as tk
import json

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

class MotionSimulator:
    def __init__(self):
        # UDP Handler instance
        self.udp_handler = UDPHandler()

        # Motion data structure (legs only, no position)
        self.motion_data = {
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

        # Initialize UI
        self.root = tk.Tk()
        self.root.title("Motion Simulator")

        self.main_container = tk.Frame(self.root)
        self.main_container.pack(expand=True, fill='both', padx=10, pady=10)

        self.status_label = tk.Label(self.main_container, text="Adjust sliders to simulate motion", font=("Arial", 10), wraplength=400)
        self.status_label.pack(pady=5)

        self.left_frame = tk.LabelFrame(self.main_container, text="Left Leg", padx=5, pady=5)
        self.left_frame.pack(side=tk.LEFT, expand=True, fill='both', padx=5)

        self.right_frame = tk.LabelFrame(self.main_container, text="Right Leg", padx=5, pady=5)
        self.right_frame.pack(side=tk.LEFT, expand=True, fill='both', padx=5)

        self.create_sliders()
        self.create_reset_button()
        self.create_ip_config()

        self.root.mainloop()

    def send_data(self):
        """Send motion data via UDP."""
        self.udp_handler.send_data(self.motion_data)
        self.status_label.config(text=f"Last sent: {json.dumps(self.motion_data)}")

    def update_leg(self, leg, axis, value):
        """Update leg motion data."""
        self.motion_data["legs"][leg][axis] = float(value)
        self.send_data()

    def create_sliders(self):
        """Create sliders for legs only."""
        axes = ["pitch", "yaw", "roll", "accX", "accY", "accZ"]
        for axis in axes:
            self.create_slider(self.left_frame, f"L-{axis.capitalize()}",
                               lambda v, a=axis: self.update_leg("left", a, v))
            self.create_slider(self.right_frame, f"R-{axis.capitalize()}",
                               lambda v, a=axis: self.update_leg("right", a, v))

    def create_slider(self, parent, text, command):
        """Create a slider UI element."""
        frame = tk.Frame(parent)
        frame.pack(fill='x', pady=2)
        
        label = tk.Label(frame, text=text, width=10)
        label.pack(side=tk.LEFT)
        
        slider = tk.Scale(frame, from_=-180, to=180, orient="horizontal", command=command)
        slider.pack(side=tk.LEFT, fill='x', expand=True)
        return slider

    def create_reset_button(self):
        """Create a reset button to reset all values."""
        reset_frame = tk.Frame(self.main_container)
        reset_frame.pack(fill='x', pady=10)

        reset_btn = tk.Button(reset_frame, text="Reset All", command=self.reset_all)
        reset_btn.pack(expand=True, fill='x')

    def reset_all(self):
        """Reset all sliders and data values to zero."""
        for frame in [self.left_frame, self.right_frame]:
            for widget in frame.winfo_children():
                if isinstance(widget, tk.Frame):  # Each slider is in a frame
                    for child in widget.winfo_children():
                        if isinstance(child, tk.Scale):
                            child.set(0)
        self.send_data()

    def create_ip_config(self):
        """Create the IP and port configuration section."""
        ip_frame = tk.Frame(self.main_container)
        ip_frame.pack(fill='x', pady=5)

        tk.Label(ip_frame, text="UDP IP:").pack(side=tk.LEFT)
        self.ip_entry = tk.Entry(ip_frame)
        self.ip_entry.insert(0, self.udp_handler.UDP_IP)
        self.ip_entry.pack(side=tk.LEFT, fill='x', expand=True)

        tk.Label(ip_frame, text="Port:").pack(side=tk.LEFT)
        self.port_entry = tk.Entry(ip_frame, width=10)
        self.port_entry.insert(0, str(self.udp_handler.UDP_PORT))
        self.port_entry.pack(side=tk.LEFT)

        tk.Button(ip_frame, text="Update", command=self.update_connection).pack(side=tk.LEFT, padx=5)

    def update_connection(self):
        """Update UDP connection settings."""
        new_ip = self.ip_entry.get()
        new_port = int(self.port_entry.get())
        self.udp_handler.update_connection(new_ip, new_port)
        self.status_label.config(text=f"Updated connection: {new_ip}:{new_port}")

if __name__ == "__main__":
    MotionSimulator()