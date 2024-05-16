## Readme

[Client](./client/README.md)

[Server](./server/README.md)

## Warning

Scary message:

You should understand the capability of this mod and trust the server owner before using it. This will download and place arbitrary code into your EFT BepInEx plugins directory from the server you connect to; that code will then be run from the game client. Except for file consistency between client and server, there are no checks on the mods that are added. This is a great tool for convenience but it has possible risk.

I am not liable for anything done using this mod. Use at your own risk.

### 

This fork is a sketch at making this compatible with SPT. It removes the check for StayInTarkov.dll and removes code to generate "Known Hashes." This means clients must enable `QUARANTINE` or `WARN` mode and allow unknown file hashes, but this isn't performed by default (as a safety measure). I strongly recommend that the server is also in `UPDATE_ONLY` mode.

**Use this at your own risk. You can absolutely get hacked if you connect to a malicious server in this mode.**