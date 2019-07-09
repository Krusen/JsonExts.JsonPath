using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace JsonExts.JsonPath.Tests
{
    public class JsonPathObjectConverterTests
    {
        private JsonSerializer Serializer { get; }

        public JsonPathObjectConverterTests()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters =
                {
                    new JsonPathObjectConverter()
                }
            };
            Serializer = JsonSerializer.CreateDefault(settings);
        }

        private T Deserialize<T>(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                return Serializer.Deserialize<T>(reader);
            }
        }

        private string Serialize(object value)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                Serializer.Serialize(jsonWriter, value);
                return writer.ToString();
            }
        }

        #region Deserialize Tests

        [Fact]
        public void Deserialize_SimpleStub()
        {
            var obj = Deserialize<SimpleStub>(SimpleStub.Json);
            obj.Value.Should().Be(1);
            obj.MainValue.Should().Be(2);
            obj.NestedValue.Should().Be(3);

            obj.NestedStub.Should().NotBeNull();
            obj.NestedStub.Value.Should().Be(3);
            obj.NestedStub.NormalValue.Should().Be(4);

            obj.NormalValue.Should().Be(5);
        }

        [Fact]
        public void Deserialize_SimpleStubWithJsonConverterAttribute()
        {
            var obj = JsonConvert.DeserializeObject<SimpleStubWithConverterAttribute>(SimpleStubWithConverterAttribute.Json);
            obj.Value.Should().Be(1);
            obj.MainValue.Should().Be(2);
            obj.NestedValue.Should().Be(3);

            obj.NestedStub.Should().NotBeNull();
            obj.NestedStub.Value.Should().Be(3);
            obj.NestedStub.NormalValue.Should().Be(4);

            obj.NormalValue.Should().Be(5);
        }

        [Fact]
        public void Deserialize_ListTypes_ArrayOfObjects()
        {
            var obj = Deserialize<ListTypesStub>(ListTypesStub.Json);
            obj.Objects.Should().HaveCount(3);
            obj.Objects.Should().NotContainNulls();
        }

        [Fact]
        public void Deserialize_ListTypes_Array()
        {
            var obj = Deserialize<ListTypesStub>(ListTypesStub.Json);
            obj.ValuesArray.Should().HaveCount(3);
        }

        [Fact]
        public void Deserialize_ListTypes_IEnumerable()
        {
            var obj = Deserialize<ListTypesStub>(ListTypesStub.Json);
            obj.ValuesEnumerable.Should().HaveCount(3);
        }

        [Fact]
        public void Deserialize_ListTypes_ArrayOfArrays()
        {
            var obj = Deserialize<ListTypesStub>(ListTypesStub.Json);
            obj.ArrayOfArrays.Should().HaveCount(2);
            obj.ArrayOfArrays[0].Should().HaveCount(3);
            obj.ArrayOfArrays[1].Should().HaveCount(1);
        }

        [Fact]
        public void Deserialize_Dictionary()
        {
            var obj = Deserialize<DictionaryPropertyStub>(DictionaryPropertyStub.Json);
            obj.Data.Should().HaveCount(2);
            obj.Data["title"].Should().Be("title");
            obj.Data["author"].Should().Be("author");
        }

        [Fact]
        public void Deserialize_ArraySingleSelection()
        {
            var obj = Deserialize<ArraySingleSelectionStub>(ArraySingleSelectionStub.Json);
            obj.Value1.Should().Be(1);
            obj.Value2.Should().Be(2);
            obj.Value3.Should().Be(3);
            obj.ValueMissing.Should().Be(null);
        }

        [Fact]
        public void Deserialize_ArrayScriptExpression()
        {
            var obj = Deserialize<ScriptExpressionStub>(ScriptExpressionStub.Json);
            obj.Filtered.Should().HaveCount(2);
            obj.Filtered[0].Points.Should().Be(11);
            obj.Filtered[1].Points.Should().Be(16);
        }

        [Fact]
        public void Deserialize_DescendantMatch()
        {
            var obj = Deserialize<DescendantMatchStub>(DescendantMatchStub.Json);
            obj.Names.Should().HaveCount(3);
            obj.Names[0].Should().Be("p1");
            obj.Names[1].Should().Be("t1");
            obj.Names[2].Should().Be("root");
        }

        [Fact]
        public void Deserialize_SamePathMoreThanOnce()
        {
            var obj = Deserialize<SamePathMoreThanOnceStub>(SamePathMoreThanOnceStub.Json);
            obj.TeamName1.Should().Be("team1");
            obj.TeamName2.Should().Be("team1");
        }

        [Fact]
        public void Deserialize_CustomConverter()
        {
            var obj = Deserialize<CustomConverterStub>(CustomConverterStub.Json);
            obj.PathProperty.Should().Be("custom-1");
        }

        [Fact]
        public void Deserialize_CustomConverterOnNormalProperty()
        {
            var obj = Deserialize<CustomConverterStub>(CustomConverterStub.Json);
            obj.NormalProperty.Should().Be("custom");
        }

        [Fact]
        public void Deserialize_CustomItemConverter_ListNonGeneric()
        {
            var obj = Deserialize<CustomItemConverterStub>(CustomItemConverterStub.Json);
            obj.NamesListNonGeneric.Should().HaveCount(2);
            obj.NamesListNonGeneric[0].Should().Be("item");
            obj.NamesListNonGeneric[1].Should().Be("item");
        }

        [Fact]
        public void Deserialize_CustomItemConverter_List()
        {
            var obj = Deserialize<CustomItemConverterStub>(CustomItemConverterStub.Json);
            obj.NamesList.Should().HaveCount(2);
            obj.NamesList[0].Should().Be("item");
            obj.NamesList[1].Should().Be("item");
        }

        [Fact]
        public void Deserialize_CustomItemConverter_IEnumerable()
        {
            var obj = Deserialize<CustomItemConverterStub>(CustomItemConverterStub.Json);
            obj.NamesEnumerable.Should().HaveCount(2);
            obj.NamesEnumerable.ElementAt(0).Should().Be("item");
            obj.NamesEnumerable.ElementAt(1).Should().Be("item");
        }

        [Fact]
        public void Deserialize_CustomItemConverter_Array()
        {
            var obj = Deserialize<CustomItemConverterStub>(CustomItemConverterStub.Json);
            obj.NamesArray.Should().HaveCount(2);
            obj.NamesArray[0].Should().Be("item");
            obj.NamesArray[1].Should().Be("item");
        }

        [Fact]
        public void Deserialize_CustomItemConverter_ReadOnlyList()
        {
            var obj = Deserialize<CustomItemConverterStub>(CustomItemConverterStub.Json);
            obj.NamesReadOnly.Should().HaveCount(2);
            obj.NamesReadOnly[0].Should().Be("item");
            obj.NamesReadOnly[1].Should().Be("item");
        }

        [Fact]
        public void Deserialize_PassThrough()
        {
            var obj = Deserialize<PassThroughStub>(PassThroughStub.Json);
            obj.Sub.Should().NotBeNull();
            obj.Sub.Value.Should().Be(42);
        }

        [Fact]
        public void Deserialize_NonJsonObjectContract()
        {

            Action act = () => Deserialize<NonJsonObjectContractStub>(NonJsonObjectContractStub.Json);

            act.Should().Throw<JsonSerializationException>().WithMessage("Unexpected contract type*");
        }

        [Fact]
        public void Deserialize_EmptyJson_ShouldBeNull()
        {
            var obj = Deserialize<SimpleStub>("");
            obj.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyObjectJson_ShouldNotBeNull()
        {
            var obj = Deserialize<SimpleStub>("{ }");
            obj.Should().NotBeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayJson_ShouldThrow()
        {
            Action act = () => Deserialize<SimpleStub>("[]");
            act.Should().Throw<JsonSerializationException>().WithMessage("Unexpected token*");
        }

        [Fact]
        public void Deserialize_DefaultValueHandling_PopulateOnProperty()
        {
            var obj = Deserialize<DefaultValueHandlingStub>("{}");
            obj.Value.Should().Be(1);
            obj.NormalValue.Should().Be(2);
        }

        [Fact]
        public void Deserialize_DefaultValueHandling_PopulateFromSettings()
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Populate,
                Converters = {new JsonPathObjectConverter()}
            };
            var obj = JsonConvert.DeserializeObject<DefaultValueHandlingStub>("{}", settings);
            obj.ValueWithoutPopulateSetting.Should().Be(3);
            obj.NormalValueWithoutPopulateSetting.Should().Be(4);
        }

        [Fact]
        public void Deserialize_DefaultValueHandling_Populate_NonNullable()
        {
            var obj = Deserialize<DefaultValueHandlingStub>("{}");
            obj.NonNullableValue.Should().Be(5);
            obj.NonNullableValueWithoutDefault.Should().Be(0);
        }

        [Fact]
        public void Deserialize_NullValueHandling_FromProperty()
        {
            var obj = new
            {
                Stub = new NullValueHandlingStub
                {
                    ValueIgnored = 1,
                    NormalValueIgnored = 2,
                    ValueIncluded = 3,
                    NormalValueIncluded = 4
                }
            };

            var reader = JObject.Parse(NullValueHandlingStub.Json).CreateReader();
            Serializer.Populate(reader, obj);

            obj.Stub.ValueIgnored.Should().Be(1);
            obj.Stub.NormalValueIgnored.Should().Be(2);
            obj.Stub.ValueIncluded.Should().Be(null);
            obj.Stub.NormalValueIncluded.Should().Be(null);
        }

        [Fact]
        public void Deserialize_NullValueHandling_Ignore_FromSettings()
        {
            var obj = new
            {
                Stub = new NullValueHandlingStub
                {
                    Value = 1,
                    NormalValue = 2
                }
            };

            var reader = JObject.Parse(NullValueHandlingStub.Json).CreateReader();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = {new JsonPathObjectConverter()}
            };
            var serializer = JsonSerializer.CreateDefault(settings);
            serializer.Populate(reader, obj);

            obj.Stub.Value.Should().Be(1);
            obj.Stub.NormalValue.Should().Be(2);
        }

        [Fact]
        public void Deserialize_NullValueHandling_Include_FromSettings()
        {
            var obj = new
            {
                Stub = new NullValueHandlingStub
                {
                    Value = 1,
                    NormalValue = 2
                }
            };

            var reader = JObject.Parse(NullValueHandlingStub.Json).CreateReader();
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Converters = {new JsonPathObjectConverter()}
            };
            var serializer = JsonSerializer.CreateDefault(settings);
            serializer.Populate(reader, obj);

            obj.Stub.Value.Should().Be(null);
            obj.Stub.NormalValue.Should().Be(null);
        }

        #endregion

        #region Serialize Tests

        [Fact]
        public void Serialize_SimpleStub()
        {
            var obj = new SimpleStub
            {
                Value = 9,
                MainValue = 8,
                NestedValue = 7,
                NestedStub = null,
                NormalValue = 6
            };

            var result = Serialize(obj);

            result.Should().NotBeNullOrEmpty();
            result.Should().Be(@"{""value"":9,""mainValue"":8,""nestedValue"":7,""nestedStub"":null,""normalValue"":6}");
        }

        [Fact]
        public void Serialize_SimpleStubWithoutConverterAttribute()
        {
            var obj = new SimpleStubWithConverterAttribute
            {
                Value = 9,
                MainValue = 8,
                NestedValue = 7,
                NestedStub = new SimpleStub { NormalValue = 6 },
                NormalValue = 5
            };

            var settings = new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
            var result = JsonConvert.SerializeObject(obj, settings);

            result.Should().NotBeNullOrEmpty();
            result.Should().Be(@"{""value"":9,""mainValue"":8,""nestedValue"":7,""nestedStub"":{""normalValue"":6},""normalValue"":5}");
        }

        [Fact]
        public void Serialize_CustomConverter()
        {
            var obj = new CustomConverterStub
            {
                PathProperty = "test",
                NormalProperty = "value ignored"
            };

            var result = Serialize(obj);

            result.Should().NotBeNullOrEmpty();
            result.Should().Be(@"{""pathProperty"":""custom-test"",""normalProperty"":""custom""}");
        }

        [Fact]
        public void Serialize_CustomItemConverter()
        {
            var list = new List<string> {"a", "b"};
            var obj = new CustomItemConverterStub
            {
                NamesArray = list.ToArray(),
                NamesEnumerable = list,
                NamesList = list,
                NamesListNonGeneric = list,
                NamesReadOnly = list
            };

            var result = Serialize(obj);

            result.Should().NotBeNullOrEmpty();
            result.Should().Be(@"{""namesArray"":[""item"",""item""],""namesEnumerable"":[""item"",""item""],""namesList"":[""item"",""item""],""namesListNonGeneric"":[""item"",""item""],""namesReadOnly"":[""item"",""item""]}");
        }

        [Fact]
        public void Serialize_NullObjectProperty()
        {
            var obj = new
            {
                Stub = (SimpleStub) null
            };
            var result = Serialize(obj);

            result.Should().Be(@"{""stub"":null}");
        }

        #endregion

        #region Stubs

        private class SimpleStub
        {
            public const string Json = "{ value: 1, main: { value: 2, nested: { value: 3, normalValue: 4 } }, normalValue: 5 }";

            [JsonPath("value")]
            public int Value { get; set; }

            [JsonPath("main.value")]
            public int MainValue { get; set; }

            [JsonPath("main.nested.value")]
            public int NestedValue { get; set; }

            [JsonPath("main.nested")]
            public SimpleStub NestedStub { get; set; }

            public int NormalValue { get; set; }
        }

        [JsonConverter(typeof(JsonPathObjectConverter))]
        private class SimpleStubWithConverterAttribute : SimpleStub
        {
        }

        private class ListTypesStub
        {
            public const string Json = "{ objects: [ {}, {}, { points: 123 } ], values: [ 9, 8, 7 ], arrays: [ { array: [ 1, 2, 3 ] }, { array: [ 4 ] } ] }";

            [JsonPath("objects")]
            public object[] Objects { get; set; }

            [JsonPath("values[*]")]
            public int[] ValuesArray { get; set; }

            [JsonPath("values[*]")]
            public IEnumerable<int> ValuesEnumerable { get; set; }

            [JsonPath("values")]
            public IList<int> ValuesList { get; set; }

            [JsonPath("arrays[*].array")]
            public int[][] ArrayOfArrays { get; set; }
        }

        private class ArraySingleSelectionStub
        {
            public const string Json = "{  list: [ { value: 1 }, { value: 2 }, { value: 3 } ] }";

            [JsonPath("list[0].value")]
            public int Value1 { get; set; }

            [JsonPath("list[1].value")]
            public int Value2 { get; set; }

            [JsonPath("list[2].value")]
            public int Value3 { get; set; }

            [JsonPath("list[99].value")]
            public int? ValueMissing { get; set; }
        }

        private class DictionaryPropertyStub
        {
            public const string Json = "{ book: { data: { title: 'title', author: 'author' } } }";

            [JsonPath("book.data")]
            public IDictionary<string, string> Data { get; set; }
        }

        private class ScriptExpressionStub
        {
            public const string Json = "{ values: [ { points: 5 }, { points: 11 }, { points: 16 } ] }";

            [JsonPath("values[?(@.points > 10)]")]
            public IList<ValueStub> Filtered { get; set; }

            public class ValueStub
            {
                public int Points { get; set; }
            }
        }

        private class DescendantMatchStub
        {
            public const string Json = "{ player: { name: 'p1' }, team: { name: 't1' }, name: 'root' }";

            [JsonPath("..name")]
            public IList<string> Names { get; set; }
        }

        private class SamePathMoreThanOnceStub
        {
            public const string Json = "{ player: { team: 'team1' } }";

            [JsonPath("player.team")]
            public string TeamName1 { get; set; }

            [JsonPath("player.team")]
            public string TeamName2 { get; set; }
        }

        private class CustomConverterStub
        {
            public const string Json = @"{ player: { id: 1 }, normalProperty: 2 }";

            [JsonPath("player.id")]
            [JsonConverter(typeof(CustomCombineConverter))]
            public string PathProperty { get; set; }

            [JsonConverter(typeof(CustomOnlyConverter), "custom")]
            public string NormalProperty { get; set; }
        }

        private class CustomItemConverterStub
        {
            public const string Json = "{ books: [ { author: null }, { author: null } ] }";

            [JsonPath("..author")]
            [JsonProperty(ItemConverterType = typeof(CustomOnlyConverter), ItemConverterParameters = new object[]{ "item" })]
            public string[] NamesArray { get; set; }

            [JsonPath("..author")]
            [JsonProperty(ItemConverterType = typeof(CustomOnlyConverter), ItemConverterParameters = new object[]{ "item" })]
            public IEnumerable<string> NamesEnumerable { get; set; }

            [JsonPath("..author")]
            [JsonProperty(ItemConverterType = typeof(CustomOnlyConverter), ItemConverterParameters = new object[]{ "item" })]
            public IList<string> NamesList { get; set; }

            [JsonPath("..author")]
            [JsonProperty(ItemConverterType = typeof(CustomOnlyConverter), ItemConverterParameters = new object[]{ "item" })]
            public IList NamesListNonGeneric{ get; set; }

            [JsonPath("..author")]
            [JsonProperty(ItemConverterType = typeof(CustomOnlyConverter), ItemConverterParameters = new object[]{ "item" })]
            public IReadOnlyList<string> NamesReadOnly { get; set; }
        }

        private class PassThroughStub
        {
            public const string Json = "{ product: { value: 42 } }";

            [JsonPath("$")]
            public PassThroughSubStub Sub { get; set; }

            public class PassThroughSubStub
            {
                [JsonPath("product.value")]
                public int Value { get; set; }
            }
        }

        private class NonJsonObjectContractStub
        {
            public const string Json = "{}";

            [JsonPath("")]
            [JsonConverter(typeof(JsonPathObjectConverter))]
            public int Value { get; set; }
        }

        private class DefaultValueHandlingStub
        {
            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            [JsonPath("value")]
            public int? Value { get; set; }

            [DefaultValue(2)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int? NormalValue { get; set; }

            [DefaultValue(3)]
            [JsonPath("value")]
            public int ValueWithoutPopulateSetting { get; set; }

            [DefaultValue(4)]
            public int NormalValueWithoutPopulateSetting { get; set; }

            [DefaultValue(5)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            [JsonPath("value")]
            public int NonNullableValue { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            [JsonPath("value")]
            public int NonNullableValueWithoutDefault { get; set; }
        }

        private class NullValueHandlingStub
        {
            public const string Json = "{ stub: { value: null, normalValueIgnored: null, normalValueIncluded: null, normalValue: null } }";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            [JsonPath("value")]
            public int? ValueIgnored { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? NormalValueIgnored { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Include)]
            [JsonPath("value")]
            public int? ValueIncluded { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Include)]
            public int? NormalValueIncluded { get; set; }

            [JsonPath("value")]
            public int? Value { get; set; }

            public int? NormalValue { get; set; }
        }

        #endregion

        #region Custom Converters

        /// <summary>
        /// Custom converter prepending the given custom value to the result.
        /// </summary>
        private class CustomCombineConverter : JsonConverter
        {
            private string CustomValue { get; }

            public CustomCombineConverter() : this("custom-")
            {
            }

            public CustomCombineConverter(string customValue)
            {
                CustomValue = customValue;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, CustomValue + value);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return CustomValue + reader.Value;
            }

            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Custom converter returning only the given custom value, ignoring the actual value.
        /// </summary>
        private class CustomOnlyConverter : JsonConverter
        {
            private string CustomValue { get; }

            public CustomOnlyConverter(string customValue)
            {
                CustomValue = customValue;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, CustomValue);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return CustomValue;
            }

            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
