import json
from pathlib import Path

root = Path(__file__).parent / "RevitAddin" / "Examples"
expected = {"Architecture", "Structure", "MEP", "General"}
found = {path.parent.name for path in root.glob("*/examples.json")}
assert found == expected, f"Unexpected discipline folders: {found}"
for discipline in sorted(expected):
    path = root / discipline / "examples.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    assert isinstance(data, list) and len(data) == 10, f"{discipline} must contain exactly 10 entries"
    for index, item in enumerate(data, 1):
        assert set(item) == {"title", "prompt"}, f"{discipline} entry {index} has wrong fields"
        assert item["title"].strip() and item["prompt"].strip(), f"{discipline} entry {index} is empty"
print("Validated 4 discipline libraries with 10 entries each: 40 total prompts.")
