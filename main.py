import tkinter as tk
from tkinter import colorchooser, messagebox
import datetime
import threading
import time
import mss
import cv2
import numpy as np
from pynput import mouse
import winsound

class PerfectRecorderApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Perfect Tutorial Recorder")
        self.root.geometry("420x340")
        self.root.configure(bg="#1e1e2f")
        self.root.attributes('-topmost', True)
        
        # State
        self.is_recording = False
        self.is_drawing = False
        self.zoom_active = False
        self.draw_color = (0, 0, 255)  # BGR
        self.mouse_x, self.mouse_y = 0, 0
        self.smooth_x, self.smooth_y = 0, 0
        self.drawing_points = []
        self.current_stroke = []
        self.ripples = []
        
        # Screen info
        self.screen_w = 0
        self.screen_h = 0
        self.target_w = 1280
        self.target_h = 720
        
        # UI
        tk.Label(root, text="🎥 Perfect Recorder", font=('Segoe UI', 18, 'bold'), bg="#1e1e2f", fg="#00d2ff").pack(pady=12)
        
        self.status_lbl = tk.Label(root, text="Ready | 720p Mode", bg="#1e1e2f", fg="#aaa", font=('Segoe UI', 10))
        self.status_lbl.pack(pady=5)
        
        ctrl_frame = tk.Frame(root, bg="#1e1e2f")
        ctrl_frame.pack(pady=12)
        
        self.record_btn = tk.Button(ctrl_frame, text="▶ Start", command=self.toggle_recording, bg="#28a745", fg="white", font=('Segoe UI', 11, 'bold'), width=13, height=2)
        self.record_btn.grid(row=0, column=0, padx=6, pady=6)
        
        self.draw_btn = tk.Button(ctrl_frame, text="✏️ Draw: OFF", command=self.toggle_draw, bg="#444", fg="white", font=('Segoe UI', 11, 'bold'), width=13, height=2)
        self.draw_btn.grid(row=0, column=1, padx=6, pady=6)
        
        self.zoom_btn = tk.Button(ctrl_frame, text="🔍 Zoom: OFF", command=self.toggle_zoom, bg="#444", fg="white", font=('Segoe UI', 11, 'bold'), width=13, height=2)
        self.zoom_btn.grid(row=1, column=0, padx=6, pady=6)
        
        self.clear_btn = tk.Button(ctrl_frame, text="🗑️ Clear All", command=self.clear_drawing, bg="#dc3545", fg="white", font=('Segoe UI', 11, 'bold'), width=13, height=2, state='disabled')
        self.clear_btn.grid(row=1, column=1, padx=6, pady=6)
        
        self.color_btn = tk.Button(ctrl_frame, text="🎨 Color", command=self.pick_color, bg="#00d2ff", fg="black", font=('Segoe UI', 11, 'bold'), width=13, height=2, state='disabled')
        self.color_btn.grid(row=2, column=0, columnspan=2, padx=6, pady=6)
        
        tk.Label(root, text="LEFT drag = Draw | RIGHT click = Ripple + Sound", bg="#1e1e2f", fg="#888", font=('Segoe UI', 9)).pack(side='bottom', pady=12)
        
        self.mouse_listener = None
        
        # Get screen size
        with mss.mss() as sct:
            mon = sct.monitors[1]
            self.screen_w = mon['width']
            self.screen_h = mon['height']

    def toggle_recording(self):
        if not self.is_recording:
            self.is_recording = True
            self.record_btn.config(text="⏹ Stop", bg="#dc3545")
            self.status_lbl.config(text="🔴 Recording...", fg="#ff4b4b")
            threading.Thread(target=self.record_screen, daemon=True).start()
        else:
            self.is_recording = False
            self.record_btn.config(text="▶ Start", bg="#28a745")
            self.status_lbl.config(text="✅ Saved!", fg="#28a745")

    def toggle_draw(self):
        self.is_drawing = not self.is_drawing
        if self.is_drawing:
            self.draw_btn.config(text="✏️ Draw: ON", bg="#00d2ff", fg="black")
            self.clear_btn.config(state='normal')
            self.color_btn.config(state='normal')
            self.start_mouse_listener()
        else:
            self.draw_btn.config(text="✏️ Draw: OFF", bg="#444", fg="white")
            self.clear_btn.config(state='disabled')
            self.color_btn.config(state='disabled')
            self.stop_mouse_listener()
            self.current_stroke = []

    def toggle_zoom(self):
        self.zoom_active = not self.zoom_active
        if self.zoom_active:
            self.zoom_btn.config(text="🔍 Zoom: ON", bg="#ffc107", fg="black")
            self.smooth_x = self.mouse_x
            self.smooth_y = self.mouse_y
        else:
            self.zoom_btn.config(text="🔍 Zoom: OFF", bg="#444", fg="white")

    def start_mouse_listener(self):
        def on_move(x, y):
            self.mouse_x = x
            self.mouse_y = y
        
        def on_click(x, y, button, pressed):
            if not self.is_drawing:
                return
            
            if pressed and button == mouse.Button.left:
                self.current_stroke = [(x, y)]
            elif not pressed and button == mouse.Button.left:
                if self.current_stroke:
                    self.drawing_points.append((self.current_stroke[:], self.draw_color))
                    self.current_stroke = []
            elif pressed and button == mouse.Button.right:
                # Add ripple
                self.ripples.append({
                    'x': x, 
                    'y': y, 
                    'radius': 40, 
                    'max_radius': 40,
                    'frame_count': 0
                })
                # Play sound in separate thread to avoid blocking
                threading.Thread(target=self.play_click_sound, daemon=True).start()
        
        self.mouse_listener = mouse.Listener(on_move=on_move, on_click=on_click)
        self.mouse_listener.start()

    def stop_mouse_listener(self):
        if self.mouse_listener:
            self.mouse_listener.stop()
            self.mouse_listener = None

    def play_click_sound(self):
        try:
            winsound.Beep(1500, 80)
        except:
            pass

    def clear_drawing(self):
        self.drawing_points = []
        self.current_stroke = []
        self.ripples = []

    def pick_color(self):
        color = colorchooser.askcolor(initialcolor="#ff0000")[0]
        if color:
            self.draw_color = (int(color[2]), int(color[1]), int(color[0]))

    def convert_to_frame_coords(self, screen_x, screen_y, crop_x, crop_y, crop_w, crop_h, zoom_active):
        """Convert screen coordinates to final frame coordinates"""
        if not zoom_active:
            # No zoom - just scale to target resolution
            scale_x = self.target_w / self.screen_w
            scale_y = self.target_h / self.screen_h
            return int(screen_x * scale_x), int(screen_y * scale_y)
        else:
            # Zoom active - convert to cropped frame coordinates
            rel_x = screen_x - crop_x
            rel_y = screen_y - crop_y
            
            # Scale to target resolution
            scale_x = self.target_w / crop_w
            scale_y = self.target_h / crop_h
            
            return int(rel_x * scale_x), int(rel_y * scale_y)

    def draw_on_frame(self, frame, crop_x, crop_y, crop_w, crop_h):
        """Draw all annotations on the frame with proper coordinate conversion"""
        
        # Draw completed strokes
        for stroke, color in self.drawing_points:
            for i in range(len(stroke) - 1):
                pt1_screen = stroke[i]
                pt2_screen = stroke[i + 1]
                pt1_frame = self.convert_to_frame_coords(pt1_screen[0], pt1_screen[1], crop_x, crop_y, crop_w, crop_h, self.zoom_active)
                pt2_frame = self.convert_to_frame_coords(pt2_screen[0], pt2_screen[1], crop_x, crop_y, crop_w, crop_h, self.zoom_active)
                cv2.line(frame, pt1_frame, pt2_frame, color, 5, cv2.LINE_AA)
        
        # Draw current stroke
        if self.current_stroke:
            for i in range(len(self.current_stroke) - 1):
                pt1_screen = self.current_stroke[i]
                pt2_screen = self.current_stroke[i + 1]
                pt1_frame = self.convert_to_frame_coords(pt1_screen[0], pt1_screen[1], crop_x, crop_y, crop_w, crop_h, self.zoom_active)
                pt2_frame = self.convert_to_frame_coords(pt2_screen[0], pt2_screen[1], crop_x, crop_y, crop_w, crop_h, self.zoom_active)
                cv2.line(frame, pt1_frame, pt2_frame, self.draw_color, 5, cv2.LINE_AA)
        
        # Draw ripples with animation
        for ripple in self.ripples[:]:
            ripple['frame_count'] += 1
            
            # Animate radius (shrink over time)
            progress = ripple['frame_count'] / 30.0  # 30 frames = 1.5 seconds at 20 FPS
            if progress >= 1.0:
                self.ripples.remove(ripple)
                continue
            
            current_radius = int(ripple['max_radius'] * (1.0 - progress))
            
            # Convert ripple position to frame coordinates
            ripple_frame = self.convert_to_frame_coords(ripple['x'], ripple['y'], crop_x, crop_y, crop_w, crop_h, self.zoom_active)
            
            # Draw multiple concentric circles for better visibility
            for i in range(3):
                radius = max(5, current_radius - i * 8)
                alpha = 1.0 - (i * 0.3)
                color = tuple(int(c * alpha) for c in (255, 255, 255))
                cv2.circle(frame, ripple_frame, radius, color, 3, cv2.LINE_AA)
        
        return frame

    def record_screen(self):
        try:
            timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"tutorial_{timestamp}.mp4"
            
            fourcc = cv2.VideoWriter_fourcc(*"mp4v")
            out = cv2.VideoWriter(filename, fourcc, 20.0, (self.target_w, self.target_h))
            
            with mss.mss() as sct:
                monitor = sct.monitors[1]
                
                while self.is_recording:
                    # 1. Grab full screen
                    img = np.array(sct.grab(monitor))
                    frame = cv2.cvtColor(img, cv2.COLOR_BGRA2BGR)
                    
                    # 2. Calculate crop region for zoom
                    crop_x, crop_y = 0, 0
                    crop_w, crop_h = self.screen_w, self.screen_h
                    
                    if self.zoom_active:
                        # Smooth mouse tracking
                        self.smooth_x += (self.mouse_x - self.smooth_x) * 0.15
                        self.smooth_y += (self.mouse_y - self.smooth_y) * 0.15
                        
                        # 2x zoom
                        zoom_factor = 2.0
                        crop_w = int(self.screen_w / zoom_factor)
                        crop_h = int(self.screen_h / zoom_factor)
                        
                        cx = int(self.smooth_x)
                        cy = int(self.smooth_y)
                        
                        crop_x = max(0, min(cx - crop_w // 2, self.screen_w - crop_w))
                        crop_y = max(0, min(cy - crop_h // 2, self.screen_h - crop_h))
                        
                        # Crop the frame
                        frame = frame[crop_y:crop_y+crop_h, crop_x:crop_x+crop_w]
                    
                    # 3. Draw annotations (with proper coordinate conversion)
                    if self.is_drawing:
                        frame = self.draw_on_frame(frame, crop_x, crop_y, crop_w, crop_h)
                    
                    # 4. Resize to target resolution
                    if self.zoom_active:
                        frame = cv2.resize(frame, (self.target_w, self.target_h), interpolation=cv2.INTER_LINEAR)
                    else:
                        frame = cv2.resize(frame, (self.target_w, self.target_h), interpolation=cv2.INTER_AREA)
                    
                    out.write(frame)
                    time.sleep(0.05)
                
                out.release()
                
        except Exception as e:
            print(f"Recording error: {e}")
            messagebox.showerror("Error", str(e))
        finally:
            self.is_recording = False
            self.stop_mouse_listener()

if __name__ == "__main__":
    root = tk.Tk()
    app = PerfectRecorderApp(root)
    root.mainloop()
