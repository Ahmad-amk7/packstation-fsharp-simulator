#!/usr/bin/env python3
"""Set one servo channel on an Adafruit 16-channel PWM Servo HAT/driver."""

import argparse
import sys

from adafruit_servokit import ServoKit

CHANNEL_COUNT = 16
OPEN_ANGLE = 90
CLOSED_ANGLE = 0


def set_servo(channel: int, action: str) -> None:
    # EN: Map each compartment to one PCA9685 channel. / DE: Jedes Fach wird einem PCA9685-Kanal zugeordnet. / AR: اربط كل خانة بقناة PCA9685 واحدة.
    if not 0 <= channel < CHANNEL_COUNT:
        raise ValueError(f"Channel must be between 0 and {CHANNEL_COUNT - 1}.")

    # EN: Keep the angles configurable for the mechanical gate. / DE: Die Winkel lassen sich an die Mechanik anpassen. / AR: يمكن تعديل الزوايا لتناسب آلية البوابة.
    angle = OPEN_ANGLE if action == "open" else CLOSED_ANGLE
    kit = ServoKit(channels=CHANNEL_COUNT)
    kit.servo[channel].angle = angle


def main() -> int:
    parser = argparse.ArgumentParser(description="Open or close one Packstation servo gate.")
    parser.add_argument("channel", type=int, help="Servo channel, 0 through 15")
    parser.add_argument("action", choices=("open", "close"), help="Gate action")
    args = parser.parse_args()

    try:
        set_servo(args.channel, args.action)
    except (ValueError, OSError) as exc:
        print(f"Servo command failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
