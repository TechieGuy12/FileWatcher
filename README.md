# FileWatcher
FileWatcher is a small application designed to monitor folders and files on the local system. When specific changes are detected a notification can be sent to and endpoint via an API request, or an action (copy, move, delete) can be performed.
## Installation
Installing FileWatcher is as simple as extracting the release zip file to the system. No other installation steps are needed as the application was designed to be portable.
### Install as a Windows service
FileWatcher can be installed as a Windows service to allow for monitoring of files and folders when a user is not logged into the machine.

The easiest way to install FileWatcher as a service is to use [NSSM](https://nssm.cc/). Once NSSM is downloaded, you can run the following steps to install FileWatcher as a service:

1. Open a Windows command prompt.
2. Navigate to the NSSM folder.
3. Run the following command: ``nssm install FileWatcher``.
4. When the NSSM window appears, use the following settings:

| Name | Value |
| ---- | ----- |
| Path | Path to the FileWatcher executable (fw.exe). |
| Startup Directory | Folder containing the FileWatcher executable. |
| Arguments | see below. |

The above are all that is needed. You can complete the details and Log on tabs as you see fit (I highly recommend you ensure the service Startup Type is set to 'Automatic' so the service automatically starts on Windows startup.)

## Command Line Arguments
There are a few command line arguments that can be used with fw.exe. They are described below:
| Argument | Description |
| --------- | ----------- |
| -f, --folder &lt;_folder_&gt; | The folder containing the configuration XML file. |
| -cf, --configFile &lt;_config file_&gt; | The name of the configuration XML file. |
| --version | Version information. |
| -?, -h, --help | Show the help and usage information. |

If no arguments are specified, FileWatcher will look for config.xml in the current folder for the settings to use.