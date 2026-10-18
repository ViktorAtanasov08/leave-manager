"""Run HTTP authentication and permission checks against an isolated demo process."""
import html
import http.cookiejar
import os
from pathlib import Path
import re
import socket
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
with socket.socket() as port_probe:
    port_probe.bind(("127.0.0.1", 0))
    PORT = port_probe.getsockname()[1]
BASE = f"http://127.0.0.1:{PORT}"


class Browser:
    def __init__(self):
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def request(self, path, data=None):
        encoded = None if data is None else urllib.parse.urlencode(data).encode()
        try:
            response = self.opener.open(BASE + path, data=encoded, timeout=5)
        except urllib.error.HTTPError as error:
            response = error
        return response.status, response.read().decode(), response.url

    def post(self, path, data, form_page):
        status, page, _ = self.request(form_page)
        assert status == 200, (status, page)
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
        assert match, "Missing anti-forgery field"
        return self.request(path, {**data, "__RequestVerificationToken": html.unescape(match[1])})

    def login(self, email):
        status, page, url = self.post("/Account/Login", {
            "Email": email, "Password": "DemoPass123!"}, "/Account/Login")
        assert status == 200 and "/Account/Login" not in url and "Your time" in page or (
            status == 200 and "A clear view of the team" in page)


def check(condition, label):
    if not condition:
        raise AssertionError(label)
    print("PASS:", label)


environment = {**os.environ, "ASPNETCORE_ENVIRONMENT": "Development"}
with subprocess.Popen(["dotnet", str(ROOT / "bin/Release/net10.0/LeaveManager.dll"),
                       "--urls", BASE], cwd=ROOT, env=environment,
                      stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) as process:
    try:
        for attempt in range(100):
            try:
                Browser().request("/Account/Login")
                break
            except (OSError, urllib.error.URLError):
                if process.poll() is not None:
                    raise RuntimeError("Demo process exited before becoming ready")
                time.sleep(0.1)
        else:
            raise RuntimeError("Demo process did not become ready")

        anonymous = Browser()
        status, page, url = anonymous.request("/")
        check(status == 200 and "/Account/Login" in url, "Anonymous users are sent to sign in")
        status, page, _ = anonymous.post("/Account/Login", {
            "Email": "alice@example.test", "Password": "incorrect"}, "/Account/Login")
        check(status == 200 and "Email or password is incorrect" in page, "Incorrect credentials refused")
        alice = Browser()
        alice.login("alice@example.test")
        status, page, _ = alice.request("/")
        check('id="available">20<' in page, "Employee starts with 20 days")
        check(alice.request("/site.css")[0] == 200, "Stylesheet is served")
        check(alice.request("/Leave/Create", {"Reason": "Missing token"})[0] == 400,
              "POST without anti-forgery token is rejected")
        status, page, _ = alice.post("/Leave/Create", {
            "Reason": "Missing dates", "EmployeeId": "bob"}, "/Leave/Create")
        check(status == 200 and "field is required" in page, "Missing dates fail server validation")
        status, _, _ = alice.post("/Leave/Decide", {"id": "1", "decision": "approve"}, "/Leave/Create")
        check(status == 403, "Employee cannot call manager decision endpoint")
        manager = Browser()
        manager.login("manager@example.test")
        check(manager.request("/Leave/Create")[0] == 403, "Manager cannot impersonate an employee")
        check(manager.request("/Leave/Decide", {"id": "1", "decision": "approve"})[0] == 400,
              "Manager POST also requires anti-forgery token")
        status, _, _ = manager.post("/Leave/Decide", {"id": "999", "decision": "approve"}, "/")
        check(status == 409, "Unknown request cannot be approved")
        alice.post("/Account/Logout", {}, "/")
        check("/Account/Login" in alice.request("/")[2], "Signing out removes session access")
        print("HTTP authentication and permission checks passed.")
    finally:
        process.terminate()
        process.wait(timeout=15)
