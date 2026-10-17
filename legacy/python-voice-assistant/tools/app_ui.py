# tools/app_ui.py
import tkinter as tk
from tkinter import ttk, messagebox
from threading import Thread
from modules.llm_processor import process_with_llm
from modules.text_to_speech import speak
from modules.speech_to_text import SpeechToText
from config import settings
import keyring, re, webbrowser, sys, time

SERVICE_NAME = settings.SERVICE_NAME
GEMINI_HELP_URL = "https://ai.google.dev/gemini-api/docs/quickstart"
GMAIL_APP_PASS_URL = "https://myaccount.google.com/apppasswords"

# ---------------- VALIDATION HELPERS ---------------- #
def validate_email(email):
    return bool(re.match(r"^[\w\.-]+@[\w\.-]+\.\w+$", email))

def validate_gemini_key(key):
    return key.startswith("AIza") and 35 <= len(key) <= 45

def validate_app_password(pwd):
    return len(pwd) == 16 and pwd.isalnum()

def extract_name(email):
    local=email.split('@')[0]
    clean=re.sub(r'[^a-zA-Z\s]',' ',local)
    clean=re.sub(r'\s+',' ',clean)
    name=clean.strip().title()
    return name


# ---------------- MAIN APP UI ---------------- #
class ModernAppUI:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("🎙️ VocalEye Assistant")
        self.root.geometry("900x600")
        self.root.configure(bg="#0f111a")
        self.root.resizable(False, False)
        self.running = False

        # loader state flag (ensure exists before any thread touches it)
        self.loader_active = False
        self.loader_step = 0

        self.setup_styles()
        self.create_layout()

        # redirect stdout/stderr to UI (keeps as-is)
        sys.stdout = self
        sys.stderr = self

    # ------------- Styles ------------- #
    def setup_styles(self):
        style = ttk.Style()
        style.theme_use("clam")
        style.configure("TButton", background="#1f1f2e", foreground="white",
                        font=("Segoe UI", 11, "bold"), borderwidth=0, padding=6)
        style.map("TButton", background=[("active", "#2b2b3c")])
        style.configure("Accent.TButton", background="#0078D7", foreground="white",
                        font=("Segoe UI Semibold", 11), padding=8, borderwidth=0)
        style.map("Accent.TButton", background=[("active", "#1084f0")])

    # ------------- Layout ------------- #
    def create_layout(self):
        sidebar = tk.Frame(self.root, bg="#161823", width=200, height=600)
        sidebar.pack(side="left", fill="y")

        tk.Label(sidebar, text="VocalEye", fg="#00ffcc", bg="#161823",
                 font=("Segoe UI", 20, "bold")).pack(pady=(40, 20))

        ttk.Button(sidebar, text="▶ Start Assistant", style="Accent.TButton",
                   command=self.start_assistant).pack(fill="x", padx=30, pady=10)
        ttk.Button(sidebar, text="⏹ Stop Assistant",
                   command=self.stop_assistant).pack(fill="x", padx=30, pady=10)
        ttk.Button(sidebar, text="⚙ Edit Credentials",
                   command=self.open_settings_modal).pack(fill="x", padx=30, pady=10)
        ttk.Button(sidebar, text="❌ Exit", command=self.root.destroy).pack(fill="x", padx=30, pady=(20, 0))

        main_frame = tk.Frame(self.root, bg="#0f111a")
        main_frame.pack(side="right", fill="both", expand=True)
        t=f" WELCOME {extract_name(keyring.get_password(SERVICE_NAME, "SENDER_EMAIL"))}"
        tk.Label(main_frame, text=t,
                 bg="#0f111a", fg="#00ffcc", font=("Consolas", 15, "bold")).pack(anchor="center", padx=20, pady=(20, 5))

        self.text_area = tk.Text(main_frame, wrap="word", font=("Segoe UI", 11),
                                 bg="#0f111a", fg="#d9e3ea", insertbackground="#00ffcc", relief="flat", bd=0, padx=20, pady=20)
        self.text_area.pack(fill="both", expand=True, padx=20, pady=10)
        self.text_area.configure(state="disabled")


        # Frame inside text_area for placing inline widgets (bubbles, loader)
        
        




        tk.Label(main_frame, text="© 2025 VocalEye | Voice Assistant for Visually Impaired",
                 fg="#5f6f80", bg="#0f111a", font=("Segoe UI", 9)).pack(side="bottom", pady=6)

    # ----------------- VocalEye Chat Renderer ----------------- #
    def add_bubble(self, text, sender="assistant"):
        # This must run on the main thread (use root.after from other threads)
        self.text_area.configure(state="normal")
        self.text_area.insert("end", "\n")
        tag = f"bubble_{sender}_{id(text)}"

        if sender == "assistant":
            self.text_area.insert("end", text + "\n", (tag,))
            self.text_area.tag_config(
                tag,
                background="#131622",
                foreground="#e0fffa",
                lmargin1=20,
                lmargin2=20,
                rmargin=80,
                spacing1=6,
                spacing2=6,
                spacing3=12,
                font=("Segoe UI", 11)
            )
        else:  # user
            self.text_area.insert("end", text + "\n", (tag,))
            self.text_area.tag_config(
                tag,
                background="#1b2330",
                foreground="#cfe8ff",
                justify="right",
                lmargin1=120,
                lmargin2=120,
                rmargin=20,
                spacing1=6,
                spacing2=6,
                spacing3=12,
                font=("Segoe UI", 11)
            )

        self.text_area.see("end")
        self.text_area.configure(state="disabled")

    def write(self, text):
        """Compatibility for print() redirection (keep as-is)"""
        # schedule add_bubble on main thread for safety
        try:
            self.root.after(0, lambda: self.add_bubble(str(text),sender="assistant"))
        except Exception:
            # fallback if mainloop isn't running yet
            pass

    def flush(self):
        pass

    # ----------------- VocalEye Loader ----------------- #
    def start_loader(self,t="●●●"):
        if getattr(self, "loader_active", False):
            return
        
        self.loader_active = True

        # breathing glow color cycle
        self.glow_colors = [
            "#66ffe5", "#33f5d4", "#00e6c5",
            "#00d4b7", "#00c0a5", "#00ad94",
            "#009982", "#008671", "#00735f",
            "#008671", "#009982", "#00ad94",
            "#00c0a5", "#00d4b7", "#00e6c5",
            "#33f5d4"
        ]

        # breathing sizes
        self.glow_sizes = [16,13,10,13,16,13,10,13,16,13,10,13]
                           

        self.loader_step = 0

        # INSERT 3 DOTS ONCE
        self.text_area.configure(state="normal")

        # buffer space to avoid sticking to last message
        self.text_area.insert("end", " ")

        # store start index
        self.loader_start = self.text_area.index("end-1c")

        # insert three dots
        
        self.text_area.insert("end", f"{t}")
        self.loader_end = self.text_area.index("end")

        # apply initial tag for whole loader
        self.text_area.tag_add("loader_tag", self.loader_start, self.loader_end)
        self.text_area.tag_config("loader_tag", foreground="#00ffcc")

        # create per-dot tags
        self.text_area.tag_add("dot1", f"{self.loader_start}", f"{self.loader_start} +1c")
        self.text_area.tag_add("dot2", f"{self.loader_start} +1c", f"{self.loader_start} +2c")
        self.text_area.tag_add("dot3", f"{self.loader_start} +2c", f"{self.loader_start} +3c")

        self.text_area.configure(state="disabled")
        self.text_area.see("end")

        # start animation
        self.animate_loader()


    def animate_loader(self):
        if not self.loader_active:
            return

        # cyclical fade index
        i = self.loader_step

        # each dot breathes at a different offset
        colors = [
            self.glow_colors[i % len(self.glow_colors)],           # dot1
            self.glow_colors[(i + 5) % len(self.glow_colors)],     # dot2
            self.glow_colors[(i + 10) % len(self.glow_colors)],    # dot3
        ]

        sizes = [
            self.glow_sizes[i % len(self.glow_sizes)],             # dot1
            self.glow_sizes[(i + 3) % len(self.glow_sizes)],       # dot2
            self.glow_sizes[(i + 6) % len(self.glow_sizes)],       # dot3
        ]

        self.loader_step += 1

        # update the tags with new glow/fade
        try:
            self.text_area.configure(state="normal")

            # dot1
            self.text_area.tag_config(
                "dot1",
                foreground=colors[0],
                font=("Segoe UI", sizes[0], "bold")
            )
            # dot2
            self.text_area.tag_config(
                "dot2",
                foreground=colors[1],
                font=("Segoe UI", sizes[1], "bold")
            )
            # dot3
            self.text_area.tag_config(
                "dot3",
                foreground=colors[2],
                font=("Segoe UI", sizes[2], "bold")
            )

            self.text_area.configure(state="disabled")
            self.text_area.see("end")

        except:
            pass

        # schedule next frame
        self.root.after(220, self.animate_loader)




    def stop_loader(self):
        self.loader_active = False
        try:
            self.text_area.configure(state="normal")
            self.text_area.delete(self.loader_start, self.loader_end)
            self.text_area.configure(state="disabled")
        except:
            pass


    # ------------- Assistant Functions ------------- #
    def start_assistant(self):
        if self.running:
            # print() is redirected to UI via write()
            return "Already running..."
            
        self.running = True
        text="STARTING VOCALEYE....."
        self.root.after(0, lambda t=text: self.add_bubble(t, sender="assistant"))
        Thread(target=self.run_assistant, daemon=True).start()

    def stop_assistant(self):
        # Stop background loop; ensure UI updates happen on main thread
        self.running = False
        # schedule stopping loader on main thread
        try:
            self.root.after(0, self.stop_loader)
            text="🛑 Assistant stopped."
            self.root.after(0, lambda t=text: self.add_bubble(t, sender="assistant"))
        except Exception:
            pass

    def run_assistant(self):
        listener = SpeechToText()
        # speak is non-UI; safe to call from this thread depending on implementation
        speak("VocalEye is now running. Say something...")
        while self.running:
            self.root.after(0, self.start_loader("||| LISTENING"))
            text = listener.listen()
            if not text:
                continue

            # schedule UI update for user bubble
            try:
                self.root.after(0, self.stop_loader)
                self.root.after(0, lambda t=text: self.add_bubble(t, sender="user"))
            except Exception:
                pass

            text = text.lower().strip()
            if "exit" in text or "stop" in text:
                # schedule stop on main thread
                try:
                    self.root.after(0, self.stop_assistant)
                except Exception:
                    self.stop_assistant()
                speak("Goodbye!")
                break

            # start loader on main thread
            try:
                self.root.after(0, self.start_loader)
            except Exception:
                pass

            # call LLM (blocking) in this worker thread
            try:
                response = process_with_llm(text)
            except Exception as e:
                response = f"Error while processing request: {e}"

            # stop loader on main thread
            try:
                self.root.after(0, self.stop_loader)
            except Exception:
                pass

            # schedule assistant bubble on main thread
            try:
                self.root.after(0, lambda r=response: self.add_bubble(r, sender="assistant"))
            except Exception:
                pass

            # speak the reply if applicable (non-UI)
            if not response.startswith(("file_generator", "generate_os_command", "send_email")):
                try:
                    speak(response)
                except Exception:
                    # don't crash if TTS fails
                    pass

    # ------------- Settings Modal with Overlay ------------- #
    def open_settings_modal(self):
        # Create semi-transparent overlay
        overlay = tk.Toplevel(self.root)
        overlay.geometry("900x600")
        overlay.overrideredirect(True)
        overlay.attributes("-alpha", 0.4)
        overlay.configure(bg="black")

        # Disable clicks on overlay itself
        overlay.lift()
        overlay.grab_set()

        # Centered modal window
        modal = tk.Toplevel(self.root)
        modal.title("⚙️ Edit Credentials")
        modal.geometry("580x420")
        modal.config(bg="#0f111a")
        modal.resizable(False, False)
        modal.transient(self.root)
        modal.lift(overlay)
        modal.grab_set()

        def close_modal():
            modal.destroy()
            overlay.destroy()

        tk.Label(modal, text="🔐 Update Configuration", fg="#00ffcc",
                 bg="#0f111a", font=("Segoe UI", 16, "bold")).pack(pady=(20, 10))

        form = tk.Frame(modal, bg="#0f111a")
        form.pack(pady=10)

        fields = [
            ("Gemini API Key", "AIza... (from Google AI)"),
            ("Sender Email", "example@gmail.com"),
            ("App Password", "16-character Google App Password"),
        ]
        entries = {}
        for i, (label, hint) in enumerate(fields):
            ttk.Label(form, text=label + ":", foreground="white", background="#0f111a").grid(row=i * 2, column=0, sticky="w", pady=(5, 0))
            e = ttk.Entry(form, width=55)
            e.grid(row=i * 2, column=1, pady=(5, 0), padx=10)
            entries[label] = e
            ttk.Label(form, text=hint, foreground="#5f6f80", background="#0f111a",
                      font=("Segoe UI", 9)).grid(row=i * 2 + 1, column=0, columnspan=2, sticky="w", padx=2)

        # Prefill stored creds
        try:
            g = keyring.get_password(SERVICE_NAME, "GEMINI_API_KEY") or ""
            e = keyring.get_password(SERVICE_NAME, "SENDER_EMAIL") or ""
            p = keyring.get_password(SERVICE_NAME, "SENDER_PASSWORD") or ""
            entries["Gemini API Key"].insert(0, g)
            entries["Sender Email"].insert(0, e)
            entries["App Password"].insert(0, p)
        except Exception:
            pass

        # Doc links
        def open_gemini_docs():
            webbrowser.open(GEMINI_HELP_URL)

        def open_app_passwords():
            webbrowser.open(GMAIL_APP_PASS_URL)

        btn_frame = tk.Frame(modal, bg="#0f111a")
        btn_frame.pack(pady=10)
        ttk.Button(btn_frame, text="📘 Get Gemini API Key", command=open_gemini_docs).grid(row=0, column=0, padx=10)
        ttk.Button(btn_frame, text="🔑 Create App Password", command=open_app_passwords).grid(row=0, column=1, padx=10)

        def save_credentials():
            gem = entries["Gemini API Key"].get().strip()
            email = entries["Sender Email"].get().strip()
            pwd = entries["App Password"].get().strip()

            if not gem or not email or not pwd:
                messagebox.showerror("Missing Fields", "Please fill all fields.")
                return
            if not validate_gemini_key(gem):
                messagebox.showerror("Invalid Gemini API Key", "Key must start with 'AIza' and be 35–45 chars.")
                return
            if not validate_email(email):
                messagebox.showerror("Invalid Email", "Please enter a valid email address.")
                return
            if not validate_app_password(pwd):
                messagebox.showerror("Invalid App Password", "Must be exactly 16 characters (letters/numbers).")
                return

            try:
                keyring.set_password(SERVICE_NAME, "GEMINI_API_KEY", gem)
                keyring.set_password(SERVICE_NAME, "SENDER_EMAIL", email)
                keyring.set_password(SERVICE_NAME, "SENDER_PASSWORD", pwd)
                messagebox.showinfo("✅ Saved", "Credentials updated successfully.")
                close_modal()
            except Exception as e:
                messagebox.showerror("Error", f"Failed to save credentials:\n{e}")

        ttk.Button(modal, text="💾 Save & Close", style="Accent.TButton",
                   command=save_credentials).pack(pady=10)
        ttk.Button(modal, text="✖ Cancel", command=close_modal).pack(pady=(0, 10))

        tk.Label(modal, text="Credentials are securely stored in Windows Keyring",
                 bg="#0f111a", fg="#5f6f80", font=("Segoe UI", 9)).pack(side="bottom", pady=10)

        # When modal closed, remove overlay
        modal.protocol("WM_DELETE_WINDOW", close_modal)

# ---------------- RUN APP ---------------- #
def run_app_ui():
    app = ModernAppUI()
    app.root.mainloop()

if __name__ == "__main__":
    run_app_ui()

