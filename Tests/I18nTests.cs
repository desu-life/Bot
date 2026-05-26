using KanonBot.Drivers;
using KanonBot.I18n;

namespace Tests;

public class I18nTests
{
    private static TranslationStore CreateTestStore()
    {
        var store = new TranslationStore();
        store.LoadDirect(Locale.ZhCN, new Dictionary<string, string>
        {
            ["error.timeout"] = "获取数据超时，请稍后重试吧",
            ["error.user_not_found"] = "猫猫没有找到此用户。",
            ["account.not_bound"] = "你还没有绑定 desu.life 账户，请使用 {prefix}bind 进行绑定。",
            ["help.main"] = "用户查询：\n{prefix}info/recent/bp/get",
            ["osu.bp_too_few_other"] = "{0}的bp太少啦，请让ta多打些吧",
            ["osu.cost_result"] = "在ONC中，{0} 的cost为：{1}",
        });
        store.LoadDirect(Locale.En, new Dictionary<string, string>
        {
            ["error.timeout"] = "Request timed out, please try again later.",
            ["error.user_not_found"] = "User not found.",
            ["account.not_bound"] = "You haven't linked your desu.life account yet. Use {prefix}bind to link.",
            ["help.main"] = "User queries:\n{prefix}info/recent/bp/get",
            ["osu.bp_too_few_other"] = "{0}'s BP list is too short.",
            ["osu.cost_result"] = "In ONC, {0}'s cost is: {1}",
        });
        return store;
    }

    // ─── TranslationStore Tests ──────────────────────────

    [Fact]
    public void Store_Get_ExistingKey()
    {
        var store = CreateTestStore();
        var value = store.Get("error.timeout", Locale.ZhCN);

        Assert.Equal("获取数据超时，请稍后重试吧", value);
    }

    [Fact]
    public void Store_Get_EnglishKey()
    {
        var store = CreateTestStore();
        var value = store.Get("error.timeout", Locale.En);

        Assert.Equal("Request timed out, please try again later.", value);
    }

    [Fact]
    public void Store_Get_MissingKey_FallbackToZhCN()
    {
        var store = CreateTestStore();
        // "error.user_not_found" exists in both; but let's test a key missing from En
        store.LoadDirect(Locale.En, new Dictionary<string, string>
        {
            ["error.timeout"] = "Timed out.",
        });

        var value = store.Get("error.user_not_found", Locale.En);

        // Falls back to zh-CN
        Assert.Equal("猫猫没有找到此用户。", value);
    }

    [Fact]
    public void Store_Get_MissingKey_ReturnsNull()
    {
        var store = CreateTestStore();
        var value = store.Get("nonexistent.key", Locale.ZhCN);

        Assert.Null(value);
    }

    [Fact]
    public void Store_HasKey()
    {
        var store = CreateTestStore();

        Assert.True(store.HasKey("error.timeout"));
        Assert.False(store.HasKey("nonexistent.key"));
    }

    [Fact]
    public void Store_LoadedLocales()
    {
        var store = CreateTestStore();
        var locales = store.LoadedLocales.ToList();

        Assert.Contains(Locale.ZhCN, locales);
        Assert.Contains(Locale.En, locales);
    }

    // ─── LocaleResolver Tests ──────────────────────────

    [Fact]
    public void Resolver_PlatformDefault_Discord()
    {
        Assert.Equal(Locale.En, LocaleResolver.GetPlatformDefault(Platform.Discord));
    }

    [Fact]
    public void Resolver_PlatformDefault_OneBot()
    {
        Assert.Equal(Locale.ZhCN, LocaleResolver.GetPlatformDefault(Platform.OneBot));
    }

    [Fact]
    public void Resolver_PlatformDefault_Guild()
    {
        Assert.Equal(Locale.ZhCN, LocaleResolver.GetPlatformDefault(Platform.Guild));
    }

    [Fact]
    public void Resolver_PlatformPrefix_Discord()
    {
        Assert.Equal("/", LocaleResolver.GetPlatformPrefix(Platform.Discord));
    }

    [Fact]
    public void Resolver_PlatformPrefix_OneBot()
    {
        Assert.Equal("!", LocaleResolver.GetPlatformPrefix(Platform.OneBot));
    }

    [Fact]
    public void Resolver_Sync_NullUser_ReturnsPlatformDefault()
    {
        var resolver = new LocaleResolver();
        var locale = resolver.Resolve(null, Platform.Discord);

        Assert.Equal(Locale.En, locale);
    }

    [Fact]
    public async Task Resolver_Async_NullUser_ReturnsPlatformDefault()
    {
        var resolver = new LocaleResolver();
        var locale = await resolver.ResolveAsync(null, Platform.OneBot);

        Assert.Equal(Locale.ZhCN, locale);
    }

    [Fact]
    public async Task Resolver_CustomProvider()
    {
        var provider = new TestLocaleProvider(Locale.En);
        var resolver = new LocaleResolver(provider);

        // User has English preference set
        var locale = await resolver.ResolveAsync("user123", Platform.OneBot);

        Assert.Equal(Locale.En, locale);
    }

    [Fact]
    public async Task Resolver_CacheWorks()
    {
        var provider = new CountingLocaleProvider(Locale.En);
        var resolver = new LocaleResolver(provider);

        // First call should query provider
        await resolver.ResolveAsync("user1", Platform.Discord);
        Assert.Equal(1, provider.CallCount);

        // Second call should use cache
        var locale = resolver.Resolve("user1", Platform.Discord);
        Assert.Equal(Locale.En, locale);
        Assert.Equal(1, provider.CallCount); // Not incremented
    }

    [Fact]
    public void Resolver_InvalidateCache()
    {
        var resolver = new LocaleResolver();

        // Populate cache
        resolver.Resolve("user1", Platform.Discord);

        // Invalidate
        resolver.InvalidateCache("user1", Platform.Discord);

        // Should not crash, returns platform default since provider returns null
        var locale = resolver.Resolve("user1", Platform.Discord);
        Assert.Equal(Locale.En, locale);
    }

    // ─── Localizer Tests ──────────────────────────────

    [Fact]
    public void Localizer_Get_Basic()
    {
        var store = CreateTestStore();
        var localizer = new Localizer(store, new LocaleResolver());

        var result = localizer.Get("error.timeout", Locale.ZhCN);

        Assert.Equal("获取数据超时，请稍后重试吧", result);
    }

    [Fact]
    public void Localizer_Get_WithFormatArgs()
    {
        var store = CreateTestStore();
        var localizer = new Localizer(store, new LocaleResolver());

        var result = localizer.Get("osu.cost_result", Locale.ZhCN, "TestUser", 42);

        Assert.Equal("在ONC中，TestUser 的cost为：42", result);
    }

    [Fact]
    public void Localizer_Get_MissingKey_ReturnsKey()
    {
        var store = CreateTestStore();
        var localizer = new Localizer(store, new LocaleResolver());

        var result = localizer.Get("missing.key", Locale.ZhCN);

        Assert.Equal("missing.key", result);
    }

    [Fact]
    public void Localizer_GetWithPlatform_PrefixReplacement()
    {
        var store = CreateTestStore();
        var localizer = new Localizer(store, new LocaleResolver());

        var result = localizer.GetWithPlatform("account.not_bound", Locale.ZhCN, Platform.OneBot);
        Assert.Contains("!bind", result);

        var resultDiscord = localizer.GetWithPlatform("account.not_bound", Locale.En, Platform.Discord);
        Assert.Contains("/bind", resultDiscord);
    }

    [Fact]
    public void Localizer_GetWithPlatform_HelpText()
    {
        var store = CreateTestStore();
        var localizer = new Localizer(store, new LocaleResolver());

        // QQ should use ! prefix and Chinese
        var zhResult = localizer.GetWithPlatform("help.main", Locale.ZhCN, Platform.OneBot);
        Assert.Contains("!info", zhResult);
        Assert.Contains("用户查询", zhResult);

        // Discord should use / prefix and English
        var enResult = localizer.GetWithPlatform("help.main", Locale.En, Platform.Discord);
        Assert.Contains("/info", enResult);
        Assert.Contains("User queries", enResult);
    }

    [Fact]
    public void Localizer_GetWithPlatform_FormatAndPrefix()
    {
        var store = CreateTestStore();
        store.LoadDirect(Locale.ZhCN, new Dictionary<string, string>
        {
            ["test.combined"] = "使用 {prefix}cmd 来执行，结果: {0}",
        });
        var localizer = new Localizer(store, new LocaleResolver());

        var result = localizer.GetWithPlatform("test.combined", Locale.ZhCN, Platform.OneBot, "成功");
        Assert.Equal("使用 !cmd 来执行，结果: 成功", result);
    }

    // ─── Locale Extensions Tests ──────────────────────────

    [Fact]
    public void LocaleExtensions_ToCode()
    {
        Assert.Equal("zh-CN", Locale.ZhCN.ToCode());
        Assert.Equal("en", Locale.En.ToCode());
    }

    [Fact]
    public void LocaleExtensions_ParseLocale()
    {
        Assert.Equal(Locale.ZhCN, LocaleExtensions.ParseLocale("zh-CN"));
        Assert.Equal(Locale.ZhCN, LocaleExtensions.ParseLocale("zh"));
        Assert.Equal(Locale.ZhCN, LocaleExtensions.ParseLocale("zh_cn"));
        Assert.Equal(Locale.En, LocaleExtensions.ParseLocale("en"));
        Assert.Equal(Locale.En, LocaleExtensions.ParseLocale("en-US"));
        Assert.Equal(Locale.En, LocaleExtensions.ParseLocale("en-GB"));
        Assert.Equal(Locale.ZhCN, LocaleExtensions.ParseLocale("unknown")); // default fallback
    }

    // ─── Test Helpers ──────────────────────────────

    private class TestLocaleProvider : IUserLocaleProvider
    {
        private readonly Locale _locale;
        public TestLocaleProvider(Locale locale) => _locale = locale;
        public Task<Locale?> GetUserLocaleAsync(string userId, Platform platform)
            => Task.FromResult<Locale?>(_locale);
    }

    private class CountingLocaleProvider : IUserLocaleProvider
    {
        private readonly Locale _locale;
        public int CallCount { get; private set; }
        public CountingLocaleProvider(Locale locale) => _locale = locale;
        public Task<Locale?> GetUserLocaleAsync(string userId, Platform platform)
        {
            CallCount++;
            return Task.FromResult<Locale?>(_locale);
        }
    }
}
