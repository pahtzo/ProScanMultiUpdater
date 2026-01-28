# ProScanMultiUpdater

A Windows .NET application specifically designed for updating multiple ProScan installations running on the same machine.
As a current ProScan user, I've read many threads related to "updating multiple ProScan installs" on <a href="https://forums.radioreference.com/forums/scanner-programming-software.89/?prefix_id=86" target="_blank" rel="nopener noreferrer">RadioReference.com</a>
 and noticed quite a few users perform manual or partially scripted updates for every individual installation they have running. Many of
 these ProScan users have numerous installs on a single PC, with some having ten or more individual instances to deal with.
 
 The goal of this project is to help those users
 streamline their update process by using a single application and reducing the potential risks that come with repetitive manual updates.

 Standalone release binaries can be found under this project's Releases page.

 **Note**: This project is unaffiliated with, and completely independent from **ProScan**.  For ProScan related information, downloads, and support visit <a href="https://proscan.org" target="_blank" rel="nopener noreferrer">https://proscan.org</a>

 **Note**: ProScanMultiUpdater is free software released under the MIT License, see the LICENSE file for details.

## Overview

ProScanMultiUpdater enumerates running processes named `ProScan.exe` containing a copyright string starting with "ProScan" and ending with "Bob Aune" (the developer of ProScan).

If you choose not to let the app download the latest ProScan installer then you must first manually download it from the ProScan website, extract the EXE installer from the ZIP file, then select the EXE installer using the **Browse** button. The updater expects the standard ProScan installer filename format `ProScan_X_Y.exe` where X, Y are the major and minor version numbers.

This is all handled automatically when using the option to download the latest version.

ProScanMultiUpdater simplifies the process of updating multiple ProScan installations by:
- Automatically detecting all running ProScan processes and their installation directories (ex. `C:\ProScan01, C:\ProScan02, C:\ProScan03`, etc.)
- Downloading the latest version from the ProScan website
- Optional manual installer selection if automatic download is not confirmed
- Stopping selected instances gracefully
- Installing updates to each unique installation directory
- Relaunching instances as their original user account with proper privilege management

## Minimum Requirements
- **Operating System**: Windows 7 SP1 and higher
- **.NET Framework**: 4.8 and higher
- **Permissions**: Run with UAC elevation (run-as administrator)

## Tested On
- Windows 11 24H2 64 bit
- Windows 10 22H2 64 bit
- Windows 7 SP1 32 & 64 bit

## How to Use

### Step 1: Backup your system and/or ProScan install directories (Optional but recommended)
Although the ProScanMultiUpdater does not touch any configuration, recordings, or data files, it is ***highly recommended*** to backup your ProScan install directories using whatever means you already have in place for backups before updating!

If you have any ProScan **"Profile Editor"** or **"Favorite Editor"** windows open you ***must*** save your work and close those before updating the
running ProScan instances (leave the actual ProScan instances running, don't close those).

### Step 2: Launch ProScanMultiUpdater
Download the latest version from the Releases page and launch the application.  Windows will auto-detect if it's running elevated.  If the app
isn't running elevated, Windows will prompt for UAC elevation.

### Step 3: Scan For Running ProScan Instances (Optional)
1. Every time it's launched the application will automatically detect all running ProScan instances
2. Review the list in the data grid
3. Optional, to refresh the process listing click **"Scan For Running ProScan Instances"** (after running updates the process list is cleared so you can use this to re-populate it)

### Step 4: Select Processes to Update
- Select the process instances you want to update in the grid view
- Use **"Select All"** or **"Deselect All"** for convenience

### Step 5: Browse - Choose a ProScan Setup Installer (Optional)
- Click **"Browse"** to manually select an already downloaded and extracted ProScan installer
- Or proceed without manually selecting - the app will offer to download the latest version

### Step 6: Restart Processes (Optional)
- **If "Restart Updated Instances" is checked:** (default Yes)
  - Processes will automatically restart as their original users
  - Each process is relaunched with the same privileges it had before
  
- **If unchecked:**
  - Processes will not restart automatically
  - You have to manually start your ProScan instances when complete

### Step 7: Close Selected Instances & Update
1. Click **"Close Selected Instances & Update"**
2. You'll be prompted to download the latest version from the ProScan website, otherwise the updater will use the manually selected installer you selected in Step 5
3. Confirm the update operation
4. The updater will:
   - Switch to the Logging tab
   - Attempt graceful shutdown of each process
   - Force-kill if graceful shutdown fails
   - Run the installer for each unique installation directory
   - Wait for each installer to complete

### Step 8: Save Log As (Optional)
- Right-Click in the logging output and **"Save Log As"** to save the complete update log
- Logs include timestamps, installer output, and any errors encountered

### Quick Update Steps
Close Profile Editor and Favorite Editor before continuing!
- Launch ProScanMultiUpdater
- Select Processes to Update
- Close Selected Instances & Update (Yes download latest, Yes confirm updates)

## Key Features

### 🔍 Automatic Process Detection
- Scans for all running 'ProScan' processes
- Basic verification check of the found processes with known copyright strings
- Displays detailed information for each instance:
  - Process ID
  - Installation path
  - Window title
  - Version number
  - Start time
  - Running user account

### 📥 Automated Download
- Automatically checks the ProScan website for the latest version
- Downloads, extracts, and runs the downloaded installer
- Optionally allows manual installer selection

### ✅ Selective Update
- Choose which ProScan instances to update
- Select All / Deselect All options
- Updates only unique installation directories (avoids duplicate updates)

### 🔐 Privilege-Aware Relaunching
- Captures user security tokens before terminating processes
- Relaunches processes as their original user account
- Handles cross-privilege scenarios (elevated updater → non-elevated processes)
- Works across 32-bit/64-bit process boundaries

### 📋 Comprehensive Logging
- Real-time updater logging of all operations
- Individual Inno Setup installation logs per install directory
- Save updater log functionality for troubleshooting

## Installation Behavior

The updater runs the ProScan installer (Inno Setup) with the following parameters:
- `/VERYSILENT` - Silent installation (no UI)
- `/NORESTART` - Prevents automatic system restart
- `/NOICONS` - Attempt skipping Start Menu icon creation
- `/MERGETASKS="!desktopicon"` - Disable desktop icon creation
- `/DIR="<path>"` - Installs to existing directory (in-place update)
- `/LOG="<path>\ProScanMultiUpdater-install.log"` - Creates custom-named installation log
- `/FORCECLOSEAPPLICATIONS` - Force-close application for update (they should already be closed at this point)

## Troubleshooting

### "No ProScan processes running"
**Solution**: Start at least one ProScan instance before running the updater.  The updater will only
update instances that are found running on the system so ideally have all your ProScan instances running
before launching the updater or clicking **"Scan For Running ProScan Instances"**

### Updated process fails to restart
**Possible causes**:
- **"Restart Updated Instances"** was not selected

**Solution**: Manually start your instances.

### Download latest version fails
**Possible causes**:
- No internet connection
- ProScan website unavailable or blocked
- ProScan_X_Y.zip installer package not found on the ProScan website
- Download blocked by endpoint security software

**Solution**: Attempt to manually download the installer from the official ProScan website, extract the installer, then use the Browse button to select it.

## Bugs & Issues

For bug and issue reporting:
- **ProScanMultiUpdater**: Open a detailed issue in this project's GitHub Issue Tracker and submit the `ProScanMultiUpdater-YYYY-MM-DD-HHMMSS.txt` log saved from the Logging tab view.


