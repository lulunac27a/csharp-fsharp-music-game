# Music Game with C# and F#

This is a music rhythm game that uses both C# and F#.

### Getting Started

```
# 1. Create a workspace solution directory shell structure
dotnet new sln -n RhythmGameSystem
mkdir CoreSrc ShellSrc

# 2. Build the underlying F# compilation library module asset dependencies
cd CoreSrc
dotnet new classlib -lang "F#" -n RhythmGame.Core
cd ..

# 3. Build the UI system driver app framework shell execution layout
cd ShellSrc
dotnet new console -n RhythmGame.Shell
cd ..

# 4. Bind compilation project assembly reference targets together 
dotnet sln add CoreSrc/RhythmGame.Core/RhythmGame.Core.fsproj
dotnet sln add ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj
dotnet add ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj reference CoreSrc/RhythmGame.Core/RhythmGame.Core.fsproj

# 5. Compile and run your cross-language project setup
dotnet run --project ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj
```