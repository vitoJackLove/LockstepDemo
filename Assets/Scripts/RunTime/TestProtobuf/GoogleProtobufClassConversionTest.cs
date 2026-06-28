using System;
using System.Collections.Generic;
using Google.Protobuf;
using Proto = Rogue.TestProtobuf.Proto;

namespace Rogue.TestProtobuf
{
    [Serializable]
    public class TestItemData
    {
        public int ItemId;
        public string ItemName;
        public int Count;
    }

    [Serializable]
    public class TestPlayerProfileData
    {
        public int PlayerId;
        public string PlayerName;
        public int Level;
        public bool IsOnline;
        public long Gold;
        public List<TestItemData> Items = new List<TestItemData>();
        public Dictionary<string, int> Attributes = new Dictionary<string, int>();
    }

    public readonly struct GoogleProtobufClassConversionTestResult
    {
        public readonly int ByteCount;
        public readonly int ItemCount;
        public readonly int AttributeCount;

        public GoogleProtobufClassConversionTestResult(int byteCount, int itemCount, int attributeCount)
        {
            ByteCount = byteCount;
            ItemCount = itemCount;
            AttributeCount = attributeCount;
        }
    }

    public static class GoogleProtobufClassConversionTest
    {
        public static GoogleProtobufClassConversionTestResult RunRoundTrip()
        {
            TestPlayerProfileData source = CreateSampleData();

            Proto.TestPlayerProfileProto protoMessage = ToProto(source);
            byte[] bytes = protoMessage.ToByteArray();

            Proto.TestPlayerProfileProto decodedProto = Proto.TestPlayerProfileProto.Parser.ParseFrom(bytes);
            TestPlayerProfileData decodedData = FromProto(decodedProto);

            ValidateEqual(source, decodedData);

            return new GoogleProtobufClassConversionTestResult(
                bytes.Length,
                decodedData.Items.Count,
                decodedData.Attributes.Count);
        }

        public static Proto.TestPlayerProfileProto ToProto(TestPlayerProfileData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Proto.TestPlayerProfileProto proto = new Proto.TestPlayerProfileProto
            {
                PlayerId = data.PlayerId,
                PlayerName = data.PlayerName ?? string.Empty,
                Level = data.Level,
                IsOnline = data.IsOnline,
                Gold = data.Gold,
            };

            if (data.Items != null)
            {
                for (int i = 0; i < data.Items.Count; i++)
                {
                    proto.Items.Add(ToProto(data.Items[i]));
                }
            }

            if (data.Attributes != null)
            {
                foreach (KeyValuePair<string, int> attribute in data.Attributes)
                {
                    proto.Attributes[attribute.Key] = attribute.Value;
                }
            }

            return proto;
        }

        public static TestPlayerProfileData FromProto(Proto.TestPlayerProfileProto proto)
        {
            if (proto == null)
            {
                throw new ArgumentNullException(nameof(proto));
            }

            TestPlayerProfileData data = new TestPlayerProfileData
            {
                PlayerId = proto.PlayerId,
                PlayerName = proto.PlayerName,
                Level = proto.Level,
                IsOnline = proto.IsOnline,
                Gold = proto.Gold,
            };

            for (int i = 0; i < proto.Items.Count; i++)
            {
                data.Items.Add(FromProto(proto.Items[i]));
            }

            foreach (KeyValuePair<string, int> attribute in proto.Attributes)
            {
                data.Attributes[attribute.Key] = attribute.Value;
            }

            return data;
        }

        private static Proto.TestItemProto ToProto(TestItemData data)
        {
            if (data == null)
            {
                return new Proto.TestItemProto();
            }

            return new Proto.TestItemProto
            {
                ItemId = data.ItemId,
                ItemName = data.ItemName ?? string.Empty,
                Count = data.Count,
            };
        }

        private static TestItemData FromProto(Proto.TestItemProto proto)
        {
            return new TestItemData
            {
                ItemId = proto.ItemId,
                ItemName = proto.ItemName,
                Count = proto.Count,
            };
        }

        private static TestPlayerProfileData CreateSampleData()
        {
            return new TestPlayerProfileData
            {
                PlayerId = 10001,
                PlayerName = "ProtobufTester",
                Level = 9,
                IsOnline = true,
                Gold = 123456789L,
                Items =
                {
                    new TestItemData { ItemId = 1, ItemName = "Sword", Count = 1 },
                    new TestItemData { ItemId = 2, ItemName = "Potion", Count = 8 },
                },
                Attributes =
                {
                    ["attack"] = 120,
                    ["defense"] = 45,
                    ["speed"] = 7,
                },
            };
        }

        private static void ValidateEqual(TestPlayerProfileData expected, TestPlayerProfileData actual)
        {
            AssertEqual(expected.PlayerId, actual.PlayerId, nameof(expected.PlayerId));
            AssertEqual(expected.PlayerName, actual.PlayerName, nameof(expected.PlayerName));
            AssertEqual(expected.Level, actual.Level, nameof(expected.Level));
            AssertEqual(expected.IsOnline, actual.IsOnline, nameof(expected.IsOnline));
            AssertEqual(expected.Gold, actual.Gold, nameof(expected.Gold));
            AssertEqual(expected.Items.Count, actual.Items.Count, nameof(expected.Items));
            AssertEqual(expected.Attributes.Count, actual.Attributes.Count, nameof(expected.Attributes));

            for (int i = 0; i < expected.Items.Count; i++)
            {
                AssertEqual(expected.Items[i].ItemId, actual.Items[i].ItemId, $"{nameof(expected.Items)}[{i}].{nameof(TestItemData.ItemId)}");
                AssertEqual(expected.Items[i].ItemName, actual.Items[i].ItemName, $"{nameof(expected.Items)}[{i}].{nameof(TestItemData.ItemName)}");
                AssertEqual(expected.Items[i].Count, actual.Items[i].Count, $"{nameof(expected.Items)}[{i}].{nameof(TestItemData.Count)}");
            }

            foreach (KeyValuePair<string, int> attribute in expected.Attributes)
            {
                if (!actual.Attributes.TryGetValue(attribute.Key, out int actualValue))
                {
                    throw new InvalidOperationException($"Missing attribute: {attribute.Key}");
                }

                AssertEqual(attribute.Value, actualValue, $"Attribute[{attribute.Key}]");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string fieldName)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{fieldName} mismatch. Expected: {expected}, Actual: {actual}");
            }
        }
    }
}
