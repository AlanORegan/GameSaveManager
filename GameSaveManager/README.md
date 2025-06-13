# GameSaveManager

GameSaveManager is a Windows Forms application for managing game save files and backup configurations.

## Features

- Configure backup strategies for different games
- Validate and edit save file naming formats
- Manage backup limits and revert options
- User-friendly validation and error reporting

## Building

1. Open the solution in Visual Studio.
2. Restore NuGet packages if prompted.
3. In the **Standard toolbar** at the top of Visual Studio, locate the **Solution Platforms** dropdown (usually labeled "Any CPU" by default).
4. Change the platform from **Any CPU** to **x64**.
5. Build the solution (`Build > Build Solution`).

## Building a Deployable Version

To build a deployable version (release build, x64) in the `bin` directory:

**Using Visual Studio:**
1. Open the solution in Visual Studio.
2. In the **Standard toolbar** at the top of Visual Studio, locate the **Solution Configurations** dropdown (usually labeled "Debug" by default).
3. Change the configuration from **Debug** to **Release**.
4. In the **Solution Platforms** dropdown, select **x64**.
5. Build the solution (`Build > Build Solution` or press `Ctrl+Shift+B`).
6. The deployable files will be located in the `bin\Release\x64` directory of your project (e.g.,  
   `GameSaveManager\bin\Release\x64\`).

**Using the command line:**
1. Open a command prompt and navigate to the project directory.
2. Run the following command:
   ```
   msbuild GameSaveManager /p:Configuration=Release /p:Platform=x64
   ```
   or, if you are using the .NET Core/SDK-style project:
   ```
   dotnet build GameSaveManager -c Release -r win-x64
   ```
3. The deployable files will be located in the `bin\Release\x64` directory.

You can distribute the contents of this folder (including all `.dll` and `.exe` files) to run the application on other Windows x64 machines with the appropriate .NET runtime installed.

## Running

- Run the application from Visual Studio (`F5` or `Debug > Start Debugging`).
- Use the UI to add or edit game configurations.

## Notes

- All configuration validation is performed in the UI before saving.
- Error messages are shown at the bottom of the configuration dialog.

## License

This project is for personal use. See LICENSE file if present.
