import argparse
import os
import sys

import requests
from dotenv import load_dotenv


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
REPO_ROOT = os.getcwd()
DEFAULT_EXTENSIONS = {
    ".cmd",
    ".cs",
    ".csproj",
    ".json",
    ".md",
    ".py",
    ".resx",
    ".sln",
    ".txt",
    ".xml",
}
SKIP_DIRS = {
    ".git",
    ".idea",
    ".venv",
    "bin",
    "obj",
    "WOTR_Blueprints",
}


def parse_args():
    parser = argparse.ArgumentParser(
        description="Ask Kimi with selected local file context appended to the prompt."
    )
    parser.add_argument(
        "-p",
        "--path",
        action="append",
        default=[],
        help="File or directory to include. May be supplied multiple times.",
    )
    parser.add_argument(
        "--max-file-bytes",
        type=int,
        default=120_000,
        help="Maximum bytes to include from any single file.",
    )
    parser.add_argument(
        "--max-total-bytes",
        type=int,
        default=500_000,
        help="Maximum total bytes to include across all files.",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=int(os.getenv("KIMI_TIMEOUT_SECONDS", "30")),
        help="HTTP timeout for the Kimi request.",
    )
    parser.add_argument(
        "prompt",
        nargs="*",
        help="Prompt text. If omitted, stdin is used.",
    )
    return parser.parse_args()


def iter_files(paths):
    for raw_path in paths:
        path = os.path.abspath(raw_path)
        if os.path.isfile(path):
            yield path
            continue

        if not os.path.isdir(path):
            print(f"Skipping missing path: {raw_path}", file=sys.stderr)
            continue

        for current_root, dirs, files in os.walk(path):
            dirs[:] = [
                name for name in dirs
                if name not in SKIP_DIRS and not name.startswith(".")
            ]
            for name in sorted(files):
                ext = os.path.splitext(name)[1]
                if ext in DEFAULT_EXTENSIONS:
                    yield os.path.join(current_root, name)


def read_file(path, max_bytes):
    with open(path, "rb") as handle:
        data = handle.read(max_bytes + 1)

    truncated = len(data) > max_bytes
    data = data[:max_bytes]
    text = data.decode("utf-8", errors="replace")
    lines = text.splitlines()
    numbered = "\n".join(f"{index + 1}: {line}" for index, line in enumerate(lines))
    rel_path = os.path.relpath(path, REPO_ROOT)
    suffix = "\n[TRUNCATED]\n" if truncated else ""
    return f"--- FILE: {rel_path} ---\n{numbered}{suffix}\n"


def build_context(paths, max_file_bytes, max_total_bytes):
    seen = set()
    chunks = []
    total = 0
    for path in iter_files(paths):
        normalized = os.path.normcase(os.path.abspath(path))
        if normalized in seen:
            continue
        seen.add(normalized)

        chunk = read_file(path, max_file_bytes)
        encoded_size = len(chunk.encode("utf-8"))
        if total + encoded_size > max_total_bytes:
            remaining = max_total_bytes - total
            if remaining <= 0:
                break
            chunk = chunk.encode("utf-8")[:remaining].decode("utf-8", errors="replace")
            chunk += "\n[CONTEXT TRUNCATED]\n"
            chunks.append(chunk)
            break

        chunks.append(chunk)
        total += encoded_size

    return "\n".join(chunks)


def main():
    args = parse_args()
    load_dotenv(os.path.join(ROOT, ".env"))

    api_key = os.getenv("KIMI_API_KEY")
    base_url = os.getenv("KIMI_BASE_URL", "https://api.moonshot.ai/v1").rstrip("/")
    model = os.getenv("KIMI_MODEL", "kimi-k2")

    if not api_key:
        print("Missing KIMI_API_KEY in .codex-tools/.env", file=sys.stderr)
        return 1

    prompt = " ".join(args.prompt).strip()
    if not prompt:
        prompt = sys.stdin.read().strip()

    if not prompt:
        print(
            "Usage: .codex-tools\\ask-kimi-with-files.cmd -p <file-or-dir> \"your prompt\"",
            file=sys.stderr,
        )
        return 1

    context = build_context(args.path, args.max_file_bytes, args.max_total_bytes)
    full_prompt = prompt
    if context:
        full_prompt += (
            "\n\nLocal file context follows. Lines are numbered as '<line>: <text>'. "
            "Use these line numbers when citing findings.\n\n"
            + context
        )

    payload = {
        "model": model,
        "messages": [
            {
                "role": "system",
                "content": (
                    "You are Kimi, a documentation and summarization assistant. "
                    "Be concise. Do not make code architecture decisions."
                ),
            },
            {
                "role": "user",
                "content": full_prompt,
            },
        ],
        "temperature": 1,
    }

    try:
        response = requests.post(
            f"{base_url}/chat/completions",
            headers={
                "Authorization": f"Bearer {api_key}",
                "Content-Type": "application/json",
            },
            json=payload,
            timeout=args.timeout_seconds,
        )
    except requests.Timeout:
        print(
            f"Kimi request timed out after {args.timeout_seconds} seconds. Retry with fewer files or a narrower prompt.",
            file=sys.stderr,
        )
        return 124

    if response.status_code != 200:
        print(f"HTTP {response.status_code}", file=sys.stderr)
        print(response.text, file=sys.stderr)
        return 1

    data = response.json()
    print(data["choices"][0]["message"]["content"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
