# Music Game with C# and F#

This is a music rhythm game that uses both C# and F#.

### Getting Started

1. Clone the repository.
2. Create new solution:
   `dotnet new sln -n RhythmGameSystem; mkdir CoreSrc ShellSrc`
3. Build F# compilation library:
   `cd CoreSrc; dotnet new classlib -lang "F#" -n RhythmGame.Core; cd ..`
4. Build UI system driver:
   `cd ShellSrc; dotnet new console -n RhythmGame.Shell; cd ..`
5. Bind compilation project:
   `dotnet sln add CoreSrc/RhythmGame.Core/RhythmGame.Core.fsproj; dotnet sln add ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj; dotnet add ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj reference CoreSrc/RhythmGame.Core/RhythmGame.Core.fsproj`
6. Compile and run the project:
   `dotnet run --project ShellSrc/RhythmGame.Shell/RhythmGame.Shell.csproj`