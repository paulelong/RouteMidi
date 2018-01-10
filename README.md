`# RouteMidi
Console Midi Routing program

Allows you to configure MIDI routes between MIDI Ports as well as UDP port.  Press ? at the prompt for more information on commands.

## Commands
### Midi Info:
* I - Show Input Routes
* O - Show Output Routes
* B - Show Both In/Out Routes
### Routes:
* A - Add Route
* D - Delete Route
* R - Show Route
### Configurations:
* C - Show Configurations
* N - New Configuration
* P - Pick Configuration
* S - Save all Configurations
* L - Load all Configuration
* U - Update Configuraiton Name
### Other:
- M - Monitor Messages

A configuration file can be used as well. Default location is local and the file is called RouteMidiConfig.cfg.  This file is simple a CSV with the imput first followed by the output.  Also #UDPPort can be used as an input.  For instance:

```
Digital Piano, MidiOut2
#9000, LoopBe Internal MIDI
```
