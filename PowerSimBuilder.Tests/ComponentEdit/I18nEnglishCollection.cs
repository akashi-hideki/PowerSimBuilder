using System;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    /// <summary>
    /// Ensures tests that depend on I18n.T run under a known language.
    /// Attach to test classes with: [Collection("I18n-English")]
    /// </summary>
    public sealed class I18nEnglishCollection : IDisposable
    {
        public const string Name = "I18n-English";

        public I18nEnglishCollection()
        {
            I18n.SetLanguage("en");
        }

        public void Dispose()
        {
            I18n.SetLanguage("en");
        }
    }

    [CollectionDefinition(I18nEnglishCollection.Name)]
    public class I18nEnglishCollectionDefinition : ICollectionFixture<I18nEnglishCollection> { }
}