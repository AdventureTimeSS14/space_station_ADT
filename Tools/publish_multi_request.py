#!/usr/bin/env python3

import argparse
import requests
import os
import subprocess
import sys
import time
from typing import Iterable

PUBLISH_TOKEN = os.environ["PUBLISH_TOKEN"]
VERSION = os.environ["GITHUB_SHA"]
RELEASE_DIR = "release"

#
# CONFIGURATION PARAMETERS
# Forks should change these to publish to their own infrastructure.
#
ROBUST_CDN_URL = "https://version.adventurestation.ru/" # ADT-Tweak

# ADT-Tweak-Start
MAX_ATTEMPTS = 3
RETRY_DELAY = 30
REQUEST_TIMEOUT = (60, 600)
BYTES_PER_MB = 1024 * 1024


def main():
    sys.stdout.reconfigure(line_buffering=True)

    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id")

    args = parser.parse_args()
    fork_id = args.fork_id or os.getenv("FORK_ID")

    print(f"Starting publish on Robust.Cdn for version {VERSION}")

    finish_attempted = False

    for attempt in range(1, MAX_ATTEMPTS + 1):
        print(f"Publish attempt {attempt}/{MAX_ATTEMPTS}")

        session = create_session()

        try:
            start_publish(session, fork_id)
            finish_attempted = False

            publish_files(session, fork_id)

            finish_attempted = True
            finish_publish(session, fork_id)

            print("SUCCESS!")
            return
        except requests.RequestException as e:
            print(f"Publish attempt {attempt} failed: {e}")

            status = None
            if e.response is not None:
                status = e.response.status_code
                print(f"Response: {e.response.text}")

            if status == 409 and finish_attempted:
                print("SUCCESS! The previous finish request went through, its response was lost.")
                return

            if status is not None and status < 500:
                fail(f"Publish rejected with HTTP {status}: {e.response.text}")

            if attempt == MAX_ATTEMPTS:
                fail(f"Publish failed after {MAX_ATTEMPTS} attempts")

            delay = RETRY_DELAY * attempt
            print(f"Retrying in {delay} seconds...")
            time.sleep(delay)


def fail(message: str):
    print(f"::error::{message}")
    sys.exit(1)


def create_session() -> requests.Session:
    session = requests.Session()
    session.headers = {
        "Authorization": f"Bearer {PUBLISH_TOKEN}",
    }
    return session


def start_publish(session: requests.Session, fork_id: str):
    data = {
        "version": VERSION,
        "engineVersion": get_engine_version(),
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/start", json=data, headers=headers, timeout=REQUEST_TIMEOUT)
    resp.raise_for_status()
    print("Publish successfully started, adding files...")


def publish_files(session: requests.Session, fork_id: str):
    for file in get_files_to_publish():
        size_mb = os.path.getsize(file) / BYTES_PER_MB
        print(f"Publishing {file} ({size_mb:.0f} MB)")
        started = time.monotonic()

        with open(file, "rb") as f:
            headers = {
                "Content-Type": "application/octet-stream",
                "Robust-Cdn-Publish-File": os.path.basename(file),
                "Robust-Cdn-Publish-Version": VERSION
            }
            resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/file", data=f, headers=headers, timeout=REQUEST_TIMEOUT)

        resp.raise_for_status()

        elapsed = time.monotonic() - started
        print(f"Published {file} in {elapsed:.0f} s, {size_mb / max(elapsed, 0.001):.1f} MB/s")

    print("Successfully pushed files, finishing publish...")


def finish_publish(session: requests.Session, fork_id: str):
    data = {
        "version": VERSION
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/finish", json=data, headers=headers, timeout=REQUEST_TIMEOUT)
    resp.raise_for_status()
# ADT-Tweak-End


def get_files_to_publish() -> Iterable[str]:
    for file in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, file)


def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe","--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


if __name__ == '__main__':
    main()
