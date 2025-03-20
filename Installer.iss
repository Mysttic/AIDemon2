[Setup]
AppName=AIDemon2
AppVersion=1.0.0
DefaultDirName={pf}\AIDemon2
DefaultGroupName=AIDemon2
OutputBaseFilename=AIDemon2_Installer
Compression=lzma
SolidCompression=yes

[Files]
Source: "AIDemon2/bin/Release/publish/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AIDemon2"; Filename: "{app}\AIDemon2.exe"
Name: "{group}\Odinstaluj AIDemon2"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\AIDemon2.exe"; Description: "Uruchom AIDemon2"; Flags: nowait postinstall skipifsilent
