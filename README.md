# reflection-experiments
Series of projects to learn and experiment with C# reflection.

To launch the project, run the command `dotnet run` in your current terminal.

## Features

This project contains a series of coding projects to get better at writing C# reflection code and understanding what happens at runtime.
All projects can be independently tested from a single entry point, including:
- An inspector
- A better inspector integrating **my own serializer and DI container**
- Running tests

The basic inspector lets you input a class name (e.g. 'Player').

<img width="436" height="76" alt="Capture d&#39;écran_20260906_034949" src="https://github.com/user-attachments/assets/0b9a5c94-b0c7-40ed-8636-f02e0e2e1776" />

Then, you can:
- Display the type info (prints type name, fields, and methods)
- Set a field
- Invoke a method

---
**The serialized inspector implements writing data to a .meta file (json format) when setting a field.** It also implements Serializing/deserialize info from the current player instance to/from disk.

<img width="496" height="113" alt="Capture d&#39;écran_20260906_035506" src="https://github.com/user-attachments/assets/dba08fc6-93a9-4f43-a6e5-8d47f727e75c" />

<img width="842" height="163" alt="Capture d&#39;écran_20260906_035520" src="https://github.com/user-attachments/assets/fca26719-04a0-431c-bbb4-24640a0386e9" />

---
I also made a custom DI Container to learn more about Dependency Injection, implemented in this inspector to let you inspect the dependencies of your selected class.

<img width="420" height="170" alt="Copie d&#39;écran_20260909_104809" src="https://github.com/user-attachments/assets/8ef9ce38-c82a-42ae-88cc-d598178dda3b" />

<img width="653" height="132" alt="Copie d&#39;écran_20260909_104817" src="https://github.com/user-attachments/assets/ad5dc180-4d7e-4679-8aa0-347d10541e65" />

## License
This project is under the **MIT license**. See LICENSE for the complete license text.
