namespace devanewbot.Services;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// Link unfurlers don't run JS, so public pages need their title and OG tags in the served HTML.
public static partial class SpaPageMeta
{
    private const string SiteName = "Devanooga";

    private record PageMeta(string Title, string Description);

    private static readonly Dictionary<string, PageMeta> Pages = new()
    {
        ["/"] = new("Join the community", "Devanooga is a community of developers and makers in Chattanooga, Tennessee."),
        ["/moderation"] = new("Moderation log", "Every administrative action taken in the Devanooga community, per our code of conduct."),
        ["/finances"] = new("Finances", "Devanooga's cash on hand, and where the money comes from and goes."),
    };

    public static async Task ServeIndex(HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var index = environment.WebRootFileProvider.GetFileInfo("index.html");
        if (!index.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string html;
        using (var reader = new StreamReader(index.CreateReadStream()))
        {
            html = await reader.ReadToEndAsync();
        }

        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
        if (Pages.TryGetValue(path == "" ? "/" : path, out var page))
        {
            html = WithMeta(html, page, $"{context.Request.Scheme}://{context.Request.Host}");
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html);
    }

    private static string WithMeta(string html, PageMeta page, string origin)
    {
        var title = WebUtility.HtmlEncode($"{page.Title} · {SiteName}");
        var description = WebUtility.HtmlEncode(page.Description);

        html = TitleTag().Replace(html, $"<title>{title}</title>", 1);
        html = DescriptionTag().Replace(html, "", 1);

        var tags = $"""
            <meta name="description" content="{description}">
            <meta property="og:type" content="website">
            <meta property="og:site_name" content="{SiteName}">
            <meta property="og:title" content="{WebUtility.HtmlEncode(page.Title)}">
            <meta property="og:description" content="{description}">
            <meta property="og:image" content="{WebUtility.HtmlEncode(origin)}/img/icons/android-chrome-512x512.png">
            <meta name="twitter:card" content="summary">
            """;

        return html.Replace("</head>", tags + "</head>");
    }

    [GeneratedRegex("<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex("<meta name=\"description\"[^>]*>")]
    private static partial Regex DescriptionTag();
}
