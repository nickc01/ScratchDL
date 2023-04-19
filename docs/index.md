![Logo](~/images/logo.png)
# ScratchDL
A .Net Library and Tool for downloading data from the Scratch Website
You can use this to do things such as:
- Download All Projects from a profile
- Download all followers of a profile
- Download a studio
- Download all project comments, remixes, and studios


# Build Instructions

Make sure you have the .Net SDK 6.0 installed. You can download it here https://dotnet.microsoft.com/en-us/download

```bash
git clone https://github.com/nickc01/ScratchDL.git

cd ScratchDL

dotnet build

dotnet run ScratchDL.GUI
```

See the [ScratchAPI](xref:ScratchDL.ScratchAPI) class for API documentation