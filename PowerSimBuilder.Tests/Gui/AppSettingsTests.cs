using System;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    public class AppSettingsTests
    {
        [Fact]
        public void AppSettings_TypeIsPresent()
        {
            var t = typeof(AppSettings);
            Assert.Equal("PowerSimBuilder.AppSettings", t.FullName);
            Assert.True(t.IsAbstract && t.IsSealed);
        }

        [Fact]
        public void AppSettings_HasExpectedMembers()
        {
            var t = typeof(AppSettings);
            Assert.NotNull(t.GetProperty("Language"));
            Assert.NotNull(t.GetMethod("Load"));
            Assert.NotNull(t.GetMethod("Save"));
            Assert.NotNull(t.GetMethod("DetectOsLanguage"));
            Assert.NotNull(t.GetMethod("ResolveStartupLanguage"));
        }

        [Fact]
        public void I18n_SetLanguage_Zh_LoadsChinese()
        {
            var original = I18n.CurrentLanguage;
            try
            {
                I18n.SetLanguage("zh");
                Assert.Equal("zh", I18n.CurrentLanguage);
                var ok = I18n.T("Common.BtnOK");
                Assert.NotEqual("[Common.BtnOK]", ok);
                Assert.Contains("确定", ok);
            }
            finally
            {
                I18n.SetLanguage(original);
            }
        }

        [Fact]
        public void I18n_EnAndZh_AreDistinct()
        {
            var original = I18n.CurrentLanguage;
            try
            {
                I18n.SetLanguage("en");
                var enOk = I18n.T("Common.BtnOK");
                I18n.SetLanguage("zh");
                var zhOk = I18n.T("Common.BtnOK");
                Assert.NotEqual(enOk, zhOk);
            }
            finally
            {
                I18n.SetLanguage(original);
            }
        }
        [Fact]
        public void AppSettings_HasDownloadSettings()
        {
            var t = typeof(AppSettings);
            Assert.NotNull(t.GetProperty("DownloadTimeoutSeconds"));
            Assert.NotNull(t.GetProperty("ProxyHost"));
            Assert.NotNull(t.GetProperty("ProxyPort"));
            Assert.NotNull(t.GetProperty("AllowedUrls"));
        }

        [Fact]
        public void AppSettings_DownloadDefaults_AreSensible()
        {
            Assert.True(AppSettings.DownloadTimeoutSeconds >= 1);
            Assert.NotNull(AppSettings.ProxyHost);
            Assert.NotNull(AppSettings.AllowedUrls);
        }
    }
}
