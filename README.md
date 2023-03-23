
Welcome to GameGearMicro Custom FirmWare Packager

Or as I'd like to call it Ggmcfwp. OK, maybe not. Let's go with Gamp ;)

====== A few words of warning first ======
==== 1. This is not a custom firmware ====
If you think this will replace any modding tool you have... no it won't. This is just a packager to build and install packages containing ROMs and artwork.
So you'll still need a CFW and something to open the SSH server.
I'm using Augen's tool: https://youtu.be/7FJvevOt_wc
Consider buying him a coffee ^^

==== 2. This is not a professional job ====
I've written this tool because I was sick and tired of compiling the artwork manually and because Augen's tool sometimes failed at creating the packages correctly.
So the target audience for this tool is well... me, meaning that it won't look as shiny as other applications and may occassionally crash.
Feel free to submit bugs :)

==== 3. No art anywhere ====
I've rebuilt something reasonably close to the original artwork, but I'm not including SEGA's art here for legal reasons.
If you want the original backgrounds you'll find them, but not here. 


====== What does it do? ======
The reason it was written was mainly to compile the artwork and roms. So basically you hand it a set of ROMS, a set of icons for the games, a font for the labels and it will take care of the rest.
It can also install these packages on the device, but only if the connection is already open.

So specifically:
	It generates full images from templates, icons and game names
	It compressed these PNGs (by reducing colors) so that they will always fit
	It compresses ROMs
	It merges images and roms into bank files compatible with Augen's custom firmware.
	It transfers bank files to an already connected device
	

====== How to use it? ======
There are two basic ways to use it:
==== Filesystem ====
Everything Gamp does is performed in the filesystem. If you drag and drop a game in the GUI it will actually move the file accordingly.
If it's looking for artwork it's actually checking the artwork folder and if it draws text it gets the font name from a JSON file.
This means that aside from clicking the "Generate","Compile" and "Send" buttons, you don't actually have to do anything in the appliation. You can prepare the folders instead.
Gamp's directory structure looks like this:
Set
	Artwork
	Common
	Banks
		Bank1
		...
		Bank7

The Common folder contains four PNGs to be used as backgrounds for the generated content. The file names here cannot be changed, the files MUST always be named 01.png,02.png,03.png,04.png
The Bank1...7 files contain the Roms. All Roms must end on .gg and be named according to their display name prefixed with their slot number and a space, for example "1 Aladdin.gg" or "4 Lemmings.gg"
The Artwork folder should be filled with cover artwork matching the games' names (without the slot prefix). For example Aladdin.png or Lemmings.png . Ideally these files should be 51x58.
The font definitions are in Set/Font.json and should be very easy to understand.
You can do all of this manually and Gamp will read it on startup.

==== GUI ====
Alternatively, you can use the graphical user interface to  set everything up. Specifically You can assign the ROM slots, rearrange them, change fonts and install icons for the games.

To start, simply drag a few games (ending on .gg) to the ROM slots in the tree on the left. You can also drag and drop multiple games to a slot in which case it will assign them to slots in the given bank starting with the one you dragged the games to.
You can rearrange them by dragging one node in the tree to another node, which will switch the content of both.
Rename the games to a reasonable name (beware that since these names are mirrored in the filesystem you can't use characters that are not allowed in paths, for example ":") by right clicking a ROM and chosing "Rename".
You'll notice that empty slots are red, while your assigned slots are now yellow. Yellow means there's no artwork yet.
	Artwork is not managed per slot, but rather by name. Name your artwork exactly like the game's name, for example Aladdin.png, then drag it to the tree.
	There's no way to manage artwork yet, if you want to rename or remove artwork, open the Set/Artwork folder in Windows explorer and make the changes there.
You'll also notice some slots are in italics. These are half-volume slots where for some reason the sound is about half as loud as for the rest.
Now, chose a font and color (or leave them as they are, your choice) by clicking the Font and color buttons.
Click "Generate artwork...". It will generate the artwork. You can view it by clicking through the different banks.
If your happy with the results, click "Compile ... into packages". Note that this won't rebuild the artwork, so always remember to click "Generate artwork..." first.
	This is so that you can manually edit the generated artwork if you want to.
If you want to install the packages, you'll need an open connection first, so open GGMCFW.exe, click slot update and follow the instructions before clicking it.


