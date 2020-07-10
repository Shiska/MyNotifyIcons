# MyNofityIcons

## Graphical user interface

The GUI can be used to start chosen applications with tray icons and add them to the autostart list. The autostart will only work if the "start with Windows" checkbox is active.

## Command line

The GUI just calls itself with the correct parameters to start the applications with a tray icon.

```
MyNofityIcons.exe --start "path_to_file" hidden_true_false
```
If hidden is true than the application hides in the tray after startup.