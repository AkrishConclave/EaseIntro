"""Exercise the actual HTTP pipeline against a running disposable Compose stack."""
import json
import os
import time
import uuid
import urllib.request
import urllib.error
from datetime import datetime, timedelta, timezone

base = os.environ.get('EASEINTRO_URL', 'http://localhost:8080')

def request(method, path, body=None, token=None, expected=200):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(base + path, data=None if body is None else json.dumps(body).encode(), headers=headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        data = response.read().decode()
        assert response.status == expected, (method, path, response.status, data)
        try:
            return json.loads(data)
        except json.JSONDecodeError:
            return data

for attempt in range(60):
    try:
        request('GET', '/health/ready')
        break
    except (OSError, AssertionError):
        if attempt == 59:
            raise
        time.sleep(2)

suffix = uuid.uuid4().hex
owner = f'owner-{suffix}@example.com'
victim = f'victim-{suffix}@example.com'
password = 'Smoke-Test-Password-123!'
request('POST', '/api/auth/register', {'userEmail': owner, 'password': password})
token = request('POST', '/api/auth/login', {'userEmail': owner, 'password': password})['token']
request('GET', '/api/meets', expected=401)
meet = request('POST', '/api/meets', {
    'title': 'HTTP smoke', 'date': (datetime.now(timezone.utc) + timedelta(minutes=30)).isoformat(),
    'location': 'Test', 'statusId': 5, 'limitMembers': 1, 'allowedPlusOne': False
}, token, expected=201)
uid = meet['uid']
ticket = request('POST', '/api/members', {'name': 'Victim', 'contact': victim, 'meetUid': uid}).split(': ', 1)[1]
preview = request('GET', '/api/members/qrcode/' + ticket)
assert preview['qrValid'] and not preview['isCheckedIn']
request('POST', '/api/auth/register', {'userEmail': victim, 'password': password})
attacker = request('POST', '/api/auth/login', {'userEmail': victim, 'password': password})['token']
assert request('GET', '/api/members/participant/' + victim, token=attacker) == []
request('POST', '/api/members/qrcode/' + ticket + '/check-in', token=attacker, expected=403)
first = request('POST', '/api/members/qrcode/' + ticket + '/check-in', token=token)
assert first['isCheckedIn'] and not first['qrValid']
second = request('POST', '/api/members/qrcode/' + ticket + '/check-in', token=token)
assert second['checkedInAt'] == first['checkedInAt']
request('DELETE', '/api/meets/' + uid, token=token, expected=204)
print('HTTP smoke passed: auth, ownership, registration, QR preview, check-in and replay.')
