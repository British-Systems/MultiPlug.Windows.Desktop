
;--------------------------------
;Includes

	!include "MUI2.nsh"
	!include LogicLib.nsh

;--------------------------------
;General

	; The name of the installer
	Name "${INSTALLNAME}"

	; The file to write
	OutFile "DesktopSetup.exe"

	; The default installation directory
	InstallDir "$PROGRAMFILES\MultiPlug\Desktop"
	
	;Get installation folder from registry if available
	InstallDirRegKey HKCU "Software\British Systems\MultiPlug\Desktop" ""

	; Request application privileges for Windows Vista
	RequestExecutionLevel admin

	!define MUI_ICON "Resources\MultiPlug.ico"

	BrandingText "MultiPlug Windows Desktop"
	
	!define MUI_COMPONENTSPAGE_SMALLDESC
	
	!define MUI_WELCOMEFINISHPAGE_BITMAP "Resources\install-left.bmp" ;

;--------------------------------
;Variables	

	Var StartMenuFolder		
;--------------------------------
;Pages

	!insertmacro MUI_PAGE_WELCOME
	!insertmacro MUI_PAGE_LICENSE "Resources\License.txt"
	!insertmacro MUI_PAGE_DIRECTORY
	
	;Start Menu Folder Page Configuration
	!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU" 
	!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\MultiPlug Desktop" 
	!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
	
	!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder	
	!insertmacro MUI_PAGE_INSTFILES
	
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES
  
;--------------------------------
;Languages
 
	!insertmacro MUI_LANGUAGE "English"
;--------------------------------


; The stuff to install
Section  "" Section1

	CreateDirectory $INSTDIR
	
	SetOutPath $INSTDIR
	
	;Store installation folder
	WriteRegStr HKCU "Software\British Systems\MultiPlug\Desktop" "" $INSTDIR
	
	;Create uninstaller
	WriteUninstaller "$INSTDIR\Uninstall.exe"
	
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiPlugWindowsDesktop" \
                 "DisplayName" "MultiPlug Windows Desktop"
				 
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiPlugWindowsDesktop" \
                 "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
				 			 
	DetailPrint "Stopping previous versions of MultiPlug Windows Desktop"	
	StrCpy $0 "MultiPlug.Windows.Desktop.exe"
	KillProc::KillProcesses
	Sleep 2000
	StrCmp $1 "-1" cantclose
	
	
			  
	DetailPrint "Installing core files"	
	File "..\src\MultiPlug.Windows.Desktop\bin\Release\MultiPlug.Windows.Desktop.exe"
	
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "MultiPlug Windows Desktop" "$INSTDIR\MultiPlug.Windows.Desktop.exe"
	
		!insertmacro MUI_STARTMENU_WRITE_BEGIN Application  
		;Create shortcuts
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortcut "$SMPROGRAMS\$StartMenuFolder\MultiPlug Desktop.lnk" "$INSTDIR\MultiPlug.Windows.Desktop.exe"
		CreateShortcut "$SMPROGRAMS\$StartMenuFolder\Uninstall MultiPlug Desktop.lnk" "$INSTDIR\Uninstall.exe"
	!insertmacro MUI_STARTMENU_WRITE_END
	
	Goto completed
 
	cantclose:
		DetailPrint "Error: Could not close MultiPlug Desktop."
		Abort

	completed:
		ExecShell "" "$INSTDIR\MultiPlug.Windows.Desktop.exe"
	
		
SectionEnd ; end the section

;--------------------------------
;Uninstaller Section
Section "uninstall"

	StrCpy $0 "MultiPlug.Windows.Desktop.exe"
	KillProc::KillProcesses
	Sleep 2000
	StrCmp $1 "-1" cantclose

	
	Delete "$SMPROGRAMS\$StartMenuFolder\Uninstall MultiPlug Desktop.lnk"
	Delete "$SMPROGRAMS\$StartMenuFolder\MultiPlug Desktop.lnk"

	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiPlugWindowsDesktop"
	DeleteRegValue HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "MultiPlug Windows Desktop"

	Delete "$INSTDIR\MultiPlug.Windows.Desktop.exe"		

	Delete "$INSTDIR\Uninstall.exe"

	RMDir /r "$INSTDIR"

	DeleteRegKey /ifempty HKCU "Software\British Systems\MultiPlug"		

	Goto completed

	cantclose:
		DetailPrint "Error: Could not close MultiPlug Desktop."
		Abort

	completed:


;	done:
 
SectionEnd



