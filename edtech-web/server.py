import http.server
import os
import urllib.parse

PORT = 8081
FRONTEND_DIR = os.path.join(os.path.dirname(__file__), "frontend")

class CleanURLHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=FRONTEND_DIR, **kwargs)

    def translate_path(self, path):
        parsed = urllib.parse.urlparse(path)
        clean_path = parsed.path.rstrip("/") or "/"

        # Map /login -> /login.html, /register -> /register.html, etc.
        base = os.path.join(FRONTEND_DIR, clean_path.lstrip("/"))

        if os.path.isfile(base):
            return base

        if os.path.isfile(base + ".html"):
            return base + ".html"

        # For /pages/teacher/dashboard -> /pages/teacher/dashboard.html, etc.
        return super().translate_path(path)

    def end_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Cache-Control", "no-cache, no-store, must-revalidate")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(204)
        self.end_headers()

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        clean_path = parsed.path.rstrip("/") or "/"
        base = os.path.join(FRONTEND_DIR, clean_path.lstrip("/"))

        if os.path.isfile(base):
            self.path = os.path.relpath(base, FRONTEND_DIR).replace("\\", "/")
        elif os.path.isfile(base + ".html"):
            self.path = os.path.relpath(base + ".html", FRONTEND_DIR).replace("\\", "/")

        return super().do_GET()


if __name__ == "__main__":
    os.chdir(FRONTEND_DIR)
    server = http.server.HTTPServer(("0.0.0.0", PORT), CleanURLHandler)
    print(f"Serving EdTech frontend at http://localhost:{PORT}")
    print(f"Clean URLs enabled: /login, /register, /pages/teacher/dashboard, etc.")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.shutdown()
