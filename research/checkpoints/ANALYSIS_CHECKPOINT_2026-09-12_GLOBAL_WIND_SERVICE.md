# Global wind service closure

Date: 2026-09-12  
Subject: `C:\Stardust-DEV\Stardust.exe`

The global block at `0x008447E0..0x00844B49` is a complete static wind service,
not an unidentified object method:

- default direction is `(1,0,0)`;
- `0x00844AA0` stores and normalizes a direction vector;
- `0x00844A80` stores scalar magnitude;
- `0x00844AD0` returns `normalizedDirection * magnitude`;
- `0x00844860/0x008449D0` add/remove raw callback functions;
- `0x00844B10` publishes the effective vector to every registered callback;
- GroundScene's `ServerWeatherMessage` handler is the sole direct setter caller.

The stable replacement-facing name is therefore `global_wind_set_direction`.
Whether the original C++ namespace was literally `Wind` is archaeological only;
the entire state and notification contract is known.
