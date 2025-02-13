import socket
import tkinter as tk

# UDP Settings
UDP_IP = "127.0.0.1"  # Change if needed
UDP_PORT = 5005

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Initial accelerometer values
accel_data = {
    "left_x": 0,
    "left_y": 0,
    "left_z": 0,
    "right_x": 0,
    "right_y": 0,
    "right_z": 0,
}

# Function to send data over UDP
def send_data():
    data = f"{accel_data['left_x']},{accel_data['left_y']},{accel_data['left_z']},{accel_data['right_x']},{accel_data['right_y']},{accel_data['right_z']}"
    sock.sendto(data.encode(), (UDP_IP, UDP_PORT))
    label.config(text=f"Sent: {data}")  # Update label

# Update function for sliders
def update_value(key, value):
    accel_data[key] = float(value)
    send_data()

# Create UI
root = tk.Tk()
root.title("Accelerometer Simulator")

# Create UI Elements
label = tk.Label(root, text="Move sliders to simulate accelerometer data", font=("Arial", 12))
label.pack()

# Sliders for Left Leg
tk.Label(root, text="Left Leg X").pack()
left_x_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("left_x", v))
left_x_slider.pack()

tk.Label(root, text="Left Leg Y").pack()
left_y_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("left_y", v))
left_y_slider.pack()

tk.Label(root, text="Left Leg Z").pack()
left_z_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("left_z", v))
left_z_slider.pack()

# Sliders for Right Leg
tk.Label(root, text="Right Leg X").pack()
right_x_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("right_x", v))
right_x_slider.pack()

tk.Label(root, text="Right Leg Y").pack()
right_y_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("right_y", v))
right_y_slider.pack()

tk.Label(root, text="Right Leg Z").pack()
right_z_slider = tk.Scale(root, from_=-90, to=90, orient="horizontal", command=lambda v: update_value("right_z", v))
right_z_slider.pack()

# Run UI loop
root.mainloop()