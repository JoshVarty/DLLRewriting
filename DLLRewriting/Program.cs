using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLLRewriting
{
    class Program
    {
        //The name of the type containing the bug
        const string IBufferGraphExtensions = "IBufferGraphExtensions";

        //The name of the method containing the bug
        const string ClassifyBufferMapDirection = "ClassifyBufferMapDirection";

        //The fully qualified name of the incorrectly referenced type
        const string IProjectionBuffer = "Microsoft.VisualStudio.Text.Projection.IProjectionBuffer";

        //The fully qualified name of the correct type
        const string IProjectionBufferBase = "Microsoft.VisualStudio.Text.Projection.IProjectionBufferBase";

        //The user's default directory.
        static string APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        //The path to Visual Studio 2015's installed extensions
        static string VS_EXTENSIONS_PATH = APPDATA_PATH + @"\Microsoft\VisualStudio\14.0\Extensions\";

        //The DLL with the bug
        static string brokenDLLFileName = @"Microsoft.CodeAnalysis.EditorFeatures.dll";

        static void Main(string[] args)
        {
            //Obviously not the best way to find a file...
            //We're executing in /bin/Debug, so we'll just jump up two directories 
            //to find the broken DLL.
            var brokenAssembly = getBrokenAssembly("../../" + brokenDLLFileName);

            //Get the broken Method
            var brokenType = brokenAssembly.MainModule.Types.Single(n => n.Name == IBufferGraphExtensions);
            var brokenMethod = brokenType.Methods.Single(n => n.Name == ClassifyBufferMapDirection);

            var processor = brokenMethod.Body.GetILProcessor();

            //Find all the incorrect casts to IProjectionBuffer
            var incorrectInstructions = new List<Instruction>();
            foreach (var item in brokenMethod.Body.Instructions)
            {
                if (item.Operand != null && item.Operand.ToString() == IProjectionBuffer)
                {
                    incorrectInstructions.Add(item);
                }
            }

            //Get the type we'd like to cast to
            var IProjectionBufferBaseType = getIProjectionBufferBaseType();
            //Make sure our broken assembly references this type
            var typeReference = brokenAssembly.MainModule.Import(IProjectionBufferBaseType);

            //New instruction: "Is instance of IProjectionBufferBase
            var newInstruction = processor.Create(OpCodes.Isinst, typeReference);
            
            foreach(var instruction in incorrectInstructions)
            {
                processor.Replace(instruction, newInstruction);
            }

            //Write back to file. If we'd like, we could overwrite the original DLL.
            brokenAssembly.Write("../../Microsoft.CodeAnalysis.EditorFeatures_FIXED.dll");

        }

        private static TypeDefinition getIProjectionBufferBaseType()
        {
            var dllLocation = "../../Microsoft.VisualStudio.Text.Data.dll";
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(dllLocation);
            var projectionBufferBaseType = assemblyDefinition.MainModule.Types.Single(n => n.Name == "IProjectionBufferBase");
            return projectionBufferBaseType;
        }

        private static AssemblyDefinition getBrokenAssembly(string brokenFilePath)
        {
            var resolver = new DefaultAssemblyResolver();

            //The assembly depends on other DLLs, we add search directories to a resolver to help it find these DLLs
            var searchDirectory = "../../ReferencedDLLs";
            resolver.AddSearchDirectory(searchDirectory);

            var parameters = new ReaderParameters()
            {
                AssemblyResolver = resolver
            };

            string assemblyPath = brokenFilePath + brokenDLLFileName;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(brokenFilePath, parameters);
            return assemblyDefinition;
        }
    }
}
