import os
import sys
import requests
from dotenv import load_dotenv

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
load_dotenv(os.path.join(ROOT, ".env"))

api_key = os.getenv("KIMI_API_KEY")
base_url = os.getenv("KIMI_BASE_URL", "https://api.moonshot.ai/v1").rstrip("/")
model = os.getenv("KIMI_MODEL", "kimi-k2")

if not api_key:
    print("Missing KIMI_API_KEY in .codex-tools/.env", file=sys.stderr)
    sys.exit(1)

prompt = " ".join(sys.argv[1:]).strip()
if not prompt:
    prompt = sys.stdin.read().strip()

if not prompt:
    print("Usage: python .codex-tools/scripts/ask-kimi.py \"your prompt\"", file=sys.stderr)
    sys.exit(1)

payload = {
    "model": model,
    "messages": [
        {
            "role": "system",
            "content": "You are Kimi, a documentation and summarization assistant. Be concise. Do not make code architecture decisions."
        },
        {
            "role": "user",
            "content": prompt
        }
    ],
    "temperature": 1
}

response = requests.post(
    f"{base_url}/chat/completions",
    headers={
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    },
    json=payload,
    timeout=120
)

if response.status_code != 200:
    print(f"HTTP {response.status_code}", file=sys.stderr)
    print(response.text, file=sys.stderr)
    sys.exit(1)

data = response.json()
print(data["choices"][0]["message"]["content"])