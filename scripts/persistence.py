"""Isolated HTTP request/decision/restart checks with an isolated database."""
from contextlib import contextmanager
from datetime import date, timedelta
import html
import http.cookiejar
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
with socket.socket() as probe:
    probe.bind(("127.0.0.1", 0))
    PORT = probe.getsockname()[1]
BASE = f"http://127.0.0.1:{PORT}"
client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

def request(path, data=None):
    encoded = None if data is None else urllib.parse.urlencode(data).encode()
    try:
        response = client.open(BASE + path, data=encoded, timeout=5)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, response.read().decode()

def post(path, data, form):
    status, page = request(form)
    assert status == 200
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    assert token
    return request(path, {**data, "__RequestVerificationToken": html.unescape(token[1])})

@contextmanager
def running(database):
    env = {**os.environ, "ASPNETCORE_ENVIRONMENT": "Development", "Database__Path": str(database)}
    with tempfile.TemporaryFile() as output:
        process = subprocess.Popen(["dotnet", str(ROOT / "bin/Release/net10.0/LeaveManager.dll"),
                                    "--urls", BASE], cwd=ROOT, env=env, stdout=output, stderr=output)
        try:
            for _ in range(100):
                if process.poll() is not None:
                    output.seek(0)
                    raise RuntimeError(output.read().decode())
                try:
                    if request("/")[0] == 200:
                        break
                except (OSError, urllib.error.URLError):
                    pass
                time.sleep(.1)
            else:
                raise RuntimeError("App did not start")
            yield
        finally:
            process.terminate()
            process.wait(timeout=15)

def login(email):
    global client
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    assert post("/Account/Login", {"Email":email, "Password":"DemoPass123!"}, "/Account/Login")[0] == 200

with tempfile.TemporaryDirectory() as temporary:
    database = Path(temporary) / "leave.db"
    today = date.today()
    day = today
    while day.weekday() >= 5:
        day += timedelta(days=1)
    with running(database):
        login("alice@example.test")
        if day.year == today.year:
            fields = {"Start":day.isoformat(), "End":day.isoformat(), "Reason":"Persistence check", "EmployeeId":"bob"}
            status, page = post("/Leave/Create", fields, "/Leave/Create")
            assert status == 200 and "Persistence check" in page and 'id="available">19<' in page
        else:
            # No eligible weekday remains this year. Seed a historical request to test persistence.
            import sqlite3
            with sqlite3.connect(database) as db:
                db.execute("INSERT INTO LeaveRequests(EmployeeId,EmployeeName,Start,End,Days,Reason) VALUES(?,?,?,?,?,?)",
                           ("alice","Alice",f"{today.year}-01-05",f"{today.year}-01-05",1,"Persistence check"))
    with running(database):
        login("bob@example.test")
        assert "Persistence check" not in request("/")[1], "Employee views remain isolated"
        login("alice@example.test")
        assert "Persistence check" in request("/")[1] and 'id="available">19<' in request("/")[1]
        assert post("/Leave/Decide", {"id":"1", "decision":"approve"}, "/Leave/Create")[0] == 403
        login("manager@example.test")
        assert post("/Leave/Decide", {"id":"1", "decision":"approve"}, "/")[0] == 200
    with running(database):
        login("alice@example.test")
        page = request("/")[1]
        assert "Approved" in page and 'id="approved">1<' in page and 'id="available">19<' in page
        login("manager@example.test")
        assert post("/Leave/Decide", {"id":"1", "decision":"reject"}, "/")[0] == 409
    print("PASS: requests, identity isolation, balances and approval survive process restarts; repeat decision refused")
