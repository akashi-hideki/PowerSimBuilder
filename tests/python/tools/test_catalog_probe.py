"""Tests for tools/catalog_probe.py."""

import sys
import urllib.error
import urllib.request
from pathlib import Path

import pytest

TOOLS = Path(__file__).resolve().parents[3] / "tools"
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

import catalog_probe  # noqa: E402


class _FakeResp:
    def __init__(self, status, body):
        self.status = status
        self._body = body

    def __enter__(self):
        return self

    def __exit__(self, *a):
        return False

    def read(self):
        return self._body


def test_http_get_returns_status_and_body(monkeypatch):
    def fake_urlopen(req, timeout):
        return _FakeResp(200, b'{"models": [1,2,3]}')

    monkeypatch.setattr(urllib.request, "urlopen", fake_urlopen)
    status, body = catalog_probe.http_get("http://x", 1.0)
    assert status == 200
    assert b"models" in body


def test_count_json_models_ok():
    assert catalog_probe.count_json_models(b'{"models": [1,2,3]}') == 3
    assert catalog_probe.count_json_models(b'{"models": []}') == 0


def test_count_json_models_invalid():
    with pytest.raises(ValueError):
        catalog_probe.count_json_models(b'{"nope": []}')


def test_resolve_github_api():
    api = catalog_probe._resolve_github_api("https://github.com/modelica/Reference-FMUs")
    assert api == "https://api.github.com/repos/modelica/Reference-FMUs/releases"


def test_resolve_github_api_non_github():
    assert catalog_probe._resolve_github_api("https://example.com/x") is None


def test_probe_one_network_error(monkeypatch):
    def fake_urlopen(req, timeout):
        raise urllib.error.URLError("boom")

    monkeypatch.setattr(urllib.request, "urlopen", fake_urlopen)
    r = catalog_probe.probe_one({"id": "t", "url": "http://x", "kind": "json"}, 0.1)
    assert r["ok"] is False
    assert "network" in r["error"]


def test_probe_one_json_ok(monkeypatch):
    def fake_urlopen(req, timeout):
        return _FakeResp(200, b'{"models": [{"name":"A"}]}')

    monkeypatch.setattr(urllib.request, "urlopen", fake_urlopen)
    r = catalog_probe.probe_one({"id": "t", "url": "http://x", "kind": "json"}, 1.0)
    assert r["ok"] is True
    assert r["status"] == 200
    assert r["models"] == 1
