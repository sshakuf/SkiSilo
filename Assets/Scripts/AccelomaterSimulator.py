import socket
import tkinter as tk
import json

# UDP Settings
UDP_IP = "127.0.0.1"
UDP_PORT = 5005

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Motion data structure
motion_data = {
    "legs": {
        "left": {
            "pitch": 0,
            "yaw": 0,
            "roll": 0
        },
        "right": {
            "pitch": 0,
            "yaw": 0,
            "roll": 0
        }
    },
    "position": {
        "x": 0,
        "y": 0,
        "z": 0
    }
}

def send_data():
    data = json.dumps(motion_data)
    sock.sendto(data.encode(), (UDP_IP, UDP_PORT))
    status_label.config(text=f"Last sent: {data}")

def update_leg(leg, axis, value):
    motion_data["legs"][leg][axis] = float(value)
    send_data()

def update_position(axis, value):
    motion_data["position"][axis] = float(value)
    send_data()

# Create UI
root = tk.Tk()
root.title("Motion Simulator")

# Main container
main_container = tk.Frame(root)
main_container.pack(expand=True, fill='both', padx=10, pady=10)

# Status Label
status_label = tk.Label(main_container, text="Adjust sliders to simulate motion", 
                       font=("Arial", 10), wraplength=400)
status_label.pack(pady=5)

# Create frames for left leg, right leg, and position
left_frame = tk.LabelFrame(main_container, text="Left Leg", padx=5, pady=5)
left_frame.pack(side=tk.LEFT, expand=True, fill='both', padx=5)

right_frame = tk.LabelFrame(main_container, text="Right Leg", padx=5, pady=5)
right_frame.pack(side=tk.LEFT, expand=True, fill='both', padx=5)

position_frame = tk.LabelFrame(main_container, text="Position", padx=5, pady=5)
position_frame.pack(side=tk.LEFT, expand=True, fill='both', padx=5)

def create_slider(parent, text, command):
    frame = tk.Frame(parent)
    frame.pack(fill='x', pady=2)
    
    label = tk.Label(frame, text=text, width=10)
    label.pack(side=tk.LEFT)
    
    slider = tk.Scale(frame, from_=-180, to=180, orient="horizontal",
                     command=command)
    slider.pack(side=tk.LEFT, fill='x', expand=True)
    return slider

# Left Leg Controls
for axis in ["pitch", "yaw", "roll"]:
    create_slider(left_frame, axis.capitalize(),
                 lambda v, a=axis: update_leg("left", a, v))

# Right Leg Controls
for axis in ["pitch", "yaw", "roll"]:
    create_slider(right_frame, axis.capitalize(),
                 lambda v, a=axis: update_leg("right", a, v))

# Position Controls
for axis in ["x", "y", "z"]:
    create_slider(position_frame, f"{axis.upper()}-Pos",
                 lambda v, a=axis: update_position(a, v))

# Reset Buttons Frame
reset_frame = tk.Frame(main_container)
reset_frame.pack(fill='x', pady=10)

def reset_all():
    for frame in [left_frame, right_frame, position_frame]:
        for widget in frame.winfo_children():
            if isinstance(widget, tk.Frame):  # Each slider is in a frame
                for child in widget.winfo_children():
                    if isinstance(child, tk.Scale):
                        child.set(0)
    send_data()

reset_btn = tk.Button(reset_frame, text="Reset All", command=reset_all)
reset_btn.pack(expand=True, fill='x')

# IP Configuration
ip_frame = tk.Frame(main_container)
ip_frame.pack(fill='x', pady=5)

tk.Label(ip_frame, text="UDP IP:").pack(side=tk.LEFT)
ip_entry = tk.Entry(ip_frame)
ip_entry.insert(0, UDP_IP)
ip_entry.pack(side=tk.LEFT, fill='x', expand=True)

tk.Label(ip_frame, text="Port:").pack(side=tk.LEFT)
port_entry = tk.Entry(ip_frame, width=10)
port_entry.insert(0, str(UDP_PORT))
port_entry.pack(side=tk.LEFT)

def update_connection():
    global UDP_IP, UDP_PORT
    UDP_IP = ip_entry.get()
    UDP_PORT = int(port_entry.get())
    status_label.config(text=f"Updated connection: {UDP_IP}:{UDP_PORT}")

tk.Button(ip_frame, text="Update", command=update_connection).pack(side=tk.LEFT, padx=5)

root.mainloop()