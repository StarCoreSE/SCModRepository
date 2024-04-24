Modular Assemblies Definitions
============================

Hey modders!

This is a bit of a mess, so please bear with me. Ping [@aristeas.] on discord if you have any questions, comments, or concerns.
https://discord.com/invite/kssCqSmbYZ

This mod behaves kind of like Assemblycore. Kind of. Definitions are declared in a similar manner, with an example below.
What makes this mod unique (other than the modular stuff) is that you're supposed to run code in it. It will not function well otherwise.

I tried my best to document stuff, most of it will be in DefinitionAPI.cs
  - You can also hover over variables in most IDEs for a description.
You have access to ModularApi, DebugDrawManager, and WcAPI; ModularApi handles stuff like GetMemberParts for modular assemblies, whereas WcAPI is your bog-standard Assemblycore ModAPI. DebugDrawManager is useful for debugging, and can be accessed statically.
If you need logic to run in a MySessionComponent, you can init the ModularApi via LoadData() and UnloadData().

Modular Definitions can be found in Data\Scripts\ILOVEKEEN\ModularAssemblies\. Don't forget to make a Assemblycore gun! It doesn't have to be part of the same mod, but it is in this example.
As for file structure, DON'T TOUCH ANYTHING IN Scripts\ILOVEKEEN\ModularAssemblies OTHER THAN DEFINITIONS. It is all important.

Good luck, and happy modularizing!





By [@aristeas.], with help from [@validpoint].
Debug drawing referenced from [@m_digi]'s "(DevTool) Programmable Block Debug API"
ModAPI, WcAPI, and included assembly mod referenced from [@darkstar0818]'s "AssemblyCore - 2.4"
