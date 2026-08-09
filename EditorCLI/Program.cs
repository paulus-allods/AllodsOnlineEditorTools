using EditorCLI;
using EditorCLI.Commands.Generation;
using EditorCLI.Commands.Pack;
using EditorCLI.Commands.Texture;
using EditorCLI.Commands.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options => options.IncludeScopes = true);
    builder.SetMinimumLevel(LogLevel.Information);
});
  
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    // Fail on unrecognized options instead of collecting them as remaining arguments.
    config.UseStrictParsing();

    config.AddBranch("pack", pack =>
    {
        pack.AddCommand<UnpackCommand>("unpack");
        pack.AddCommand<PackListCommand>("ls");
        pack.AddCommand<PackInfoCommand>("info");
    });
    config.AddBranch("generate", structs =>
    {
        structs.AddCommand<GenerateStructCodeCommand>("structs").IsHidden();
    });
    config.AddBranch("texture", texture =>
    {
        texture.AddBranch("bin", bin =>
        {
            bin.AddCommand<BinExportCommand>("export");
        });
        texture.AddBranch("dds", dds =>
        {
            dds.AddCommand<DDSImportCommand>("import").IsHidden();
        });
    });
    config.AddBranch("utils", utils =>
    {
        utils.AddCommand<CompressCommand>("compress");
        utils.AddCommand<DecompressCommand>("decompress");
    });
});

try
{
    return app.Run(args);
}
finally
{
    registrar.Dispose();
}

namespace EditorCLI
{
    public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar, IDisposable
    {
        private ServiceProvider? _provider;

        public ITypeResolver Build()
        {
            _provider = services.BuildServiceProvider();
            return new TypeResolver(_provider);
        }

        public void Register(Type service, Type implementation) => services.AddSingleton(service, implementation);

        public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);

        public void RegisterLazy(Type service, Func<object> factory) => services.AddSingleton(service, _ => factory());

        public void Dispose() => _provider?.Dispose();
    }

    public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
    {
        public object? Resolve(Type? type) => type == null ? null : provider.GetService(type);
    }
}