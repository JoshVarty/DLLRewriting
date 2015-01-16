# Basic DLL Rewriting

Code sample based on the talk ["All yours DLLs are belong to us"](https://www.youtube.com/watch?v=mhtuVcE7o3A)

[Red Gate Reflector](http://www.red-gate.com/products/dotnet-development/reflector/) - IL Decompiler

[Gray Wolf](https://digitalbodyguard.com/graywolf.html) - IL Rewriter/Reverse Engineering Tool

[Code Connect](http://codeconnect.io) - Visual Studio plugin dependant on flawed DLL.


### The problem

Code Connect's editors are `IElisionBuffers`. This means they project text from one source document (A typical C# file) to our custom editors. They inherit from `IProjectionBufferBase` as shown in the inheritance hierarchy below:

![Visual Studio Hierarchy](http://i.imgur.com/bC2qioI.png)

Unfortunately, within Visual Studio 2013's copy of of `Microsoft.CodeAnalysis.EditorFeatures.dll` lived a frustrating bug. 

The following function was supposed to work out how two buffers were related to one another. It would answer questions like "Is `startBuffer` the source buffer for `destinationBuffer`?"
However, instead of casting the buffer to `IProjectionBufferBase`, it cast to `IProjectionBuffer`. This meant that this case failed for all Code Connect's `IElisionBuffers`.

![Broken function](http://i.imgur.com/SBNSWBg.png)

At this point, Microsoft was no longer actively supporting Roslyn for Visual Studio 2013. So this bug was not getting fixed. We needed to get Code Connect out to our users, so we patched this bug with Mono.Cecil. 

This project simply fixes the improper cast in the above code.
