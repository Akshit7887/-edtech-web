import http.server
import os
import urllib.parse

PORT = 8081
FRONTEND_DIR = os.path.join(os.path.dirname(__file__), "frontend")
API_BASE_URL = os.environ.get("API_BASE_URL", "http://localhost:5000")

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
        
        # Security headers
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "DENY")
        self.send_header("X-XSS-Protection", "1; mode=block")
        self.send_header("Referrer-Policy", "strict-origin-when-cross-origin")
        self.send_header("Permissions-Policy", "camera=(), microphone=(), geolocation=()")
        
        # CSP header - restrictive but allows necessary resources
        csp = (
            "default-src 'self'; "
            "script-src 'self' 'unsafe-inline' https://accounts.google.com https://cdn.jsdelivr.net; "
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; "
            "font-src 'self' https://fonts.gstatic.com; "
            "img-src 'self' data: https:; "
            "connect-src 'self' https://accounts.google.com https://*.googleapis.com; "
            "frame-src https://accounts.google.com; "
            "frame-ancestors 'none'; "
            "base-uri 'self'; "
            "form-action 'self'"
        )
        self.send_header("Content-Security-Policy", csp)
        
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

        # Inject API_BASE_URL into HTML responses
        if self.path.endswith('.html'):
            self.inject_api_base()
        
        return super().do_GET()

    def inject_api_base(self):
        try:
            full_path = os.path.join(FRONTEND_DIR, self.path.lstrip('/'))
            if os.path.isfile(full_path):
                with open(full_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Replace the meta tag content
                if '<meta name="api-base-url" content="">' in content:
                    content = content.replace(
                        '<meta name="api-base-url" content="">',
                        f'<meta name="api-base-url" content="{API_BASE_URL}">'
                    )
                
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.send_header("Content-Length", str(len(content.encode('utf-8'))))
                self.end_headers()
                self.wfile.write(content.encode('utf-8'))
                return True
        except Exception as e:
            print(f"Error injecting API base URL: {e}")
        return False


if __name__ == "__main__":
    os.chdir(FRONTEND_DIR)
    server = http.server.HTTPServer(("0.0.0.0", PORT), CleanURLHandler)
    print(f"Serving EdTech frontend at http://localhost:{PORT}")
    print(f"Clean URLs enabled: /login, /register, /pages/teacher/dashboard, etc.")
    print(f"API Base URL: {API_BASE_URL}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.shutdown()
