#!/usr/bin/env python3
"""Test one selected servo channel, then return it to the closed angle."""

import argparse
import time

from gate_control import set_servo


def main() -> int:
    parser = argparse.ArgumentParser(description="Test one servo channel (0-15).")
    parser.add_argument("channel", type=int, help="Servo channel, 0 through 15")
    args = parser.parse_args()

    set_servo(args.channel, "open")
    time.sleep(1)
    set_servo(args.channel, "close")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
