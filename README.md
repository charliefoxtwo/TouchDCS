# Getting Started
### What you'll need:
1. DCS-BIOS ([Original](https://github.com/dcs-bios/dcs-bios) or [Flightpanels fork](https://github.com/DCSFlightpanels/dcs-bios))
2. [TouchOSC](https://hexler.net/products/touchosc) on your phone/tablet
    - editor recommended as well for creating custom layouts
    - at this time, TouchOSC is $5 on the Google Play store and I have no affiliation with the app or its creators
3. [.NET 5 **Runtime**](https://dotnet.microsoft.com/download/dotnet/5.0) (SDK/AspNetCore Runtimes not required)

### Download the latest version of TouchDCS
You can get the latest version in the [releases](https://github.com/charliefoxtwo/TouchDCS/releases) section. Download both `TouchDcs.exe` as well as `config.json`, and keep them together. For future releases (unless otherwise specified) you shouldn't need to download `config.json` again.

### Configure TouchDCS to talk to your device
TouchDCS sends and receives commands from your device with TouchOSC installed. Open up the `config.json` file that came with your TouchDCS download, and edit the settings under `osc` -> `endpoint` to match the settings you see in TouchOSC -> Settings -> OSC. You'll also need to add the IP address of the machine running TouchDCS to TouchOSC.

![screenshot of example configuration](doc/img/TouchOSC_OSCSettings.jpg)

### Configure TouchDCS to talk to DCS-BIOS
Unless you have an abnormal configuration, you shouldn't have to do anything here. If your network configuration is different (e.g., DCS-BIOS is running elsewhere on the network and exporting to a non-default IP/port combination), you'll need to edit the settings under `dcsBios` -> `endpoint` to match your network configuration.

### Adding a layout
You can find out more about creating layouts and adding existing layouts [here](https://github.com/charliefoxtwo/TouchDCS/wiki/Layouts). Make sure to change your configuration path under `osc` -> `configLocations` to wherever you end up putting your *.json osc configuration files.

### Launch TouchDCS
That's it! Launch TouchDcs.exe, then load up your favorite layout in TouchOSC and it should automatically sync up with your aircraft in DCS.

When launching for the first time:
 - a modal may pop up saying **Windows protected your PC**. Click _More info_, then click **Run anyway**. Don't worry, TouchDCS is guaranteed to be 99.9% virus free!
 - you may see *another dialog* requesting network permissions. Click **Allow access** (99.9%, remember?)
 - after granting all these permissions, you'll likely have to relaunch in order to actually be able to use your device with TouchDCS. Sorry about that :/

# Going Further
### [Build your own layout](https://github.com/charliefoxtwo/TouchDCS/wiki/Layouts)
Using the TouchOSC Editor on your computer, you can create your own layouts.

### Found a bug?
Probably. This thing needs a lot of work. Open an issue in the Issues section and let's get it fixed.
