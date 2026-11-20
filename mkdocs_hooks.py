"""Expose the authored guide alongside Graphify's generated MkDocs wiki."""

from pathlib import Path
import re
from urllib.parse import quote, unquote


ROOT = Path(__file__).resolve().parent
WIKI = ROOT / "graphify-out" / "wiki"
GUIDE = ROOT / "doc" / "architecture-and-developer-guide.md"
REPORT = ROOT / "graphify-out" / "GRAPH_REPORT.md"
GUIDE_PAGE = WIKI / "Architecture_and_Developer_Guide.md"
REPORT_PAGE = WIKI / "Graphify_Report.md"
SOURCE_BASE = "https://github.com/ThinBuddha/RuleEngine/blob/main/"
LINK = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")


def _write_if_changed(path: Path, content: str) -> None:
    if not path.exists() or path.read_text(encoding="utf-8") != content:
        path.write_text(content, encoding="utf-8")


def _site_link(match: re.Match[str]) -> str:
    label, destination = match.groups()
    path, separator, fragment = destination.partition("#")
    if not path or "://" in path:
        return match.group(0)

    target = (GUIDE.parent / unquote(path)).resolve()
    relative = target.relative_to(ROOT)
    if target == REPORT:
        url = REPORT_PAGE.name
    else:
        url = SOURCE_BASE + quote(relative.as_posix(), safe="/")
    if separator:
        url += "#" + fragment
    return f"[{label}]({url})"


def on_pre_build(config, **kwargs):
    WIKI.mkdir(parents=True, exist_ok=True)
    guide = GUIDE.read_text(encoding="utf-8")
    _write_if_changed(GUIDE_PAGE, LINK.sub(_site_link, guide))
    _write_if_changed(REPORT_PAGE, REPORT.read_text(encoding="utf-8"))


def on_config(config, **kwargs):
    pages = sorted(
        (path for path in WIKI.glob("*.md")
         if path.name not in {"index.md", GUIDE_PAGE.name, REPORT_PAGE.name}),
        key=lambda path: path.stem.casefold(),
    )
    config["nav"] = [
        {"Start": "index.md"},
        {"Architecture and developer guide": GUIDE_PAGE.name},
        {"Graphify wiki": [{path.stem.replace("_", " "): path.name} for path in pages]},
        {"Graphify report": REPORT_PAGE.name},
    ]
    return config
