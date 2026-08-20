import json
from pathlib import Path

root = Path(__file__).parent / "RevitAddin" / "Examples"
expected = {"Architecture", "Structure", "MEP", "General"}
found = {path.parent.name for path in root.glob("*/examples.json")}
assert found == expected, f"Unexpected discipline folders: {found}"
new_count = 0
all_count = 0
for discipline in sorted(expected):
    path = root / discipline / "examples.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    assert isinstance(data, list) and len(data) >= 20, f"{discipline} must contain the legacy library plus 10 new entries"
    titles = [item.get("title", "").strip() for item in data]
    assert len(titles) == len(set(titles)), f"{discipline} contains duplicate titles"
    discipline_new = 0
    for index, item in enumerate(data, 1):
        assert set(item).issubset({"title", "prompt", "codeTemplate", "version"}), f"{discipline} entry {index} has unknown fields"
        assert item["title"].strip() and item["prompt"].strip(), f"{discipline} entry {index} is empty"
        if item.get("version") == "1.8.3":
            discipline_new += 1
            assert item.get("codeTemplate", "").strip(), f"{discipline} v1.8.3 entry {index} has no runnable code template"
    assert discipline_new == 10, f"{discipline} must contain exactly 10 v1.8.3 additions, found {discipline_new}"
    new_count += discipline_new
    all_count += len(data)
assert new_count == 40, f"Expected exactly 40 v1.8.3 additions, found {new_count}"
assert all_count == 101, f"Expected 101 total embedded prompts, found {all_count}"
print("Validated 4 discipline libraries: 101 total prompts and exactly 40 v1.8.3 educational templates.")
