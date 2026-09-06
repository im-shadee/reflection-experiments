# reflection-experiments
Series of projects to learn and experiment with C# reflection.

To launch the project, run the command
`dotnet run`

in your current terminal.

## Features
This project contains a series of coding projects to get better at writing C# reflection code and understanding what happens at runtime.
All projects can be independently tested from a single entry point, including:
- An inspector
- A better inspector integrating my serializer
- Running tests

The inspector lets you input a class name (e.g. 'Player').
Then, you can:
- Display the type info (prints type name, fields, and methods)
- Set a field
- Invoke a method

The serialized inspector implements writing data to a .meta file (json format) when setting a field.
You can also deserialize data from the current object's file (you can modify the file yourself, and press '5' to deserialize to test it).
