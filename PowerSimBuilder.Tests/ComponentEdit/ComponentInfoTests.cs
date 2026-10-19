using Xunit;
using PowerSimBuilder;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    [Collection(I18nEnglishCollection.Name)]
    public class ComponentInfoTests
    {
        [Fact]
        public void SourceLabel_EnglishResources()
        {
            I18n.SetLanguage("en");
            Assert.Equal("bundled",  new ComponentInfo { Source = ComponentSourceKind.Bundled  }.SourceLabel);
            Assert.Equal("override", new ComponentInfo { Source = ComponentSourceKind.Override }.SourceLabel);
            Assert.Equal("adapter",  new ComponentInfo { Source = ComponentSourceKind.Adapter  }.SourceLabel);
        }

        [Fact]
        public void SourceLabel_ChineseResources()
        {
            I18n.SetLanguage("zh");
            try
            {
                Assert.Equal("内置",   new ComponentInfo { Source = ComponentSourceKind.Bundled  }.SourceLabel);
                Assert.Equal("覆盖",   new ComponentInfo { Source = ComponentSourceKind.Override }.SourceLabel);
                Assert.Equal("适配器", new ComponentInfo { Source = ComponentSourceKind.Adapter  }.SourceLabel);
            }
            finally { I18n.SetLanguage("en"); }
        }

        [Fact]
        public void IsEditable_OnlyOverrideAndAdapter()
        {
            Assert.False(new ComponentInfo { Source = ComponentSourceKind.Bundled }.IsEditable);
            Assert.True(new ComponentInfo { Source = ComponentSourceKind.Override }.IsEditable);
            Assert.True(new ComponentInfo { Source = ComponentSourceKind.Adapter }.IsEditable);
        }
    }
}