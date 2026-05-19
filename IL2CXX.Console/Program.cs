using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CommandLine;
using IL2CXX;

Parser.Default.ParseArguments<Options>(args).MapResult(options =>
{
    var names = new SortedSet<string>();
    var type2paths = new Dictionary<Type, List<string>>();
    using (var context = new MetadataLoadContext(new PathAssemblyResolver(options.Assemblies!.Prepend(Path.GetDirectoryName(options.Source)).Append(RuntimeEnvironment.GetRuntimeDirectory()).SelectMany(x => Directory.EnumerateFiles(x!.Length > 0 ? x : ".", "*.dll")).Append(typeof(Builtin).Assembly.Location).UnionBy(Enumerable.Empty<string>(), Path.GetFileNameWithoutExtension))))
    {
        var assembly = context.LoadFromAssemblyPath(options.Source ?? throw new Exception());
        var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
        if (Directory.Exists(options.Out)) Directory.Delete(options.Out, true);
        Directory.CreateDirectory(options.Out ?? throw new Exception());
        Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName ?? throw new Exception()).GetType(x.FullName ?? throw new Exception(), true)!;
        var builtin = DefaultBuiltin.Create(get, options.Target);
        if (options.Target == PlatformID.Other)
        {
            builtin
            .For(get(typeof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument)), (type, code) =>
            {
                code.For(
                    type.GetMethod(nameof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument.ToManaged), [get(typeof(string).MakeByRefType())]),
                    transpiler => ($@"{'\t'}if (a_0->v_slot.v_Type.v) {{
{'\t'}{'\t'}auto& p = reinterpret_cast<t_System_2eString*&>(a_0->v_slot.v_IntPtrValue.v);
{'\t'}{'\t'}f__store(*a_1, p);
{'\t'}{'\t'}f__store(p, nullptr);
{'\t'}}} else {{
{'\t'}{'\t'}f__store(*a_1, nullptr);
{'\t'}}}
", 1)
                );
                code.For(
                    type.GetMethod(nameof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument.ToJS), [get(typeof(string))]),
                    transpiler => ($@"{'\t'}if (a_1) {{
{'\t'}{'\t'}a_0->v_slot.v_Type.v = 15;
{'\t'}{'\t'}f__store(reinterpret_cast<t_System_2eString*&>(a_0->v_slot.v_IntPtrValue.v), a_1);
{'\t'}}} else {{
{'\t'}{'\t'}a_0->v_slot.v_Type.v = 0;
{'\t'}}}
", 1)
                );
            })
            .For(context.LoadFromAssemblyName("System.Runtime.InteropServices.JavaScript").GetType("Interop+Runtime", true)!, (type, code) =>
            {
                code.For(
                    type.GetMethod("RegisterGCRoot"),
                    transpiler => (string.Empty, 1)
                );
                code.For(
                    type.GetMethod("DeregisterGCRoot"),
                    transpiler => (string.Empty, 1)
                );
                code.For(
                    type.GetMethod("BindJSImportST"),
                    transpiler => ($@"{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_bind_js_import_ST(a_0);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("InvokeJSImportST"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_invoke_jsimport_ST(a_0, a_1);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("ReleaseCSOwnedObject", BindingFlags.Static | BindingFlags.NonPublic),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_release_cs_owned_object(a_0);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("ResolveOrRejectPromise"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_resolve_or_reject_promise(a_0);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("InvokeJSFunction"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_invoke_js_function(a_0, a_1);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("CancelPromise"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_cancel_promise(a_0);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("AssemblyGetEntryPoint"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}*a_2 = mono_wasm_assembly_get_entry_point(static_cast<char*>(static_cast<void*>(a_0)), a_1);
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("BindAssemblyExports"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_bind_assembly_exports(static_cast<char*>(static_cast<void*>(a_0)));
{'\t'}}});
", 0)
                );
                code.For(
                    type.GetMethod("GetAssemblyExport"),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}*a_5 = mono_wasm_get_assembly_export(static_cast<char*>(static_cast<void*>(a_0)), static_cast<char*>(static_cast<void*>(a_1)), static_cast<char*>(static_cast<void*>(a_2)), static_cast<char*>(static_cast<void*>(a_3)), a_4);
{'\t'}}});
", 0)
                );
            })
            .For(context.LoadFromAssemblyName("System.Runtime.InteropServices.JavaScript").GetType("System.Runtime.InteropServices.JavaScript.JSHostImplementation", true)!, (type, code) =>
            {
                code.For(
                    type.GetMethod("LoadLazyAssembly"),
                    transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
                );
                code.For(
                    type.GetMethod("LoadSatelliteAssembly"),
                    transpiler => ("\tthrow std::runtime_error(\"NotImplementedException \" + IL2CXX__AT());\n", 0)
                );
            })
            .For(get(typeof(System.Runtime.InteropServices.RuntimeInformation)), (type, code) =>
            {
                code.For(
                    type.GetProperty(nameof(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture))!.GetMethod,
                    transpiler => ($"\treturn {(int)System.Runtime.InteropServices.Architecture.Wasm};\n", 1)
                );
            })
            .For(get(typeof(JSSynchronizationContext)), (type, code) =>
            {
                code.For(
                    type.GetMethod(nameof(JSSynchronizationContext.Notify)),
                    transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}il2cxx_js_synchronization_context_notify();
{'\t'}}});
", 0)
                );
            });
            // TODO configure ThreadPool.MinThreads/MaxThreads?
        }
        Type load(string x) => Type.GetType(x, context.LoadFromAssemblyName, (assembly, name, ignoreCase) => int.TryParse(name, out var x) ? Type.MakeGenericMethodParameter(x) : assembly?.GetType(name, false, ignoreCase), true)!;
        var bundleTypes = new List<Type>();
        var bundleMethods = new List<MethodInfo>();
        var methodPattern = new Regex(@"(.*?)\s*:\s*(\w+)`(\d+)\((.+?)\)\s*(.+)", RegexOptions.Compiled);
        foreach (var x in options.Bundle!)
        {
            var match = methodPattern.Match(x);
            if (match.Success)
                bundleMethods.Add(load(match.Groups[1].Value).GetMethod(
                    match.Groups[2].Value,
                    int.Parse(match.Groups[3].Value),
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    match.Groups[4].Value.Split(';').Select(load).ToArray(),
                    null
                )!.MakeGenericMethod(
                    match.Groups[5].Value.Split(';').Select(load).ToArray()
                ));
            else
                bundleTypes.Add(load(x));
        }
        var reflection = options.Reflection!.Select(load).ToHashSet();
        if (options.Target == PlatformID.Other)
        {
            var types = new[]
            {
                load("System.Runtime.InteropServices.JavaScript.JavaScriptExports, System.Runtime.InteropServices.JavaScript"),
                assembly.GetType("System.Runtime.InteropServices.JavaScript.__GeneratedInitializer")
            }.Where(x => x != null);
            bundleTypes.AddRange(types!);
            var jssc = get(typeof(JSSynchronizationContext));
            bundleMethods.AddRange([
                    jssc.GetMethod(nameof(JSSynchronizationContext.Install))!,
                    jssc.GetMethod(nameof(JSSynchronizationContext.Pump))!
            ]);
            reflection.UnionWith(types!);
            reflection.Add(get(typeof(System.Threading.Tasks.Task<>)));
        }
        var transpiler = new Transpiler(get, builtin, /*Console.Error.WriteLine*/_ => { }, options.Target, options.Is64, options.CheckNull)
        {
            Bundle = bundleTypes,
            BundleMethods = bundleMethods,
            GenerateReflection = reflection.Contains
        };
        var definition = TextWriter.Null;
        try
        {
            using var declarations = File.CreateText(Path.Combine(options.Out, "declarations.h"));
            declarations.WriteLine(@"#ifndef DECLARATIONS_H
#define DECLARATIONS_H");
            using var inlines = new StringWriter();
            using var others = new StringWriter();
            using var main = File.CreateText(Path.Combine(options.Out, "main.cc"));
            main.WriteLine("#include \"declarations.h\"\n");
            transpiler.Do(entry, declarations, main, (type, inline) =>
            {
                if (inline) return inlines;
                if (type.IsInterface || type.IsSubclassOf(transpiler.typeofMulticastDelegate) || type.IsGenericParameter) return others;
                definition.Dispose();
                while ((transpiler.GetNullableUnderlyingType(type) ?? type.GetElementType()) is Type t) type = t;
                while (type.IsNested) type = type.DeclaringType ?? throw new Exception();
                if (type.IsGenericType) type = type.GetGenericTypeDefinition();
                if (type2paths.TryGetValue(type, out var paths))
                {
                    var last = paths[paths.Count - 1];
                    if (new FileInfo(last).Length < 1024 * 1024) return definition = new StreamWriter(last, true);
                }
                else
                {
                    paths = [];
                    type2paths.Add(type, paths);
                }
                var escaped = transpiler.EscapeType(type);
                if (escaped.Length > 240) escaped = escaped.Substring(0, 240);
                var name = $"{escaped}.cc";
                for (var i = 0; !names.Add(name); ++i) name = $"{escaped}__{i}.cc";
                var path = Path.Combine(options.Out, name);
                paths.Add(path);
                definition = new StreamWriter(path);
                definition.WriteLine(@"#include ""declarations.h""

namespace il2cxx
{");
                return definition;
            });
            declarations.WriteLine("\nnamespace il2cxx\n{");
            declarations.Write(inlines);
            declarations.WriteLine(@"
}

#endif");
            main.WriteLine("\nnamespace il2cxx\n{");
            main.Write(others);
            main.WriteLine("\n}");
        }
        finally
        {
            definition.Dispose();
        }
    }
    foreach (var path in type2paths.Values.SelectMany(x => x)) File.AppendAllText(path, "\n}\n");
    void copy(string path)
    {
        var destination = Path.Combine(options.Out, path);
        Directory.CreateDirectory(destination);
        var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        foreach (var x in Directory.EnumerateDirectories(source)) copy(Path.Combine(path, Path.GetFileName(x)));
        foreach (var x in Directory.EnumerateFiles(source)) File.Copy(x, Path.Combine(destination, Path.GetFileName(x)));
    }
    copy("src");
    if (options.Target == PlatformID.Other) copy("wasm");
    var name = Path.GetFileNameWithoutExtension(options.Source);
    File.WriteAllText(Path.Combine(options.Out, "CMakeLists.txt"), $@"cmake_minimum_required(VERSION 3.16)
project({name})

add_subdirectory(src/recyclone EXCLUDE_FROM_ALL)

add_executable({name}
{'\t'}src/types.cc
{'\t'}src/engine.cc
{'\t'}src/handles.cc
{(options.Target == PlatformID.Win32NT ? string.Empty : "\tsrc/waitables.cc\n")
}{string.Join(string.Empty, names.Select(x => $"\t{x}\n"))
}{'\t'}main.cc
{'\t'})
target_compile_options({name} PRIVATE $<$<NOT:$<CXX_COMPILER_ID:MSVC>>:-fno-rtti> $<$<CXX_COMPILER_ID:MSVC>:/bigobj>)
target_precompile_headers({name} PRIVATE declarations.h){
(options.Target == PlatformID.Other ? $@"
set_target_properties({name} PROPERTIES OUTPUT_NAME dotnet.native)
target_sources({name} PRIVATE wasm/src/driver.cc wasm/src/pinvoke.cc)
target_include_directories({name} PRIVATE wasm/src src .)
target_link_libraries({name} recyclone dl
{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/src/libminipal.a
{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/src/libSystem.Native.a
{'\t'}""-s FORCE_FILESYSTEM;-s EXPORTED_RUNTIME_METHODS=\""['cwrap', 'setValue', 'lengthBytesUTF8', 'UTF8ToString', 'UTF8ArrayToString', 'stringToUTF8Array', 'FS', 'runtimeKeepalivePush', 'runtimeKeepalivePop', 'HEAP8', 'HEAP16', 'HEAP32', 'HEAP64', 'HEAPU8', 'HEAPU16', 'HEAPU32', 'HEAPU64', 'HEAPF32', 'HEAPF64']\"";-s EXPORTED_FUNCTIONS=\""['_free', '_malloc', 'stackSave', 'stackRestore', 'stackAlloc']\"";-s EXPORT_NAME=\""'createDotnetRuntime'\"";-s MODULARIZE;-s EXPORT_ES6;--emit-symbol-map;--pre-js ${{PROJECT_SOURCE_DIR}}/wasm/src/es6/dotnet.es6.pre.js;--js-library ${{PROJECT_SOURCE_DIR}}/wasm/src/es6/dotnet.es6.lib.js;--extern-post-js ${{PROJECT_SOURCE_DIR}}/wasm/src/es6/dotnet.es6.extpost.js""
{'\t'})
" : $@"
target_include_directories({name} PRIVATE src .)
target_link_libraries({name} recyclone  $<$<NOT:$<CXX_COMPILER_ID:MSVC>>:dl>)
")}");
    return 0;
}, _ => 1);

class Options
{
    [Option(Required = true)]
    public PlatformID Target { get; set; }
    [Option]
    public bool Is32 { get; set; }
    public bool Is64 => !Is32;
    [Option("check-null")]
    public bool CheckNull { get; set; }
    [Option(Default = "out")]
    public string? Out { get; set; }
    [Value(0, Required = true)]
    public string? Source { get; set; }
    [Option]
    public IEnumerable<string>? Assemblies { get; set; }
    [Option]
    public IEnumerable<string>? Bundle { get; set; }
    [Option]
    public IEnumerable<string>? Reflection { get; set; }
}
