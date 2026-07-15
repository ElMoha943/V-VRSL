## V-VRSL

This is my personal fork of [VRSL](https://github.com/AcChosen/VR-Stage-Lighting) which is mainly focused on removing features I dont use and improving the ones I do. This is a work in progress and **will probably not fit your own needs**, it is provided as-is and without any warranty.

I code in C# so some shader code changes are vibe coded, you have been warned.

### Removed stuff

- Legacy mode compatibility and legacy prefabs.
- URP/HDRP and non VRChat support.
- Some docs, examples and misc scripts.

### Improvements

- Fixed DMX Materials inspector.
- Added DMX Fixture out of universe bounds warning.
- Fixture editor now only shows properties in use by the fixture type.
- Unlimited mesh amount support and null checks.
- Optimization improvements both on editor and runtime.
- Version is now get directly from package.
- Implemented Smoothing tweak by HappyRobot.
- Added more gobos.
