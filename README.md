# Universal-disk-mounter

- Mounts partition to user selected drive letter
- Uses WSL as backend
- Supports any FS that has an appropriate driver installed in WSL


## Limitations
- WSL Doesn't support mounting USB devices as of writing this article
- The disk label is set to "Disconnected network drive", while not affecting functionality (The folder is still accesible) it might cause confusion.
- SUBST only works while the terminal session remains open. The console will automatically unmount properly once the PAUSE command issued after a succesful mount is released.
- A solution for the above will be made in the GUI version.


## Prerequisites
- WSL is enabled and installed, C:\Windows\System32\wsl.exe is the executable path
- Administrator access for the (un)mount command


## Setup
- Building and running the raw exe should be fine as of now.

1. After building, run the .exe file in an **UNELEVATED** command prompt
2. Follow the instructions on screen to mount/unmount the partition
3. Once it is mounted, leave the console open for the SUBST to not cancel.
4. Once you want to close the session and unmount the disk, hit enter. This will automatically unmount the disk.

## Tested file systems

|Filesystem|Capabilities (Read/Write)|
| --- | --- |
| EXT2/3/4 | RW\*\* |
| NTFS | RW |
| ExFAT | RW |
| FAT16 | RX |
| FAT32 | RX |
| btrfs | RX\*\*\* |
| XFS | RX\*\*\* |
| Reiserfs | XX |
| HFS | XX* |
| HFS+ | ??\*|


\* HFS should work, but the mount command has to be further specified. A formatting update is TBD, will address this.

\*\* RW is accesible in all folders that have the proper permissions set (I.E. You cannot access /root)

\*\*\* Further permission testing needs to be performed for writing.


--- 

> GUI Version in development.
