# reflection-experiments
Series of projects to learn and experiment with C# reflection.

To launch the project, run the command `dotnet run` in your current terminal.

## Features

This project contains a series of coding projects to get better at writing C# reflection code and understanding what happens at runtime.
All projects can be independently tested from a single entry point, including:
- An inspector
- A better inspector integrating my serializer
- Running tests

The inspector lets you input a class name (e.g. 'Player').

<img width="436" height="76" alt="Capture d&#39;écran_20260906_034949" src="https://github.com/user-attachments/assets/0b9a5c94-b0c7-40ed-8636-f02e0e2e1776" />

Then, you can:
- Display the type info (prints type name, fields, and methods)
- Set a field
- Invoke a method

<img width="337" height="126" alt="Capture d&#39;écran_20260906_035420" src="https://github.com/user-attachments/assets/593ca19c-2f29-4d79-b1ce-015adadb9062" />

---
**The serialized inspector implements writing data to a .meta file (json format) when setting a field.**
You can also deserialize data from the current object's file (you can modify the file yourself, and press '5' to deserialize to test it).

<img width="496" height="113" alt="Capture d&#39;écran_20260906_035506" src="https://github.com/user-attachments/assets/dba08fc6-93a9-4f43-a6e5-8d47f727e75c" />

<img width="842" height="163" alt="Capture d&#39;écran_20260906_035520" src="https://github.com/user-attachments/assets/fca26719-04a0-431c-bbb4-24640a0386e9" />
