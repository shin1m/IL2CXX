﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using CommandLine;

namespace IL2CXX.Console
{
    using System;
    using System.Runtime.InteropServices;

    class Program
    {
        class Options
        {
            [Option(Required = true)]
            public PlatformID Target { get; set; }
            [Option(Group = "is32", Default = false)]
            public bool Is32 { get; set; }
            public bool Is64 => !Is32;
            [Option(Default = "out")]
            public string Out { get; set; }
            [Value(0, Required = true)]
            public string Source { get; set; }
            [Option]
            public IEnumerable<string> Assemblies { get; set; }
            [Option]
            public IEnumerable<string> Bundle { get; set; }
            [Option]
            public IEnumerable<string> Reflection { get; set; }
        }
        static int Main(string[] args) => Parser.Default.ParseArguments<Options>(args).MapResult(options =>
        {
            var names = new SortedSet<string>();
            var type2paths = new Dictionary<Type, List<string>>();
            using (var context = new MetadataLoadContext(new PathAssemblyResolver(options.Assemblies.Prepend(Path.GetDirectoryName(options.Source)).Append(RuntimeEnvironment.GetRuntimeDirectory()).SelectMany(x => Directory.EnumerateFiles(x.Length > 0 ? x : ".", "*.dll")).Append(typeof(Builtin).Assembly.Location).UnionBy(Enumerable.Empty<string>(), Path.GetFileNameWithoutExtension))))
            {
                var assembly = context.LoadFromAssemblyPath(options.Source);
                var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
                if (Directory.Exists(options.Out)) Directory.Delete(options.Out, true);
                Directory.CreateDirectory(options.Out);
                Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var builtin = DefaultBuiltin.Create(get, options.Target);
                if (options.Target == PlatformID.Other)
                {
                    void forIf(Builtin.Code code, MethodBase method, Func<Transpiler, (string body, int inline)> body)
                    {
                        if (method != null) code.For(method, body);
                    }
                    builtin
                    .For(get(typeof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument)), (type, code) =>
                    {
                        void codeFor(MethodBase method, Func<Transpiler, (string body, int inline)> body) => forIf(code, method, body);
                        codeFor(
                            type.GetMethod(nameof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument.ToManaged), new[] { get(typeof(string).MakeByRefType()) }),
                            transpiler => ($@"{'\t'}if (a_0->v_slot.v_Type.v) {{
{'\t'}{'\t'}auto& p = reinterpret_cast<t_System_2eString*&>(a_0->v_slot.v_IntPtrValue.v);
{'\t'}{'\t'}f__store(*a_1, p);
{'\t'}{'\t'}f__store(p, nullptr);
{'\t'}}} else {{
{'\t'}{'\t'}f__store(*a_1, nullptr);
{'\t'}}}
", 1)
                        );
                        codeFor(
                            type.GetMethod(nameof(System.Runtime.InteropServices.JavaScript.JSMarshalerArgument.ToJS), new[] { get(typeof(string)) }),
                            transpiler => ($@"{'\t'}if (a_1) {{
{'\t'}{'\t'}a_0->v_slot.v_Type.v = 15;
{'\t'}{'\t'}f__store(reinterpret_cast<t_System_2eString*&>(a_0->v_slot.v_IntPtrValue.v), a_1);
{'\t'}}} else {{
{'\t'}{'\t'}a_0->v_slot.v_Type.v = 0;
{'\t'}}}
", 1)
                        );
                    })
                    .For(context.LoadFromAssemblyName("System.Runtime.InteropServices.JavaScript").GetType("Interop+Runtime", true), (type, code) =>
                    {
                        void codeFor(MethodBase method, Func<Transpiler, (string body, int inline)> body) => forIf(code, method, body);
                        codeFor(
                            type.GetMethod("ReleaseCSOwnedObject", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_release_cs_owned_object(a_0);
{'\t'}}});
", 0)
                        );
                        codeFor(
                            type.GetMethod("BindJSFunction"),
                            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}int bound;
{'\t'}{'\t'}*a_4 = 0;
{'\t'}{'\t'}mono_wasm_bind_js_function(a_0, a_1, a_2, &bound, a_4, a_5);
{'\t'}{'\t'}*a_3 = bound;
{'\t'}}});
", 0)
                        );
                        codeFor(
                            type.GetMethod("InvokeJSFunction"),
                            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_invoke_bound_function(a_0, a_1);
{'\t'}}});
", 0)
                        );
                        codeFor(
                            type.GetMethod("BindCSFunction"),
                            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_bind_cs_function(a_0, a_1, a_2, a_3, a_4);
{'\t'}}});
", 0)
                        );
                        codeFor(
                            type.GetMethod("MarshalPromise"),
                            transpiler => ($@"{'\t'}f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}mono_wasm_marshal_promise(a_0);
{'\t'}}});
", 0)
                        );
                        codeFor(
                            type.GetMethod("RegisterGCRoot"),
                            transpiler => (string.Empty, 1)
                        );
                        codeFor(
                            type.GetMethod("DeregisterGCRoot"),
                            transpiler => (string.Empty, 1)
                        );
                    })
                    .For(get(typeof(System.Runtime.InteropServices.RuntimeInformation)), (type, code) =>
                    {
                        code.For(
                            type.GetProperty(nameof(System.Runtime.InteropServices.RuntimeInformation.OSArchitecture)).GetMethod,
                            transpiler => ($"\treturn {(int)System.Runtime.InteropServices.Architecture.Wasm};\n", 1)
                        );
                    })
                    .For(get(typeof(ThreadPool)), (type, code) => code.For(
                        type.GetMethod("InitializeConfigAndDetermineUsePortableThreadPool", BindingFlags.Static | BindingFlags.NonPublic),
                        transpiler =>
                        {
                            var set = get(typeof(AppContext)).GetMethod("SetData");
                            transpiler.Enqueue(set);
                            return ($@"{'\t'}{transpiler.Escape(set)}(f__new_string(u""System.Threading.ThreadPool.MinThreads""sv), f__new_constructed<{transpiler.Escape(get(typeof(int)))}>(1));
{'\t'}{transpiler.Escape(set)}(f__new_string(u""System.Threading.ThreadPool.MaxThreads""sv), f__new_constructed<{transpiler.Escape(get(typeof(int)))}>(1));
{'\t'}return true;
", 1);
                        }
                    ));
                }
                Type load(string x) => Type.GetType(x, context.LoadFromAssemblyName, (assembly, name, ignoreCase) => int.TryParse(name, out var x) ? Type.MakeGenericMethodParameter(x) : assembly.GetType(name, false, ignoreCase), true);
                var bundleTypes = new List<Type>();
                var bundleMethods = new List<MethodInfo>();
                var methodPattern = new Regex(@"(.*?)\s*:\s*(\w+)`(\d+)\((.+?)\)\s*(.+)", RegexOptions.Compiled);
                foreach (var x in options.Bundle)
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
                        ).MakeGenericMethod(
                            match.Groups[5].Value.Split(';').Select(load).ToArray()
                        ));
                    else
                        bundleTypes.Add(load(x));
                }
                var reflection = options.Reflection.Select(load).ToHashSet();
                var transpiler = new Transpiler(get, builtin, /*Console.Error.WriteLine*/_ => { }, options.Target, options.Is64, false)
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
                        while (type.IsNested) type = type.DeclaringType;
                        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
                        if (type2paths.TryGetValue(type, out var paths))
                        {
                            var last = paths[paths.Count - 1];
                            if (new FileInfo(last).Length < 1024 * 1024) return definition = new StreamWriter(last, true);
                        }
                        else
                        {
                            paths = new();
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
target_compile_options({name} PRIVATE ""-fno-rtti"")
target_precompile_headers({name} PRIVATE declarations.h){
(options.Target == PlatformID.Other ? $@"
set_target_properties({name} PROPERTIES OUTPUT_NAME dotnet)
target_sources({name} PRIVATE wasm/src/driver.cc wasm/src/pinvoke.cc)
target_include_directories({name} PRIVATE wasm/src src .)
target_link_libraries({name} recyclone dl
{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/src/libSystem.Native.a
{'\t'}""-s FORCE_FILESYSTEM;-s EXPORTED_RUNTIME_METHODS=\""['cwrap', 'setValue', 'UTF8ToString', 'UTF8ArrayToString', 'FS']\"";-s EXPORTED_FUNCTIONS=\""['_free', '_malloc', 'stackSave', 'stackRestore', 'stackAlloc']\"";-s EXPORT_NAME=\""'createDotnetRuntime'\"";-s MODULARIZE;-s EXPORT_ES6;--emit-symbol-map;--extern-pre-js ${{PROJECT_SOURCE_DIR}}/wasm/runtime/bin/src/es6/runtime.es6.iffe.js;--pre-js ${{PROJECT_SOURCE_DIR}}/wasm/runtime/es6/dotnet.es6.pre.js;--js-library ${{PROJECT_SOURCE_DIR}}/wasm/runtime/es6/dotnet.es6.lib.js;--js-library ${{PROJECT_SOURCE_DIR}}/wasm/src/pal_random.lib.js;--post-js ${{PROJECT_SOURCE_DIR}}/wasm/runtime/es6/dotnet.es6.post.js;--extern-post-js ${{PROJECT_SOURCE_DIR}}/wasm/runtime/es6/dotnet.es6.extpost.js""
{'\t'})
" : $@"
target_include_directories({name} PRIVATE src .)
target_link_libraries({name} recyclone dl)
")}");
            return 0;
        }, _ => 1);
    }
}
