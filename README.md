# Universal-disk-mounter

- Mounts partition to user selected drive letter
- Uses WSL as backend
- Supports any FS that has an appropriate driver installed in WSL (I.e. Reiserfs needs reiserfs-utils)

## Limitations
- WSL Doesn't support mounting USB devices as of writing this article
- The disk label is set to "Disconnected network drive", while not affecting functionality (The folder is still accesible) it might cause confusion.

## Prerequisites
- WSL is enabled and installed, C:\Windows\System32\wsl.exe is the executable path
- Administrator access for the (un)mount command
- [TEMPORARY] WSL Distribution is Ubuntu

## Setup
- TODO: Add distro config
- Building and running the raw exe should be fine as of now.

## Usage
- After building, run the .exe file in an **UNELEVATED** command prompt
- Select operation mode [m/mount -> for mounting new pattitions, u/unmount -> for unmounting existing partitions]
- Follow the instructions on screen to mount/unmount the partition

--- 

> GUI Version in development.
