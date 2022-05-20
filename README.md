# Doxygen tag file generator for .NET
This project aims to generate external tag files for .NET language to be referenced in a Doxygen configuration file.
This provides the possibility for .NET based projects that uses Doxygen to generate documentation to refer to .NET library types, that links to the [Microsoft Docs API browser][MSDocs].

## Doxygen
[Doxygen][Doxygen] does not need an introduction. It is a tool for generating documentation from the comments of a source code. The tool is primarily for C++ source, but it works with C# XML documentation as well. But don't take my word, [check it out yourself][Doxygen].

## How it works
There are multiple programs for each library that will iterate throiugh all the Microsoft assemblies from the GAC, and collect all the types defined using reflection, then build the XML tag file that can be used with your Doxygen project. The releases on this repository are the tag files themselves for you to use, by providing the file with the base URL to the MS Docs:

~~~
dotnetfw_40.tag=https://docs.microsoft.com/en-us/dotnet/api/
~~~

The tag file is versioned, eventhough it may contain all the types that the latest version can support. That is a limitation for how this project works.

If a tag file is missing, or you wish to alter the program, you can download, compile and use the generated file in your project. There is no point in introducing the program in your integration procedure, as the library for a given version is not changing, and only introduces discrepancies when you run it with a newer .NET library, introducing types that are not available in the target libbrary.

## Known quirks
Types that share the same name, but one of them is a generic type will challenge Doxygen; i.e., recursion detected where there isn't one, or mixes up the generic type with the non-generic type.

[Doxygen]: https://www.doxygen.nl/
[MSDocs]: https://docs.microsoft.com/en-us/dotnet/api/
