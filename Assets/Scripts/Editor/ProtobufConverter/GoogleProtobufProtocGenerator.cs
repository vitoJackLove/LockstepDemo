using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor
{
    public static class GoogleProtobufProtocGenerator
    {
        private const string DefaultProtobufHome = @"E:\protobuf";
        private const string ProtoRoot = "Assets/Proto";
        private const string ProtocExeEnvKey = "ROGUE_PROTOC_EXE";
        private const string ProtobufHomeEnvKey = "ROGUE_PROTOBUF_HOME";
        private const string ProtobufIncludeEnvKey = "ROGUE_PROTOBUF_INCLUDE";

        private static readonly ProtocJob[] Jobs =
        {
            new ProtocJob(
                "network_packet_runtime.proto",
                "Assets/Scripts/RunTime/Server/Protobuf/Generated"),
            new ProtocJob(
                "google_protobuf_class_conversion_test.proto",
                "Assets/Scripts/RunTime/TestProtobuf/Generated"),
        };

        [MenuItem("Tools/Protobuf Converter/Generate Google.Protobuf Sources", false, 110)]
        public static void GenerateFromMenu()
        {
            RunAndReport();
        }

        public static void GenerateAllForCi()
        {
            RunAndReport();
        }

        private static void RunAndReport()
        {
            try
            {
                GenerateAllInternal();

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Protobuf] Generation failed: {ex}");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                EditorUtility.DisplayDialog("Protobuf generation failed", ex.Message, "OK");
            }
        }

        private static void GenerateAllInternal()
        {
            string protocExe = ResolveProtocExe();
            string protobufIncludePath = ResolveProtobufIncludePath();
            ValidateEnvironment(protocExe, protobufIncludePath);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            for (int i = 0; i < Jobs.Length; i++)
            {
                GenerateJob(projectRoot, Jobs[i], protocExe, protobufIncludePath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static string ResolveProtocExe()
        {
            string protocExe = Environment.GetEnvironmentVariable(ProtocExeEnvKey);
            if (!string.IsNullOrWhiteSpace(protocExe))
            {
                return protocExe;
            }

            string protobufHome = ResolveProtobufHome();
            return Path.Combine(protobufHome, "bin", "protoc.exe");
        }

        private static string ResolveProtobufIncludePath()
        {
            string includePath = Environment.GetEnvironmentVariable(ProtobufIncludeEnvKey);
            if (!string.IsNullOrWhiteSpace(includePath))
            {
                return includePath;
            }

            string protobufHome = ResolveProtobufHome();
            return Path.Combine(protobufHome, "include");
        }

        private static string ResolveProtobufHome()
        {
            string protobufHome = Environment.GetEnvironmentVariable(ProtobufHomeEnvKey);
            return string.IsNullOrWhiteSpace(protobufHome) ? DefaultProtobufHome : protobufHome;
        }

        private static void ValidateEnvironment(string protocExe, string protobufIncludePath)
        {
            if (!File.Exists(protocExe))
            {
                throw new FileNotFoundException($"protoc.exe not found: {protocExe}");
            }

            if (!Directory.Exists(protobufIncludePath))
            {
                throw new DirectoryNotFoundException($"protobuf include path not found: {protobufIncludePath}");
            }

            string protoRootFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ProtoRoot));
            if (!Directory.Exists(protoRootFullPath))
            {
                throw new DirectoryNotFoundException($"proto root not found: {protoRootFullPath}");
            }
        }

        private static void GenerateJob(string projectRoot, ProtocJob job, string protocExe, string protobufIncludePath)
        {
            string sourcePath = $"{ProtoRoot}/{job.SourceFile}";
            string sourceFullPath = Path.GetFullPath(Path.Combine(projectRoot, sourcePath));
            string outputFullPath = Path.GetFullPath(Path.Combine(projectRoot, job.OutputDirectory));

            if (!File.Exists(sourceFullPath))
            {
                throw new FileNotFoundException($"Proto source not found: {sourceFullPath}");
            }

            Directory.CreateDirectory(outputFullPath);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = protocExe,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = string.Join(" ",
                    Quote($"--proto_path={ProtoRoot}"),
                    Quote($"--proto_path={protobufIncludePath}"),
                    Quote($"--csharp_out={job.OutputDirectory}"),
                    Quote(sourcePath))
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException($"Failed to start protoc for {sourcePath}");
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"protoc failed for {sourcePath} (exit code {process.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                }

                UnityEngine.Debug.Log($"[Protobuf] Generated {sourcePath} -> {job.OutputDirectory}");
            }
        }

        private static string Quote(string value)
        {
            return $"\"{value}\"";
        }

        private readonly struct ProtocJob
        {
            public readonly string SourceFile;
            public readonly string OutputDirectory;

            public ProtocJob(string sourceFile, string outputDirectory)
            {
                SourceFile = sourceFile;
                OutputDirectory = outputDirectory;
            }
        }
    }
}
