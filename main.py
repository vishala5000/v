import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import datetime
import threading
import time
import os
import mss
import cv2
import numpy as np
import platform

class ScreenRecorderApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Quick Screen Recorder")
        self.root.geometry("500x350")
        self.root.configure(bg="#1e1e2f")
        
        tk.Label(root, text="🎥 Quick Screen Recorder", font=('Segoe UI', 20, 'bold'), bg="#1e1e2f", fg="#00d2ff").pack(pady=20)
        
        self.status_label = tk.Label(root, text="Ready to record", bg="#1e1e2f", fg="#aaa", font=('Segoe UI', 12))
        self.status_label.pack(pady=10)
        
        btn_frame = tk.Frame(root, bg="#1e1e2f")
        btn_frame.pack(pady=20)
        
        self.record_btn = tk.Button(btn_frame, text="▶ Start Recording", command=self.toggle_recording, 
                                    bg="#28a745", fg="white", font=('Segoe UI', 12, 'bold'), padx=20, pady=10)
        self.record_btn.pack(side='left', padx=10)
        
        self.info_label = tk.Label(root, text=f"OS: {platform.system()} | Saves to current folder", bg="#1e1e2f", fg="#666", font=('Segoe UI', 9))
        self.info_label.pack(side='bottom', pady=20)
        
        self.is_recording = False
        self.recording_thread = None

    def toggle_recording(self):
        if not self.is_recording:
            self.is_recording = True
            self.record_btn.config(text="⏹ Stop Recording", bg="#dc3545")
            self.status_label.config(text="🔴 Recording in progress...", fg="#ff4b4b")
            self.recording_thread = threading.Thread(target=self.record_screen, daemon=True)
            self.recording_thread.start()
        else:
            self.is_recording = False
            self.record_btn.config(text="▶ Start Recording", bg="#28a745")
            self.status_label.config(text="✅ Recording saved!", fg="#28a745")

    def record_screen(self):
        try:
            timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"recording_{timestamp}.avi"
            
            with mss.mss() as sct:
                monitor = sct.monitors[1]  # Primary monitor
                width = monitor["width"]
                height = monitor["height"]
                
                # Limit resolution for lightweight file size (720p)
                scale = min(1.0, 1280 / width)
                out_width = int(width * scale)
                out_height = int(height * scale)
                
                fourcc = cv2.VideoWriter_fourcc(*"XVID")
                out = cv2.VideoWriter(filename, fourcc, 15.0, (out_width, out_height))
                
                start_time = time.time()
                max_duration = 60  # Max 60 seconds for safety
                
                while self.is_recording and (time.time() - start_time) < max_duration:
                    img = sct.grab(monitor)
                    frame = np.array(img)
                    frame = cv2.cvtColor(frame, cv2.COLOR_BGRA2BGR)
                    frame = cv2.resize(frame, (out_width, out_height))
                    out.write(frame)
                    time.sleep(0.06)  # ~15 FPS
                
                out.release()
                
            if self.is_recording: # If stopped by time limit
                self.root.after(0, lambda: self.status_label.config(text="⏱️ Time limit reached (60s)", fg="#ffc107"))
                self.root.after(0, lambda: self.record_btn.config(text="▶ Start Recording", bg="#28a745"))
            self.is_recording = False
            
        except Exception as e:
            self.root.after(0, lambda: self.status_label.config(text=f"❌ Error: {str(e)}", fg="#dc3545"))
            self.is_recording = False

if __name__ == "__main__":
    root = tk.Tk()
    app = ScreenRecorderApp(root)
    root.mainloop()
