using System.Text.Json;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm
{
    public class ParameterTypeHelperTests
    {
        [Theory]
        [InlineData("int")] [InlineData("integer")] [InlineData("long")]
        [InlineData("short")] [InlineData("byte")] [InlineData("uint")]
        [InlineData("uint8_t")] [InlineData("uint32_t")] [InlineData("volatile uint32_t")]
        [InlineData("System.Int32")] [InlineData("System.Int64")]
        public void Classify_Integer(string type)
        {
            Assert.Equal(ParameterTypeCategory.Integer, ParameterTypeHelper.Classify(type, 0L));
        }

        [Theory]
        [InlineData("double")] [InlineData("float")] [InlineData("decimal")]
        [InlineData("single")] [InlineData("real")] [InlineData("number")]
        [InlineData("numeric")] [InlineData("System.Double")]
        public void Classify_Number(string type)
        {
            Assert.Equal(ParameterTypeCategory.Number, ParameterTypeHelper.Classify(type, 0.0));
        }

        [Theory]
        [InlineData("bool")] [InlineData("boolean")] [InlineData("bit")]
        [InlineData("flag")] [InlineData("System.Boolean")]
        public void Classify_Boolean(string type)
        {
            Assert.Equal(ParameterTypeCategory.Boolean, ParameterTypeHelper.Classify(type, true));
        }

        [Theory]
        [InlineData("string")] [InlineData("text")] [InlineData("char")]
        [InlineData("System.String")]
        public void Classify_String(string type)
        {
            Assert.Equal(ParameterTypeCategory.String, ParameterTypeHelper.Classify(type, "x"));
        }

        [Fact] public void Classify_EmptyType_Double_Number() =>
            Assert.Equal(ParameterTypeCategory.Number, ParameterTypeHelper.Classify("", 1.5));

        [Fact] public void Classify_EmptyType_Long_Integer() =>
            Assert.Equal(ParameterTypeCategory.Integer, ParameterTypeHelper.Classify("", 42L));

        [Fact] public void Classify_EmptyType_Int_Integer() =>
            Assert.Equal(ParameterTypeCategory.Integer, ParameterTypeHelper.Classify("", 42));

        [Fact] public void Classify_EmptyType_Bool_Boolean() =>
            Assert.Equal(ParameterTypeCategory.Boolean, ParameterTypeHelper.Classify("", true));

        [Fact] public void Classify_EmptyType_String_String() =>
            Assert.Equal(ParameterTypeCategory.String, ParameterTypeHelper.Classify("", "abc"));

        [Fact] public void Classify_NullType_Double_Number() =>
            Assert.Equal(ParameterTypeCategory.Number, ParameterTypeHelper.Classify(null, 2.5));

        [Fact] public void Classify_UnknownType_Double_Number() =>
            Assert.Equal(ParameterTypeCategory.Number, ParameterTypeHelper.Classify("custom_thing", 3.14));

        [Fact] public void Classify_NullValue_EmptyType_String() =>
            Assert.Equal(ParameterTypeCategory.String, ParameterTypeHelper.Classify("", null));

        [Fact]
        public void Classify_JsonNumber_Number()
        {
            using (var doc = JsonDocument.Parse("3.14"))
                Assert.Equal(ParameterTypeCategory.Number,
                    ParameterTypeHelper.Classify("", doc.RootElement));
        }

        [Fact]
        public void Classify_JsonTrue_Boolean()
        {
            using (var doc = JsonDocument.Parse("true"))
                Assert.Equal(ParameterTypeCategory.Boolean,
                    ParameterTypeHelper.Classify("", doc.RootElement));
        }

        [Fact]
        public void Classify_JsonString_String()
        {
            using (var doc = JsonDocument.Parse("\"hello\""))
                Assert.Equal(ParameterTypeCategory.String,
                    ParameterTypeHelper.Classify("", doc.RootElement));
        }

        [Theory]
        [InlineData("123", 123L)]
        [InlineData("-5", -5L)]
        [InlineData("0", 0L)]
        [InlineData("2147483648", 2147483648L)]
        public void ParseText_Integer(string text, long expected)
        {
            var result = ParameterTypeHelper.ParseText(text, ParameterTypeCategory.Integer);
            Assert.IsType<long>(result);
            Assert.Equal(expected, (long)result);
        }

        [Fact] public void ParseText_Integer_Throws_OnInvalid() =>
            Assert.Throws<System.FormatException>(
                () => ParameterTypeHelper.ParseText("abc", ParameterTypeCategory.Integer));

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData("-3.14", -3.14)]
        [InlineData("100", 100.0)]
        [InlineData("0.001", 0.001)]
        public void ParseText_Number(string text, double expected)
        {
            var result = ParameterTypeHelper.ParseText(text, ParameterTypeCategory.Number);
            Assert.IsType<double>(result);
            Assert.Equal(expected, (double)result, 6);
        }

        [Fact] public void ParseText_Number_Throws_OnInvalid() =>
            Assert.Throws<System.FormatException>(
                () => ParameterTypeHelper.ParseText("abc", ParameterTypeCategory.Number));

        [Theory]
        [InlineData("true", true)] [InlineData("True", true)] [InlineData("TRUE", true)]
        [InlineData("1", true)] [InlineData("false", false)] [InlineData("0", false)]
        [InlineData("", false)]
        public void ParseText_Boolean(string text, bool expected)
        {
            var result = ParameterTypeHelper.ParseText(text, ParameterTypeCategory.Boolean);
            Assert.IsType<bool>(result);
            Assert.Equal(expected, (bool)result);
        }

        [Theory]
        [InlineData("hello", "hello")] [InlineData("", "")] [InlineData("123", "123")]
        public void ParseText_String(string text, string expected)
        {
            var result = ParameterTypeHelper.ParseText(text, ParameterTypeCategory.String);
            Assert.IsType<string>(result);
            Assert.Equal(expected, (string)result);
        }

        [Fact] public void ParseText_Null_Handled()
        {
            var result = ParameterTypeHelper.ParseText(null, ParameterTypeCategory.String);
            Assert.IsType<string>(result);
            Assert.Equal("", (string)result);
        }
    }
}