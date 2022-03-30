# File Watcher

File Watcher is an application designed to monitor folders and files on the local system. When specific changes are detected a notification, can be sent to and endpoint via an API request, an action (copy, move, delete) can be performed, or a command executed.

## Features

File Watcher includes the following:

**Monitor files and folders.** Specify paths to folders on a local or external hard drive, and perform an action when a file or folder is created, modified, or deleted in the path.

**Exclude, or include, specific files and folders.** Files and folders can be excluded from monitoring based on the name, attribute, or path.

**Send notifications to an API endpoint.** Send an API request to an endpoint on a creation, modification, or deletion of a file or folder.

**Perform an action.** Copy, move, or delete a file or folder when a change is detected.

**Run a command.** Run a command, such as an executable or script, when a file or folder change is detected.

**Portable.** No installation is required. Download the [latest release](https://github.com/TechieGuy12/FileWatcher/releases/latest) and unzip the contents to a folder. Create the [configuration file](https://github.com/TechieGuy12/FileWatcher/wiki/Configuration-File) and then run the executable.

**Low resource usage.** With 7 watches monitoring a mix of internal and USB-connected external hard drives, File Watcher uses less than 40 MB of RAM, and negligible CPU usage.

**Logging.** Writes to a log file, that includes rollover functionality.

## System Support

- Windows
- MacOS
- Linux

For information using File Watcher, please read the [Wiki](https://github.com/TechieGuy12/FileWatcher/wiki).

For example use cases for File Watcher, please read [Use Cases](https://github.com/TechieGuy12/FileWatcher/wiki/Use-Cases).
