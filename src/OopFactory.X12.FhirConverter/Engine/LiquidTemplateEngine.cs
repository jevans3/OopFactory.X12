using System.Reflection;
using DotLiquid;
using DotLiquid.FileSystems;
using OopFactory.X12.FhirConverter.Engine.CustomFilters;

namespace OopFactory.X12.FhirConverter.Engine;

public class LiquidTemplateEngine
{
    private readonly string _templateBasePath;
    private readonly Dictionary<string, Template> _templateCache = new();
    private readonly object _cacheLock = new();

    public LiquidTemplateEngine(string templateBasePath)
    {
        _templateBasePath = templateBasePath;

        Template.RegisterFilter(typeof(DateFilters));
        Template.RegisterFilter(typeof(X12Filters));
        Template.RegisterFilter(typeof(FhirFilters));
        Template.RegisterFilter(typeof(CodeMappingFilters));

        Template.FileSystem = new LocalFileSystem(Path.Combine(_templateBasePath, "Shared"));
    }

    public string Render(string templateCategory, string transactionType, string templateName, Hash data)
    {
        var templatePath = Path.Combine(_templateBasePath, templateCategory, transactionType, $"{templateName}.liquid");
        var template = GetOrLoadTemplate(templatePath);
        return template.Render(data);
    }

    public string RenderX12ToFhir(string transactionType, string templateName, Hash data)
    {
        return Render("X12ToFhir", transactionType, templateName, data);
    }

    public string RenderFhirToX12(string transactionType, string templateName, Hash data)
    {
        return Render("FhirToX12", transactionType, templateName, data);
    }

    private Template GetOrLoadTemplate(string templatePath)
    {
        lock (_cacheLock)
        {
            if (_templateCache.TryGetValue(templatePath, out var cached))
                return cached;

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Liquid template not found: {templatePath}");

            var templateContent = File.ReadAllText(templatePath);
            var template = Template.Parse(templateContent);
            _templateCache[templatePath] = template;
            return template;
        }
    }

    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _templateCache.Clear();
        }
    }

    public IReadOnlyList<string> GetAvailableTemplates()
    {
        var templates = new List<string>();
        if (Directory.Exists(_templateBasePath))
        {
            foreach (var file in Directory.GetFiles(_templateBasePath, "*.liquid", SearchOption.AllDirectories))
            {
                templates.Add(Path.GetRelativePath(_templateBasePath, file));
            }
        }
        return templates;
    }
}
