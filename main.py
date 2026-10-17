import tkinter as tk
from tkinter import colorchooser
import datetime
import threading
import time
import mss
import cv2
import numpy as np
import winsound

class AdvancedRecorderApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Pro Tutorial Recorder")
        self.root.geometry("450x250")
        self.root.configure(bg="#1e1e2f")
        self.root.attributes('-topmost', True)
        
        self.is_recording = False
        self.is_drawing = False
        self.zoom_active = False
        self.draw_color = "#ff0000"
        self.mouse_x, self.mouse_y = 0, 0
        self.smooth_x, self.smooth_y = 0, 0
        self.overlay = None
        self.canvas = None
        
        tk.Label(root, text="🎥 Pro Tutorial Recorder", font=('Segoe UI', 16, 'bold'), bg="#1e1e2f", fg="#00d2ff").pack(pady=10)
        
        self.status_lbl = tk.Label(root, text="Ready | 720p Lightweight Mode", bg="#1e1e2f", fg="#aaa", font=('Segoe UI', 10))
        self.status_lbl.pack(pady=5)
        
        ctrl_frame = tk.Frame(root, bg="#1e1e2f")
        ctrl_frame.pack(pady=10)
        
        self.record_btn = tk.Button(ctrl_frame, text="▶ Start", command=self.toggle_recording, bg="#28a745", fg="white", font=('Segoe UI', 10, 'bold'), width=10)
        self.record_btn.grid(row=0, column=0, padx=5, pady=5)
        
        self.draw_btn = tk.Button(ctrl_frame, text="✏️ Draw: OFF", command=self.toggle_draw, bg="#444", fg="white", font=('Segoe UI', 10, 'bold'), width=10)
        self.draw_btn.grid(row=0, column=1, padx=5, pady=5)
        
        self.zoom_btn = tk.Button(ctrl_frame, text="🔍 Zoom: OFF", command=self.toggle_zoom, bg="#444", fg="white", font=('Segoe UI', 10, 'bold'), width=10)
        self.zoom_btn.grid(row=1, column=0, padx=5, pady=5)
        
        self.clear_btn = tk.Button(ctrl_frame, text="🗑️ Clear All", command=self.clear_drawing, bg="#dc3545", fg="white", font=('Segoe UI', 10, 'bold'), width=10, state='disabled')
        self.clear_btn.grid(row=1, column=1, padx=5, pady=5)
        
        self.color_btn = tk.Button(ctrl_frame, text="🎨 Color", command=self.pick_color, bg="#00d2ff", fg="black", font=('Segoe UI', 10, 'bold'), width=10, state='disabled')
        self.color_btn.grid(row=2, column=0, columnspan=2, padx=5, pady=5)
        
        self.info_lbl = tk.Label(root, text="Tip: Use Draw Mode to annotate. Click for sound/ripple.", bg="#1e1e2f", fg="#666", font=('Segoe UI', 8))
        self.info_lbl.pack(side='bottom', pady=10)

    def toggle_recording(self):
        if not self.is_recording:
            self.is_recording = True
            self.record_btn.config(text="⏹ Stop", bg="#dc3545")
            self.status_lbl.config(text="🔴 Recording 720p...", fg="#ff4b4b")
            threading.Thread(target=self.record_screen, daemon=True).start()
        else:
            self.is_recording = False
            self.record_btn.config(text="▶ Start", bg="#28a745")
            self.status_lbl.config(text="✅ Saved to folder!", fg="#28a745")

    def toggle_draw(self):
        self.is_drawing = not self.is_drawing
        if self.is_drawing:
            self.draw_btn.config(text="✏️ Draw: ON", bg="#00d2ff", fg="black")
            self.clear_btn.config(state='normal')
            self.color_btn.config(state='normal')
            self.create_overlay()
        else:
            self.draw_btn.config(text="✏️ Draw: OFF", bg="#444", fg="white")
            self.clear_btn.config(state='disabled')
            self.color_btn.config(state='disabled')
            if self.overlay:
                self.overlay.destroy()
                self.overlay = None

    def toggle_zoom(self):
        self.zoom_active = not self.zoom_active
        if self.zoom_active:
            self.zoom_btn.config(text="🔍 Zoom: ON", bg="#ffc107", fg="black")
        else:
            self.zoom_btn.config(text="🔍 Zoom: OFF", bg="#444", fg="white")
            self.smooth_x, self.smooth_y = 0, 0

    def create_overlay(self):
        self.overlay = tk.Toplevel(self.root)
        self.overlay.title("Draw Overlay")
        self.overlay.attributes('-transparentcolor', 'black')
        self.overlay.attributes('-topmost', True)
        self.overlay.overrideredirect(True)
        
        with mss.mss() as sct:
            mon = sct.monitors[1]
            self.overlay.geometry(f"{mon['width']}x{mon['height']}+0+0")
        
        self.canvas = tk.Canvas(self.overlay, bg='black', highlightthickness=0, cursor='crosshair')
        self.canvas.pack(fill='both', expand=True)
        
        self.canvas.bind('<B1-Motion>', self.on_draw)
        self.canvas.bind('<Button-1>', self.on_click)
        self.canvas.bind('<Motion>', self.track_mouse)

    def track_mouse(self, event):
        self.mouse_x = event.x
        self.mouse_y = event.y

    def on_draw(self, event):
        if not self.is_drawing: return
        self.canvas.create_line(self.last_x, self.last_y, event.x, event.y, fill=self.draw_color, width=4, capstyle='round')
        self.last_x, self.last_y = event.x, event.y

    def on_click(self, event):
        if not self.is_drawing: return
        self.last_x, self.last_y = event.x, event.y
        try:
            winsound.Beep(1200, 60)
        except:
            pass
        self.animate_ripple(event.x, event.y, 20)

    def animate_ripple(self, x, y, radius):
        if not self.is_drawing or radius <= 0:
            return
        self.canvas.create_oval(x-radius, y-radius, x+radius, y+radius, outline=self.draw_color, width=3)
        self.canvas.after(30, self.animate_ripple, x, y, radius-3)

    def clear_drawing(self):
        if self.canvas:
            self.canvas.delete("all")

    def pick_color(self):
        color = colorchooser.askcolor(initialcolor=self.draw_color)[1]
        if color:
            self.draw_color = color

    def record_screen(self):
        try:
            timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"tutorial_{timestamp}.mp4"
            
            target_w, target_h = 1280, 720
            fourcc = cv2.VideoWriter_fourcc(*"mp4v")
            out = cv2.VideoWriter(filename, fourcc, 20.0, (target_w, target_h))
            
            with mss.mss() as sct:
                monitor = sct.monitors[1]
                
                while self.is_recording:
                    img = np.array(sct.grab(monitor))
                    frame = cv2.cvtColor(img, cv2.COLOR_BGRA2BGR)
                    
                    if self.zoom_active and self.is_drawing:
                        if self.smooth_x == 0:
                            self.smooth_x, self.smooth_y = self.mouse_x, self.mouse_y
                        else:
                            self.smooth_x += (self.mouse_x - self.smooth_x) * 0.1
                            self.smooth_y += (self.mouse_y - self.smooth_y) * 0.1
                        
                        zoom_factor = 1.5
                        crop_w = int(target_w / zoom_factor)
                        crop_h = int(target_h / zoom_factor)
                        
                        scale_x = monitor['width'] / target_w
                        scale_y = monitor['height'] / target_h
                        
                        cx = int(self.smooth_x / scale_x)
                        cy = int(self.smooth_y / scale_y)
                        
                        x1 = max(0, min(cx - crop_w//2, monitor['width'] - crop_w))
                        y1 = max(0, min(cy - crop_h//2, monitor['height'] - crop_h))
                        
                        cropped = frame[y1:y1+crop_h, x1:x1+crop_w]
                        frame = cv2.resize(cropped, (target_w, target_h), interpolation=cv2.INTER_LINEAR)
                    else:
                        frame = cv2.resize(frame, (target_w, target_h), interpolation=cv2.INTER_AREA)
                    
                    out.write(frame)
                    time.sleep(0.05)
                
                out.release()
                
        except Exception as e:
            print(f"Recording error: {e}")
        finally:
            self.is_recording = False

if __name__ == "__main__":
    root = tk.Tk()
    app = AdvancedRecorderApp(root)
    root.mainloop()
