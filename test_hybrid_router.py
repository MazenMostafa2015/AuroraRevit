import importlib.util
import json
import os
from pathlib import Path

path = Path(__file__).parent / "AuroraRevit.extension" / "RevitTools.tab" / "AIAssistant.panel" / "UtilityTools" / "ai_router.py"
spec = importlib.util.spec_from_file_location("aurora_ai_router_test", str(path))
router = importlib.util.module_from_spec(spec)
spec.loader.exec_module(router)

calls = []
def fake_post(url, payload, timeout=120):
    calls.append(url)
    if url.endswith("/api/revit-query"):
        return {"type": "info", "message": "OpenAI result"}
    return {"message": {"content": json.dumps({"type": "info", "message": "Ollama result"})}}

router._post = fake_post
os.environ["AURORA_AI_PROVIDER"] = "openai"
assert router.query("hello")["message"] == "OpenAI result"
os.environ["AURORA_AI_PROVIDER"] = "ollama"
assert router.query("hello")["message"] == "Ollama result"

os.environ["AURORA_AI_PROVIDER"] = "openai"
original_query = router.query
router.query = lambda prompt: (_ for _ in ()).throw(RuntimeError("primary unavailable"))
result = router.smart_query("hello")
assert result.get("providerFallback") is True
assert result.get("message") == "Ollama result"
router.query = original_query
print("openai_route=PASS")
print("ollama_route=PASS")
print("smart_fallback=PASS")
print("hybrid_router_simulation=PASS")
