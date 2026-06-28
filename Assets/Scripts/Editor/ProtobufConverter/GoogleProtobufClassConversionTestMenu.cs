using Rogue.TestProtobuf;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor
{
    public static class GoogleProtobufClassConversionTestMenu
    {
        [MenuItem("Tools/Protobuf Converter/Run Google.Protobuf Class Conversion Test", false, 120)]
        public static void Run()
        {
            GoogleProtobufClassConversionTestResult result = GoogleProtobufClassConversionTest.RunRoundTrip();
            Debug.Log(
                $"[Google.Protobuf Test] Class -> proto -> bytes -> proto -> class succeeded. " +
                $"Bytes={result.ByteCount}, Items={result.ItemCount}, Attributes={result.AttributeCount}");
        }
    }
}
