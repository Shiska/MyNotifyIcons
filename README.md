# MyNofityIcons

## Graphical user interface

The GUI can be used to start chosen applications with tray icons and add them to the autostart list. The autostart will only work if the "start with Windows" checkbox is active.

## Command line

Start the application hidden without arguments.
```
MyNofityIcons.exe --start "path_to_file"
```
You can add arguments after the path but then you also need to specify the hidden value as last argument.
```
MyNofityIcons.exe --start "path_to_file" arguments hidden_true_false
```