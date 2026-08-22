# -*- coding: utf-8 -*-
"""Provider router for Aurora pyRevit tools.

Uses only standard-library HTTP APIs available in IronPython 2.7. Provider
selection order is AURORA_AI_PROVIDER, then Quick Settings JSON, then OpenAI.
"""
from __future__ import print_function

import json
import os

try:
    import urllib2
except ImportError:
    urllib2 = None
try:
    from urllib import request as urllib_request
except ImportError:
    urllib_request = None

DEFAULT_ENDPOINT = "http://localhost:11434"
DEFAULT_OLLAMA_MODEL = "llama3.2"
DEFAULT_OPENAI_PROXY = "http://localhost:5001"


def _settings_path():
    appdata = os.environ.get("APPDATA", "")
    return os.path.join(appdata, "AuroraRevit", "command_tools_settings.json")


def _settings():
    try:
        with open(_settings_path(), "r") as handle:
            value = json.load(handle)
        return value if isinstance(value, dict) else {}
    except Exception:
        return {}


def provider():
    value = os.environ.get("AURORA_AI_PROVIDER", "").strip().lower()
    if value in ["openai", "ollama"]:
        return value
    value = str(_settings().get("provider", "openai")).strip().lower()
    return value if value in ["openai", "ollama"] else "openai"


def ollama_endpoint():
    value = os.environ.get("AURORA_OLLAMA_ENDPOINT", "").strip()
    if not value:
        value = str(_settings().get("ollama_endpoint", DEFAULT_ENDPOINT)).strip()
    value = (value or DEFAULT_ENDPOINT).rstrip("/")
    if value.lower().endswith("/api"):
        value = value[:-4].rstrip("/")
    return value


def ollama_model():
    value = os.environ.get("AURORA_OLLAMA_MODEL", "").strip()
    if value:
        return value
    value = str(_settings().get("ollama_model", "")).strip()
    if value:
        return value
    legacy = str(_settings().get("model", "")).strip()
    return legacy or DEFAULT_OLLAMA_MODEL


def _post(url, payload, timeout=120):
    body = json.dumps(payload).encode("utf-8")
    if urllib2:
        request = urllib2.Request(url, body, {"Content-Type": "application/json"})
        response = urllib2.urlopen(request, timeout=timeout)
    else:
        request = urllib_request.Request(url, body, {"Content-Type": "application/json"})
        response = urllib_request.urlopen(request, timeout=timeout)
    try:
        raw = response.read()
        if not isinstance(raw, str):
            raw = raw.decode("utf-8")
        return json.loads(raw)
    finally:
        try:
            response.close()
        except Exception:
            pass


def _openai(prompt):
    last_error = None
    for base in [DEFAULT_OPENAI_PROXY, "http://localhost:5000"]:
        try:
            return _post(base + "/api/revit-query", {"prompt": prompt})
        except Exception as error:
            last_error = error
    raise RuntimeError("OpenAI proxy unavailable: " + str(last_error))


def _ollama(prompt):
    payload = {
        "model": ollama_model(),
        "stream": False,
        "messages": [
            {"role": "system", "content": "Return exactly one JSON object with type select, schedule, code, or info. Do not add markdown fences."},
            {"role": "user", "content": prompt},
        ],
    }
    result = _post(ollama_endpoint() + "/api/chat", payload)
    if isinstance(result, dict) and result.get("error"):
        raise RuntimeError("Ollama error: " + str(result.get("error")))
    message = result.get("message", {}) if isinstance(result, dict) else {}
    content = message.get("content", "") if isinstance(message, dict) else ""
    if not content:
        return {"type": "info", "message": "Ollama returned an empty response."}
    try:
        parsed = json.loads(content)
        return parsed if isinstance(parsed, dict) else {"type": "info", "message": content}
    except Exception:
        return {"type": "info", "message": content}


def query(prompt):
    """Query the selected provider without silently switching modes."""
    if provider() == "ollama":
        return _ollama(prompt)
    return _openai(prompt)


def smart_query(prompt):
    """Query the selected provider, falling back to the other local provider."""
    selected = provider()
    try:
        return query(prompt)
    except Exception as primary_error:
        try:
            fallback = _ollama(prompt) if selected == "openai" else _openai(prompt)
            if isinstance(fallback, dict):
                fallback["providerFallback"] = True
                fallback["providerNote"] = "Primary provider unavailable; Smart Fallback used the alternate provider."
            return fallback
        except Exception:
            return {"type": "info", "message": "Both Aurora AI providers are unavailable. Start the selected local service and try again. Primary error: " + str(primary_error)}
