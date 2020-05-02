# SOR4Explorer
Simple modding utility for Streets of Rage 4

This small utility shows a file navigator and allows you
to browse the game's textures, copy them to the clipboard,
and export them as PNG.

Eventually, it will also allow you to replace textures,
but at the moment it's unclear if the game would load
additional texture files or the replacements need to be
written inside the existing big files, which would be
pretty cumbersome and slow.

## Installation

This program uses the .Net Core 5 preview. Get it from

    https://dotnet.microsoft.com/download/dotnet/5.0

and then 

    dotnet run

## Usage

* Drop the SOR4 installation folder (which will contain
  just a 'data' and a 'x64' subfolder) into the window.
* You can double click an image and export/copy it
  from the image's context menu, drag them to a folder, etc.
* In order to export textures in bulk, right click a folder
  in the left tree and choose 'Save as...' (warning: slow!)


## Trivia

The game seems to be very data-driven.

The file named 'bigfile' contains a serialization of custom
.Net objects including data for all the characters, moves, 
levels and a lot of other stuff. If this file is correctly
reverse-engineered, there is a chance modders would
eventually be able to add new character or moves to the 
game, instead of just making texture swaps.
