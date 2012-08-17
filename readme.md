## VProj: Convert VS Solutions and Projects to .NET 3.5 ##

`VProj` is a simple tool that will copy an existing VS solution and convert it to build under the .NET 3.5 framework.

If you are building a re-usable library and you wisht to target both .NET Frameworks version 4.0 and 3.5 then you might find it useful.

Many projects that target multiple framework versions use a different approach, in that they add conditional elements in the MSBUILD project file. I have experimented
with this alternative approach and prefer it for the following reasons:

1. Minor changes to the Visual Studio projects are required, and they can be performed from Visual Studio.
2. Less confusion to Visual Studio, as there are no custom edits to the project file.
3. Plays nicer with nuget, as there are no conditional elements for assembly references.

That last point is an important one for me. I find that if I have hand-edited `<Reference>` elements in my project file, it can confuse nuget during updates.

## Limitations ##

* Only knows about C# projects. It will ignore everything else. (I only use C#, but it would not be difficult to upgrade for VB, F#, etc).
* Does not convert Web projects. I find that I only want to use this for re-usable assemblies, I never want to target a web application for multiple frameworks.

## Instructions ##

Open your solution and decide which projects you wish to target for multiple frameworks. For each project, ensure that the compile constant `NET40`
is defined. It is important to define `NET40` for every configuration in each project (eg Debug and Release for each project). If you do not define
`NET40` for every configuration in a project, then that project will be ignored.

Run the `vproj` tool to create a copy of your solution file and assocated project files.

To build for multiple framework targets, build your original (.NET 4.0) solution, and then build your copied (.NET 3.5) solution.

If you have code that needs conditional compilation, use the `NET40` and `NET35` conditional compilation constants:

  #if NET40
      // ... code that works under .NET 4.0
  #endif
  
  #if NET35
      // ... code that works under .NET 3.5
  #endif

## Usage ##

  C:\>vproj --help
  Usage: vproj [ options ]
  Convert a VS project from .NET 4.0 to .NET 3.5.
  
  Options:
    -p, --project=VALUE        * Project file
    -o, --output=VALUE           Output file
    -n, --nochange               Show proposed changes, but do not write files
        --clean                  Remove files created from previous operation
        --noprompt               Do not prompt to remove files, just do it
    -v, --verbose                Log additional debug messages
    -q, --quiet                  Quiet operation, log errors and warnings only
    -?, --help                   Show this help message
  (Items marked * are mandatory)

## Conversion Process ##

The following steps outline how this utility converts the solution from .NET 4.0 to .NET 3.5

1. Load the solution file and discard any projects that are not C# projects, and that do not have 


