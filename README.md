# Adjustable LCDs

## Introduction

This mod adds controls to LCDs allowing you to adjust position and rotation.


Rotors and subgrids are a pain right? I think we can all agree that needing a stack of subgrids, just for your cool bridge or sexy fighter cockpit, is 
both clangy and annoying (no projector subgrids anyone?). Well fear not brave engineer, for do I have the product for you. This ~~reasonably priced~~ mod will 
allow you to relocate your LCDs to your hearts content\*. 

\* Hitboxes do not change. I am not responible for any injury caused due to collisions with invisible blocks


## Features 

Adjust the location of any vanilla and possibly modded LCD. The full list of adjustable
properties is:

- Yaw
- Pitch
- Z offset
- Y offset
- X offset
- Persistence including through saves without the mod installed (spawning a ship without the mod installed then saving it will not wipe the configuration of any existing LCDs)


### Disclaimer 

This mod changes the `LocalMatrix` of the block. This will likely confuse any mods or PB scripts which rely 
on the LCD always being aligned to an axis. I have yet to encounter a mod or PB broken by this but it may happen. 
You have been warned.

### Support for modded blocks

This mod will support any modded block that uses `MyObjectBuilder_TextPanel`. So probably any one you are 
likely to use. If you find one that is not supported, leave a comment and I'll try and add support.

### A note on realism

This mod does not change the hitbox of blocks. However, it will allow the visible part of the block to clip
through other blocks and of course hang suspended apparently unsupported. You can of course choose not to manipulate
the block in such ways as to shatter the illusion but you have been warned.

## Conflicts with PB scripts 

This mod _should not_ conflict with any PB script which uses the MyIni format for the Custom Data. Most popular 
scripts nowadays do support this format but may require additional configuration (Automatic LCDs 2 for example). 
This mod is not an never will be compatable with any scripts which do not use this format. 

## License/Reuploading 

My code in this mod in licensed under the GNU GPLv3. Parts of this mod are not my own work and I 
cannot and do not relicense them. These parts are:

- `Log.cs` (author Digi)

Please contact the respective authors of the above for redistribution rights. As for my code, read the license:

```
    Adjustable LCDs Space Engineers mod
    Copyright (C) 2021 Natasha England-Elbro

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
```
tl;dr Yes you can reupload part or all of it as long as you release all of the source 
including modifcations made, keep all existing license notices, and 
use the GPLv3 license for any modifications.


## Source

The full source code for this mod can be found here: https://github.com/0x00002a/AdjustableLCDs
