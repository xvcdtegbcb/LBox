#!/usr/bin/env python3
"""Send a Blender Python snippet to the BlenderMCP addon socket (127.0.0.1:9876).

Usage:
  python blender_mcp_send.py "import bpy; print(bpy.app.version_string)"
  python blender_mcp_send.py --file script.py
  python blender_mcp_send.py --port 9876 "print('hi')"

Prints the raw JSON response from Blender on stdout (LLM-friendly).
Exit code 0 on success, 1 on connection/parse error.
"""
import argparse
import json
import socket
import sys


def send_code(code: str, host: str = "127.0.0.1", port: int = 9876) -> str:
    """Send execute_code command, return Blender's JSON response string."""
    payload = json.dumps({"type": "execute_code", "params": {"code": code}})
    with socket.create_connection((host, port), timeout=15.0) as sock:
        sock.sendall(payload.encode("utf-8"))
        chunks = []
        while True:
            chunk = sock.recv(8192)
            if not chunk:
                break
            chunks.append(chunk)
            try:
                json.loads(b"".join(chunks).decode("utf-8"))
                break  # complete JSON received
            except json.JSONDecodeError:
                continue
    return b"".join(chunks).decode("utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Send Blender Python via BlenderMCP socket")
    parser.add_argument("code", nargs="?", help="Inline Blender Python code")
    parser.add_argument("--file", help="Read Blender Python code from a file")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=9876)
    args = parser.parse_args()

    if args.file:
        with open(args.file, "r", encoding="utf-8") as f:
            code = f.read()
    elif args.code:
        code = args.code
    else:
        print("Error: provide inline code or --file", file=sys.stderr)
        return 1

    try:
        response = send_code(code, args.host, args.port)
        parsed = json.loads(response)
        if parsed.get("status") == "error":
            print(f"Blender error: {parsed.get('message')}", file=sys.stderr)
            return 1
        print(response)  # raw JSON, includes result.result (stdout)
        return 0
    except (ConnectionError, OSError, json.JSONDecodeError) as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
