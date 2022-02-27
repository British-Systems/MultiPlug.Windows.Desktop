@echo OFF

set INSTALLNAME=MultiPlug Windows Desktop 1.1.6

set MAKENSIS_PATH= NSIS\bin\MAKENSIS.EXE

echo Building MultiPlug Windows Desktop Install - Please Wait.
%MAKENSIS_PATH% /DINSTALLNAME="%INSTALLNAME%" "MultiPlug.Windows.Desktop.nsi"

pause


