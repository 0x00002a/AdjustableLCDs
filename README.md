# Adjustable LCDs

## Introduction

This mod adds controls to LCDs allowing you to adjust position and rotation.


Rotors and subgrids are a pain right? I think we can all agree that needing a
stack of subgrids, just for your cool bridge or sexy fighter cockpit, is both
clangy and annoying (no projector subgrids anyone?). Well fear not brave
engineer, for do I have the product for you. This ~~reasonably priced~~ mod will
allow you to relocate your LCDs to your hearts content\*.

\* Hitboxes do not change. I am not responsible for any injury caused due to
collisions with invisible blocks


## Features

Adjust the location of any vanilla and possibly modded LCD. The full list of
adjustable properties is:

- Yaw
- Pitch
- Roll
- Z offset
- Y offset
- X offset

Other features:

- Persistence including through saves without the mod installed (spawning a ship
  without the mod installed then saving it will not wipe the configuration of
  any existing LCDs)


### Disclaimer

This mod changes the `LocalMatrix` of the block. This will likely confuse any
mods or PB scripts which rely on the LCD always being aligned to an axis. I have
yet to encounter a mod or PB broken by this but it may happen.  You have been
warned.

### Support for modded blocks

This mod will support any modded block that uses `MyObjectBuilder_TextPanel`. So
probably any one you are likely to use. If you find one that is not supported,
leave a comment and I'll try and add support.

### A note on realism

This mod does not change the hitbox of blocks. However, it will allow the
visible part of the block to clip through other blocks and of course hang
suspended apparently unsupported. You can of course choose not to manipulate the
block in such ways as to shatter the illusion but you have been warned.

## Conflicts with PB scripts 

This mod _should not_ conflict with any PB script which uses the MyIni format
for the Custom Data. Most popular scripts nowadays do support this format but
may require additional configuration (Automatic LCDs 2 for example).  This mod
is not an never will be compatible with any scripts which do not use this
format. 

Note that in the case of Automatic LCDs 2, you _must_ set up the terminator in
the Custom Data, else you'll get a bunch of errors. Check out the "Compatibility
with other scripts" section
[here](https://steamcommunity.com/sharedfiles/filedetails/?id=407158161)


### Known conflicts with other mods

- [Fast Artificial Horizon 30 FPS!](https://steamcommunity.com/workshop/filedetails/?id=2217821984)

## License/Reuploading 

Parts of this mod are not my own work and I cannot and do not relicense them.
These parts are:

- `Log.cs` (author Digi)

Please contact the respective authors of the above for redistribution rights. As
for my code:

This mod is GPLv3. That means you can reupload it or any mod that contains it
_as long as_ you:

- Keep all existing license notices intact
- Credit me
- List your changes (easiest way is with git and github repo)
- Make _all_ the source code of the relevant mod available freely and publicly
  with no restrictions placed on its access
- Make your mod GPLv3 as well
- Give me your first born child

(ok that last one isn't actually legally binding)

If in doubt, ask me in comments or the Keen discord (\@Natomic).  Full license
is available
[here](https://github.com/0x00002a/AdjustableLCDs/blob/850d5e4b9309e719b4001ae6f54e7a800ece34c4/LICENSE).
I reserve the right to ask for your mod to be yeeted if you have reused my mod
without obeying the license.


## Source

The full source code for this mod can be found here:
https://github.com/0x00002a/AdjustableLCDs
