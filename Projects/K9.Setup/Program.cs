// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using K9.Setup.Verbs;
using K9.Utils;

namespace K9.Setup
{
    internal class Program : IProgram
    {
        public static Program Instance;
        public string DefaultLogCategory => "K9.SETUP";

        private static void Main(string[] args)
        {
            try
            {
                // Initialize Core
                Instance = new Program();
                Core.Init(Instance);

                Parser parser = Core.GetDefaultParser(true);

                ParserResult<object> results =
                    parser.ParseArguments<Perforce, SetEnvironmentVariable, WriteFile, DeleteFolder, DeleteFile,
                        CopyFile, CopyFolder, Checkout, Zip, CompressFolder, ExtractToFolder, ReplaceInFile>(Core.Arguments);

                bool newResult = results.MapResult(
                    (Perforce perforce) => perforce.CanExecute() && perforce.Execute(),
                    (SetEnvironmentVariable env) => env.CanExecute() && env.Execute(),
                    (WriteFile write) => write.CanExecute() && write.Execute(),
                    (DeleteFolder deleteFolder) => deleteFolder.CanExecute() && deleteFolder.Execute(),
                    (DeleteFile deleteFile) => deleteFile.CanExecute() && deleteFile.Execute(),
                    (CopyFile copy) => copy.CanExecute() && copy.Execute(),
                    (CopyFolder copyFolder) => copyFolder.CanExecute() && copyFolder.Execute(),
                    (Checkout checkout) => checkout.CanExecute() && checkout.Execute(),
                    (Zip zip) => zip.CanExecute() && zip.Execute(),
                    (CompressFolder compressFolder) => compressFolder.CanExecute() && compressFolder.Execute(),
                    (ExtractToFolder extractFolder) => extractFolder.CanExecute() && extractFolder.Execute(),
                    (ReplaceInFile replaceInFile) => replaceInFile.CanExecute() && replaceInFile.Execute(),
                    _ => false);

                if (!newResult)
                {
                    CommandLineUtil.HandleParserResults(results);
                }
            }
            catch (Exception e)
            {
                Core.ExceptionHandler(e);
            }
            finally
            {
                Core.Shutdown();
            }
        }
    }
}