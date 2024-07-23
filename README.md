<p align="center">
    <img src="https://raw.githubusercontent.com/hyperbx/Ausar/main/Ausar/Resources/Images/Icon.png"
         width="128"/>
</p>

<h1 align="center">Ausar</h1>

<p align="center">A trainer for Halo 5: Forge for configuring graphics and various gameplay features.</p>

# Features
- Adjustable FPS limit.
- Adjustable FOV with crosshair scaling and view model correction.
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

### FPS
- Certain weapons may fire at a faster rate at higher frame rates.
- Thruster Pack may boost with improper trajectory on slopes at higher frame rates.

### Dynamic Aspect Ratio
- Resizing the game window too many times will cause UI elements and the font renderer to start artefacting until they eventually disappear (which is why being at the main menu is a requirement for now).
- UI elements rendered in 3D space (such as navigation points) are still drawn at 16:9 and may appear stretched at non-16:9 resolutions.
- Ludicrously wide aspect ratios may have graphical errors with certain buffers being drawn at the incorrect offset.

# Credits
- Hyper - programming, research
- [Gamecheat13](https://www.youtube.com/@gamecheat13) - research
- [Snaacky](https://github.com/Snaacky) - research
- [no1dead](https://github.com/no1dead) - research
- Xbox7887 - research
