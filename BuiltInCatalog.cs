namespace DailyAnimeWallpaper;

/// <summary>Builds the default catalog of verified anime, waifu, booru, and ACG wallpaper APIs.</summary>
internal static class BuiltInCatalog
{
    /// <summary>Creates fresh instances of all shipped anime wallpaper API sources and their subcategories.</summary>
    /// <returns>The ordered list of built-in providers.</returns>
    public static List<WallpaperProvider> CreateDefaultProviders() =>
    [
        CreateJsonProvider(
            id: "waifu-best",
            name: "Waifu.best",
            jsonPath: "items[*].url",
            subcategories:
            [
                Sfw("general", "General", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False"),
                Sfw("waifu", "Waifu", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=waifu"),
                Sfw("maid", "Maid", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=maid"),
                Sfw("uniform", "Uniform", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=uniform"),
                Sfw("selfies", "Selfies", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=selfies"),
                Sfw("genshin-impact", "Genshin Impact", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=genshin-impact"),
                Sfw("marin-kitagawa", "Marin Kitagawa", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=marin-kitagawa"),
                Sfw("raiden-shogun", "Raiden Shogun", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=raiden-shogun"),
                Sfw("mori-calliope", "Mori Calliope", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=mori-calliope"),
                Sfw("kamisato-ayaka", "Kamisato Ayaka", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=kamisato-ayaka"),
                Sfw("rem", "Rem", "https://api.waifu.best/images?IsNsfw=False&IsAnimated=False&IncludedTags=rem"),
                Nsfw("nsfw-general", "NSFW General", "https://api.waifu.best/images?IsNsfw=True&IsAnimated=False"),
                Nsfw("ecchi", "Ecchi", "https://api.waifu.best/images?IsNsfw=True&IsAnimated=False&IncludedTags=ecchi"),
                Nsfw("ero", "Ero", "https://api.waifu.best/images?IsNsfw=True&IsAnimated=False&IncludedTags=ero")
            ]),

        CreateJsonProvider(
            id: "meow-photos",
            name: "Meow.photos",
            jsonPath: "items[*].url",
            subcategories:
            [
                Sfw("general", "General", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False"),
                Sfw("waifu", "Waifu", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=waifu"),
                Sfw("maid", "Maid", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=maid"),
                Sfw("uniform", "Uniform", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=uniform"),
                Sfw("genshin-impact", "Genshin Impact", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=genshin-impact"),
                Sfw("marin-kitagawa", "Marin Kitagawa", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=marin-kitagawa"),
                Sfw("raiden-shogun", "Raiden Shogun", "https://api.meow.photos/images?IsNsfw=False&IsAnimated=False&IncludedTags=raiden-shogun"),
                Nsfw("nsfw-general", "NSFW General", "https://api.meow.photos/images?IsNsfw=True&IsAnimated=False"),
                Nsfw("ecchi", "Ecchi", "https://api.meow.photos/images?IsNsfw=True&IsAnimated=False&IncludedTags=ecchi"),
                Nsfw("ero", "Ero", "https://api.meow.photos/images?IsNsfw=True&IsAnimated=False&IncludedTags=ero")
            ]),

        CreateJsonProvider(
            id: "waifu-imnb",
            name: "Waifu.imnb",
            jsonPath: "items[*].url",
            subcategories:
            [
                Sfw("general", "General", "https://waifu.imnb.cn:8443/images?IsNsfw=False&IsAnimated=False"),
                Nsfw("nsfw-general", "NSFW General", "https://waifu.imnb.cn:8443/images?IsNsfw=True&IsAnimated=False")
            ]),

        CreateJsonProvider(
            id: "nekos-best",
            name: "Nekos.best",
            jsonPath: "results[*].url",
            subcategories:
            [
                Sfw("general", "General", "https://nekos.best/api/v2/waifu|https://nekos.best/api/v2/neko|https://nekos.best/api/v2/kitsune"),
                Sfw("waifu", "Waifu", "https://nekos.best/api/v2/waifu"),
                Sfw("neko", "Neko", "https://nekos.best/api/v2/neko"),
                Sfw("kitsune", "Kitsune", "https://nekos.best/api/v2/kitsune"),
                Sfw("husbando", "Husbando", "https://nekos.best/api/v2/husbando", enabled: false)
            ]),

        CreateJsonProvider(
            id: "nekos-api",
            name: "NekosAPI v4",
            jsonPath: "[*].url",
            subcategories:
            [
                Sfw("general", "General", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe"),
                Sfw("catgirl", "Catgirl", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=catgirl"),
                Sfw("foxgirl", "Foxgirl", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=foxgirl"),
                Sfw("maid", "Maid", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=maid"),
                Sfw("uniform", "Uniform", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=uniform"),
                Sfw("kimono", "Kimono", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=kimono"),
                Sfw("wings", "Wings", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=wings"),
                Sfw("sword", "Sword", "https://api.nekosapi.com/v4/images/random?limit=1&rating=safe&tags=sword"),
                Nsfw("suggestive", "Suggestive", "https://api.nekosapi.com/v4/images/random?limit=1&rating=suggestive"),
                Nsfw("borderline", "Borderline", "https://api.nekosapi.com/v4/images/random?limit=1&rating=borderline"),
                Nsfw("explicit", "Explicit", "https://api.nekosapi.com/v4/images/random?limit=1&rating=explicit")
            ]),

        CreateJsonProvider(
            id: "nekosia-cat",
            name: "Nekosia.cat",
            jsonPath: "image.original.url",
            fallbackJsonPath: "image.compressed.url",
            subcategories:
            [
                Sfw("general", "General", "https://api.nekosia.cat/api/v1/images/random?rating=safe"),
                Sfw("catgirl", "Catgirl", "https://api.nekosia.cat/api/v1/images/catgirl?rating=safe"),
                Sfw("foxgirl", "Foxgirl", "https://api.nekosia.cat/api/v1/images/foxgirl?rating=safe"),
                Sfw("wolfgirl", "Wolfgirl", "https://api.nekosia.cat/api/v1/images/wolfgirl?rating=safe"),
                Sfw("animal-ears", "Animal Ears", "https://api.nekosia.cat/api/v1/images/animal-ears?rating=safe"),
                Sfw("cute", "Cute", "https://api.nekosia.cat/api/v1/images/cute?rating=safe"),
                Sfw("maid", "Maid", "https://api.nekosia.cat/api/v1/images/maid?rating=safe"),
                Sfw("vtuber", "VTuber", "https://api.nekosia.cat/api/v1/images/vtuber?rating=safe"),
                Sfw("sailor-uniform", "Sailor Uniform", "https://api.nekosia.cat/api/v1/images/sailor-uniform?rating=safe"),
                Sfw("hoodie", "Hoodie", "https://api.nekosia.cat/api/v1/images/hoodie?rating=safe"),
                Sfw("white-hair", "White Hair", "https://api.nekosia.cat/api/v1/images/white-hair?rating=safe"),
                Sfw("blonde", "Blonde", "https://api.nekosia.cat/api/v1/images/blonde?rating=safe"),
                Sfw("blue-archive", "Blue Archive", "https://api.nekosia.cat/api/v1/images/blue-archive?rating=safe"),
                Sfw("heterochromia", "Heterochromia", "https://api.nekosia.cat/api/v1/images/heterochromia?rating=safe"),
                Nsfw("suggestive-general", "Suggestive General", "https://api.nekosia.cat/api/v1/images/random?rating=suggestive"),
                Nsfw("suggestive-catgirl", "Suggestive Catgirl", "https://api.nekosia.cat/api/v1/images/catgirl?rating=suggestive")
            ]),

        CreateJsonProvider(
            id: "pic-re",
            name: "Pic.re",
            jsonPath: "file_url",
            subcategories:
            [
                Sfw("general", "General", "https://pic.re/image.json?compress=false"),
                Sfw("original", "Original", "https://pic.re/image.json?compress=false&in=original"),
                Sfw("long-hair", "Long Hair", "https://pic.re/image.json?compress=false&in=long_hair"),
                Sfw("short-hair", "Short Hair", "https://pic.re/image.json?compress=false&in=short_hair"),
                Sfw("animal-ears", "Animal Ears", "https://pic.re/image.json?compress=false&in=animal_ears"),
                Sfw("twintails", "Twintails", "https://pic.re/image.json?compress=false&in=twintails"),
                Sfw("blonde-hair", "Blonde Hair", "https://pic.re/image.json?compress=false&in=blonde_hair"),
                Sfw("red-eyes", "Red Eyes", "https://pic.re/image.json?compress=false&in=red_eyes"),
                Sfw("purple-eyes", "Purple Eyes", "https://pic.re/image.json?compress=false&in=purple_eyes"),
                Sfw("tail", "Tail", "https://pic.re/image.json?compress=false&in=tail")
            ]),

        new WallpaperProvider
        {
            Id = "nekos-moe",
            Name = "Nekos.moe",
            IsBuiltIn = true,
            Enabled = true,
            Weight = DefaultWeight,
            ResponseMode = ProviderResponseMode.NekosMoeId,
            Subcategories =
            [
                Sfw("general", "General", "https://nekos.moe/api/v1/random/image?nsfw=false&count=1"),
                Nsfw("nsfw-general", "NSFW General", "https://nekos.moe/api/v1/random/image?nsfw=true&count=1")
            ]
        },

        CreateJsonProvider(
            id: "nekos-life",
            name: "Nekos.life",
            jsonPath: "url",
            subcategories:
            [
                Sfw("general", "General", "https://nekos.life/api/v2/img/wallpaper|https://nekos.life/api/v2/img/waifu|https://nekos.life/api/v2/img/neko|https://nekos.life/api/v2/img/fox_girl"),
                Sfw("wallpaper", "Wallpaper", "https://nekos.life/api/v2/img/wallpaper"),
                Sfw("waifu", "Waifu", "https://nekos.life/api/v2/img/waifu"),
                Sfw("neko", "Neko", "https://nekos.life/api/v2/img/neko"),
                Sfw("fox-girl", "Fox Girl", "https://nekos.life/api/v2/img/fox_girl"),
                Sfw("gecg", "Catgirl Engineered", "https://nekos.life/api/v2/img/gecg")
            ]),

        CreateJsonProvider(
            id: "nekobot",
            name: "NekoBot",
            jsonPath: "message",
            subcategories:
            [
                Sfw("general", "General", "https://nekobot.xyz/api/image?type=neko|https://nekobot.xyz/api/image?type=kemonomimi|https://nekobot.xyz/api/image?type=coffee"),
                Sfw("neko", "Neko", "https://nekobot.xyz/api/image?type=neko"),
                Sfw("kemonomimi", "Kemonomimi", "https://nekobot.xyz/api/image?type=kemonomimi"),
                Sfw("coffee", "Anime Coffee", "https://nekobot.xyz/api/image?type=coffee"),
                Nsfw("nsfw-neko", "NSFW Neko", "https://nekobot.xyz/api/image?type=hneko"),
                Nsfw("nsfw-kitsune", "NSFW Kitsune", "https://nekobot.xyz/api/image?type=hkitsune"),
                Nsfw("nsfw-thigh", "NSFW Thigh", "https://nekobot.xyz/api/image?type=hthigh")
            ]),

        CreateJsonProvider(
            id: "purrbot",
            name: "PurrBot",
            jsonPath: "link",
            subcategories:
            [
                Sfw("general", "General", "https://api.purrbot.site/v2/img/sfw/background/img|https://api.purrbot.site/v2/img/sfw/neko/img|https://api.purrbot.site/v2/img/sfw/kitsune/img|https://api.purrbot.site/v2/img/sfw/senko/img|https://api.purrbot.site/v2/img/sfw/shiro/img|https://api.purrbot.site/v2/img/sfw/holo/img|https://api.purrbot.site/v2/img/sfw/okami/img"),
                Sfw("background", "Background Wallpaper", "https://api.purrbot.site/v2/img/sfw/background/img"),
                Sfw("neko", "Neko", "https://api.purrbot.site/v2/img/sfw/neko/img"),
                Sfw("kitsune", "Kitsune", "https://api.purrbot.site/v2/img/sfw/kitsune/img"),
                Sfw("senko", "Senko", "https://api.purrbot.site/v2/img/sfw/senko/img"),
                Sfw("shiro", "Shiro", "https://api.purrbot.site/v2/img/sfw/shiro/img"),
                Sfw("holo", "Holo", "https://api.purrbot.site/v2/img/sfw/holo/img"),
                Sfw("okami", "Okami", "https://api.purrbot.site/v2/img/sfw/okami/img"),
                Nsfw("nsfw-neko", "NSFW Neko", "https://api.purrbot.site/v2/img/nsfw/neko/img")
            ]),

        CreateBooruProvider(
            id: "konachan-net",
            name: "Konachan.net",
            subcategories:
            [
                Sfw("general", "General", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe"),
                Sfw("landscape", "Landscape", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+landscape"),
                Sfw("scenic", "Scenic", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+scenic"),
                Sfw("sky", "Sky", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+sky"),
                Sfw("clouds", "Clouds", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+clouds"),
                Sfw("night", "Night", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+night"),
                Sfw("water", "Water", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+water"),
                Sfw("original", "Original", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+original"),
                Sfw("touhou", "Touhou", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+touhou"),
                Sfw("vocaloid", "Vocaloid", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+vocaloid"),
                Sfw("genshin-impact", "Genshin Impact", "https://konachan.net/post.json?limit=25&tags=order:random+rating:safe+genshin_impact")
            ]),

        CreateBooruProvider(
            id: "yande-re",
            name: "Yande.re",
            subcategories:
            [
                Sfw("general", "General", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe"),
                Sfw("landscape", "Landscape", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe+landscape"),
                Sfw("wallpaper", "Wallpaper", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe+wallpaper"),
                Sfw("kimono", "Kimono", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe+kimono"),
                Sfw("maid", "Maid", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe+maid"),
                Sfw("wings", "Wings", "https://yande.re/post.json?limit=25&tags=order:random+rating:safe+wings"),
                Nsfw("questionable", "Questionable", "https://yande.re/post.json?limit=25&tags=order:random+rating:questionable"),
                Nsfw("explicit", "Explicit", "https://yande.re/post.json?limit=25&tags=order:random+rating:explicit")
            ]),

        CreateBooruProvider(
            id: "danbooru",
            name: "Danbooru",
            subcategories:
            [
                Sfw("general", "General", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general"),
                Sfw("scenery", "Scenery", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general+scenery"),
                Sfw("wallpaper", "Wallpaper", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general+wallpaper"),
                Sfw("1girl", "Solo Girl", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general+1girl"),
                Sfw("cat-ears", "Cat Ears", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general+cat_ears"),
                Sfw("maid", "Maid", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:general+maid"),
                Nsfw("sensitive", "Sensitive", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:sensitive"),
                Nsfw("questionable", "Questionable", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:questionable"),
                Nsfw("explicit", "Explicit", "https://danbooru.donmai.us/posts.json?limit=25&random=true&tags=rating:explicit")
            ]),

        CreateGelbooruDapiProvider(
            id: "safebooru-org",
            name: "Safebooru.org",
            imageBaseUrl: "https://safebooru.org",
            subcategories:
            [
                Sfw("general", "General", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40", maxRandomPage: 100),
                Sfw("wallpaper", "Wallpaper", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=wallpaper", maxRandomPage: 25),
                Sfw("scenery", "Scenery", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=scenery", maxRandomPage: 30),
                Sfw("1girl", "Solo Girl", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=1girl", maxRandomPage: 80),
                Sfw("cat-ears", "Cat Ears", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=cat_ears", maxRandomPage: 25),
                Sfw("fox-ears", "Fox Ears", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=fox_ears", maxRandomPage: 15),
                Sfw("kimono", "Kimono", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=kimono", maxRandomPage: 20),
                Sfw("hatsune-miku", "Hatsune Miku", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=hatsune_miku", maxRandomPage: 25),
                Sfw("genshin-impact", "Genshin Impact", "https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=genshin_impact", maxRandomPage: 25)
            ]),

        CreateGelbooruDapiProvider(
            id: "tbib",
            name: "TBIB (The Big ImageBoard)",
            imageBaseUrl: "https://tbib.org",
            subcategories:
            [
                Sfw("general", "General", "https://tbib.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=rating:safe", maxRandomPage: 80),
                Sfw("wallpaper", "Wallpaper", "https://tbib.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=rating:safe+wallpaper", maxRandomPage: 20),
                Sfw("scenery", "Scenery", "https://tbib.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=rating:safe+scenery", maxRandomPage: 20),
                Nsfw("questionable", "Questionable", "https://tbib.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=rating:questionable", maxRandomPage: 40),
                Nsfw("explicit", "Explicit", "https://tbib.org/index.php?page=dapi&s=post&q=index&json=1&limit=40&tags=rating:explicit", maxRandomPage: 40)
            ]),

        CreateBooruProvider(
            id: "aibooru",
            name: "AIBooru",
            subcategories:
            [
                Sfw("general", "General", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:general"),
                Sfw("scenery", "Scenery", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:general+scenery"),
                Sfw("1girl", "Solo Girl", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:general+1girl"),
                Nsfw("sensitive", "Sensitive", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:sensitive"),
                Nsfw("questionable", "Questionable", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:questionable"),
                Nsfw("explicit", "Explicit", "https://aibooru.online/posts.json?limit=25&random=true&tags=rating:explicit")
            ]),

        CreateDirectProvider(
            id: "alcy-acg",
            name: "Alcy ACG",
            subcategories:
            [
                Sfw("general", "General Anime PC", "https://t.alcy.cc/pc?json"),
                Sfw("moe", "Moe PC", "https://t.alcy.cc/moe?json"),
                Sfw("scenery", "Anime Scenery", "https://t.alcy.cc/fj?json"),
                Sfw("character", "Anime Character", "https://t.alcy.cc/bd?json"),
                Sfw("genshin", "Genshin Impact", "https://t.alcy.cc/ys?json"),
                Sfw("ai-pc", "Anime AI PC", "https://t.alcy.cc/ai?json")
            ]),

        CreateDirectProvider(
            id: "uapipro-acg",
            name: "UApiPro ACG",
            subcategories:
            [
                Sfw("general", "General ACG PC", "https://uapis.cn/api/v1/random/image?category=acg&type=pc"),
                Sfw("anime", "Anime", "https://uapis.cn/api/v1/random/image?category=anime"),
                Sfw("general-anime", "General Anime", "https://uapis.cn/api/v1/random/image?category=general_anime"),
                Sfw("ai-anime", "Anime AI Illustration", "https://uapis.cn/api/v1/random/image?category=ai_drawing")
            ]),

        CreateDirectProvider(
            id: "horosama-anime",
            name: "Horosama Anime",
            subcategories:
            [
                Sfw("general", "General PC", "https://api.horosama.com/random.php?type=pc"),
                Sfw("mobile", "Mobile Portrait", "https://api.horosama.com/random.php?type=mobile", enabled: false)
            ]),

        CreateDirectProvider(
            id: "imlazy-acg",
            name: "Imlazy Pixiv ACG",
            subcategories:
            [
                Sfw("general", "General PC", "https://api.imlazy.ink/img"),
                Sfw("mobile", "Mobile Portrait", "https://api.imlazy.ink/img-phone", enabled: false)
            ]),

        CreateDirectProvider(
            id: "yppp-acg",
            name: "Yppp ACG",
            subcategories:
            [
                Sfw("general", "General PC", "https://api.yppp.net/pc.php"),
                Sfw("mobile", "Mobile Portrait", "https://api.yppp.net/pe.php", enabled: false)
            ]),

        CreateDirectProvider(
            id: "paulzzh-touhou",
            name: "Paulzzh Touhou",
            subcategories:
            [
                Sfw("general", "General PC", "https://img.paulzzh.com/touhou/random?size=pc"),
                Sfw("all", "All Orientations", "https://img.paulzzh.com/touhou/random?size=all", enabled: false)
            ]),

        CreateDirectProvider(
            id: "furina-acg",
            name: "Furina ACG",
            subcategories:
            [
                Sfw("general", "General PC", "https://furina.baigei.cc/pc.php"),
                Sfw("mobile", "Mobile Portrait", "https://furina.baigei.cc/phone.php", enabled: false)
            ]),

        CreateJsonProvider(
            id: "btstu-dongman",
            name: "Btstu Anime",
            jsonPath: "imgurl",
            subcategories:
            [
                Sfw("general", "General PC", "https://api.btstu.cn/sjbz/api.php?lx=dongman&format=json")
            ]),

        CreateDirectProvider(
            id: "imgapi-dongman",
            name: "ImgApi Anime",
            subcategories:
            [
                Sfw("general", "General PC", "https://imgapi.cn/api.php?zd=pc&fl=dongman"),
                Sfw("adaptive", "Adaptive", "https://imgapi.cn/api.php?zd=zsy&fl=dongman"),
                Sfw("mobile", "Mobile Portrait", "https://imgapi.cn/api.php?zd=mobile&fl=dongman", enabled: false)
            ]),

        CreateJsonProvider(
            id: "dmoe-acg",
            name: "Dmoe ACG",
            jsonPath: "imgurl",
            subcategories:
            [
                Sfw("general", "General PC", "https://www.dmoe.cc/random.php?return=json")
            ])
    ];

    static WallpaperProvider CreateJsonProvider(
        string id,
        string name,
        string jsonPath,
        List<WallpaperSubcategory> subcategories,
        string fallbackJsonPath = "") => new()
    {
        Id = id,
        Name = name,
        IsBuiltIn = true,
        Enabled = true,
        Weight = DefaultWeight,
        ResponseMode = ProviderResponseMode.JsonElementPath,
        JsonPath = jsonPath,
        FallbackJsonPath = fallbackJsonPath,
        Subcategories = subcategories
    };

    static WallpaperProvider CreateDirectProvider(
        string id,
        string name,
        List<WallpaperSubcategory> subcategories) => new()
    {
        Id = id,
        Name = name,
        IsBuiltIn = true,
        Enabled = true,
        Weight = DefaultWeight,
        ResponseMode = ProviderResponseMode.DirectUrlOrImage,
        Subcategories = subcategories
    };

    static WallpaperProvider CreateBooruProvider(
        string id,
        string name,
        List<WallpaperSubcategory> subcategories) => new()
    {
        Id = id,
        Name = name,
        IsBuiltIn = true,
        Enabled = true,
        Weight = DefaultWeight,
        ResponseMode = ProviderResponseMode.BooruPostList,
        Subcategories = subcategories
    };

    static WallpaperProvider CreateGelbooruDapiProvider(
        string id,
        string name,
        string imageBaseUrl,
        List<WallpaperSubcategory> subcategories) => new()
    {
        Id = id,
        Name = name,
        IsBuiltIn = true,
        Enabled = true,
        Weight = DefaultWeight,
        ResponseMode = ProviderResponseMode.GelbooruDapi,
        ImageBaseUrl = imageBaseUrl,
        Subcategories = subcategories
    };

    static WallpaperSubcategory Sfw(string id, string name, string endpoint, bool enabled = true, int maxRandomPage = 0) => new()
    {
        Id = id,
        Name = name,
        Endpoint = endpoint,
        IsNsfw = false,
        Enabled = enabled,
        Weight = DefaultWeight,
        MaxRandomPage = maxRandomPage
    };

    static WallpaperSubcategory Nsfw(string id, string name, string endpoint, int maxRandomPage = 0) => new()
    {
        Id = id,
        Name = name,
        Endpoint = endpoint,
        IsNsfw = true,
        Enabled = false,
        Weight = DefaultWeight,
        MaxRandomPage = maxRandomPage
    };
}
