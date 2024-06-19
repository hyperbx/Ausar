# Ausar
A trainer for Halo 5: Forge for adjusting graphics settings.

# Features
- Adjustable FPS limit.
- Adjustable field of view.
- Dynamic aspect ratio (adjusts for any non-16:9 resolution on the fly).
- Draw distance scaling and performance options.
- Various toggles for frontend and camera visuals.

# Prerequisites
### Building
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Running
- .NET 8.0 Desktop Runtime ([x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x86-installer), [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.6-windows-x64-installer))
- [Halo 5: Forge](https://www.microsoft.com/store/productId/9NBLGGH4V0FR?ocid=pdpshare) (1.194.6192.2)

# Issues
Please report any unknown issues on the issue tracker [here](https://github.com/hyperbx/Ausar/issues).

### Dynamic Aspect Ratio
- Resizing the game window too many times will cause UI elements and the font renderer to start artefacting until they eventually disappear (which is why being at the main menu is a requirement for now).
- UI elements rendered in 3D space (such as navigation points) are still drawn at 16:9 and may appear stretched at non-16:9 resolutions.
- Ludicrously wide aspect ratios may have graphical errors with certain buffers being drawn at the incorrect offset.

# Motivation
*tl;dr - I wanted to simplify graphics tweaks down to a single application for a friend group.*

Halo 5: Forge is undoubtedly a very unpopulated game, so putting effort into something like this might seem a bit strange.

I recently added it to the roster of Halo games that I play online with a group. I use a 144Hz ultrawide display, everyone else also has high refresh rate displays.

There were tools for ultrawide support made a long time ago, but they were for an older version of the game and used hard-coded addresses, so they no longer worked.

One of these tools was [H5Tweak](https://github.com/Snaacky/h5tweak), which supported version 1.114.4592.2 of the game, and that build [just so happened to have been uploaded to BetaArchive](https://www.betaarchive.com/database/view_release.php?uuid=52ee8305-fbbe-44b8-9e07-f14273137934). By cross-analysing between that version's binary and the latest, I was able to find the data that tool was editing, and from there I got to work.

Upon finishing a working prototype, I got a bit frustrated that I had to launch the game with three different applications running in the background just to fix up the graphics; [Exuberant](https://www.youtube.com/watch?v=1XlriRF5ogA), Halo5Fix (for adjustable FPS) and my own tool. This was also a bit user unfriendly to the group I wanted to play with, so I wanted to simplify things down to a single application.

At this point, I decompiled Exuberant and found the addresses for mainly the graphics related options (for transparency, these will be listed at the bottom of the README) and implemented them here, as well as again cross-analysing with H5Tweak to figure out how its FPS hack worked.

This tool is by no means a replacement for Exuberant, that tool has its own subset of features that aren't relevant to the ones I needed. It may conflict with runtime patching though, if the FOV and options under Performance Settings don't match between both applications.

# Credits
- Hyper - programming, research
- [Gamecheat13](https://www.youtube.com/@gamecheat13) - research
- [Snaacky](https://github.com/Snaacky) - research
- [no1dead](https://github.com/no1dead) - research
- Xbox7887 - research

<details><summary><h3>Research Attribution</h3></summary>

Feature|Origin
--------|------
FPS|H5Tweak
FOV|Exuberant
High FOV Fix|Exuberant
Apply Custom FOV to Vehicles|Exuberant
Dynamic Aspect Ratio|Ausar, H5Tweak
Resolution Scale|Ausar
General Draw Distance Scalar|Exuberant
Object Detail Scalar|Exuberant
BSP Geometry Draw Distance Scalar|Exuberant
Effect Draw Distance Scalar|Exuberant
Particle Draw Distance Scalar|Exuberant
Decorator Draw Distance Scalar|Exuberant
Toggle Fog|Exuberant
Toggle Weather|Exuberant
Toggle Frontend|Ausar
Toggle Navigation Points|Ausar
Toggle Ragdolls|Exuberant
Toggle Smaller Crosshair Scale|Ausar
Toggle Third Person Camera|Exuberant
Toggle World Space View Model|Ausar

</details>
