using System.Collections.Generic;
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
            var type2path = new Dictionary<Type, string>();
            using (var context = new MetadataLoadContext(new PathAssemblyResolver(options.Assemblies.Prepend(Path.GetDirectoryName(options.Source)).Append(RuntimeEnvironment.GetRuntimeDirectory()).SelectMany(x => Directory.EnumerateFiles(x.Length > 0 ? x : ".", "*.dll")).Append(typeof(Builtin).Assembly.Location).UnionBy(Enumerable.Empty<string>(), Path.GetFileNameWithoutExtension))))
            {
                var assembly = context.LoadFromAssemblyPath(options.Source);
                var entry = assembly.EntryPoint ?? throw new InvalidOperationException();
                if (Directory.Exists(options.Out)) Directory.Delete(options.Out, true);
                Directory.CreateDirectory(options.Out);
                Type get(Type x) => context.LoadFromAssemblyName(x.Assembly.FullName).GetType(x.FullName, true);
                var builtin = DefaultBuiltin.Create(get, options.Target);
                try
                {
                    builtin
                    .For(context.LoadFromAssemblyName("System.Private.Runtime.InteropServices.JavaScript").GetType("Interop+Runtime", true), (type, code) =>
                    {
                        var typeofIntByRef = get(typeof(int).MakeByRefType());
                        code.For(
                            type.GetMethod("CompileFunction", BindingFlags.Static | BindingFlags.NonPublic, new[] { get(typeof(string)), typeofIntByRef }),
                            transpiler => ($@"{'\t'}*a_1 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_compile_function(a_0, a_1);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("InvokeJS", BindingFlags.Static | BindingFlags.NonPublic, new[] { get(typeof(string)), typeofIntByRef }),
                            transpiler => ($@"{'\t'}*a_1 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_invoke_js(a_0, a_1);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("InvokeJSWithArgs", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_3 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_invoke_js_with_args(a_0, a_1, a_2, a_3);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("GetObjectProperty", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_2 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_get_object_property(a_0, a_1, a_2);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("SetObjectProperty", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_5 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_set_object_property(a_0, a_1, a_2, a_3, a_4, a_5);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("GetByIndex", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_2 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_get_by_index(a_0, a_1, a_2);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("SetByIndex", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_3 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_set_by_index(a_0, a_1, a_2, a_3);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("GetGlobalObject", BindingFlags.Static | BindingFlags.NonPublic, new[] { get(typeof(string)), typeofIntByRef }),
                            transpiler => ($@"{'\t'}*a_1 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_get_global_object(a_0, a_1);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("ReleaseCSOwnedObject", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}return static_cast<t__object*>(f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_release_cs_owned_object(a_0);
{'\t'}}}));
", 0)
                        );
                        code.For(
                            type.GetMethod("CreateCSOwnedObject", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_2 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_create_cs_owned_object(a_0, a_1, a_2);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("TypedArrayFrom", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_5 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_typed_array_from(a_0, a_1, a_2, a_3, a_4, a_5);
{'\t'}}});
", 0)
                        );
                        code.For(
                            type.GetMethod("TypedArrayToArray", BindingFlags.Static | BindingFlags.NonPublic),
                            transpiler => ($@"{'\t'}*a_1 = 0;
{'\t'}return f_epoch_region([&]
{'\t'}{{
{'\t'}{'\t'}return mono_wasm_typed_array_to_array(a_0, a_1);
{'\t'}}});
", 0)
                        );
                    })
                    .For(context.LoadFromAssemblyName("Microsoft.JSInterop.WebAssembly").GetType("WebAssembly.JSInterop.InternalCalls", true), (type, code) => code.ForGeneric(
                        type.GetMethod("InvokeJS"),
                        (transpiler, types) =>
                        {
                            string marshal(Type t, string x) =>
                                t.IsByRef || t.IsPointer ? x :
                                t.IsPrimitive || t.IsEnum ? $"reinterpret_cast<void*>({x})" :
                                t.IsValueType ? $"&{x}" : x;
                            string root(Type t, string x) => transpiler.Define(t).IsManaged ? $"\t\t{transpiler.EscapeForRoot(t)} r{x}({x});\n" : string.Empty;
                            return ($@"{'\t'}/*std::printf(""InvokeJS<{string.Join(", ", types.Select(x => x.ToString()))}>:\n"");
{'\t'}std::printf(""\tFunctionIdentifier: %s\n"", f__string(a_1->v_FunctionIdentifier.v).c_str());
{'\t'}std::printf(""\tResultType: %d\n"", a_1->v_ResultType.v);
{'\t'}if (auto s = a_1->v_MarshalledCallArgsJson.v) std::printf(""\tMarshalledCallArgsJson: %s\n"", f__string(s).c_str());
{'\t'}std::printf(""\tMarshalledCallAsyncHandle: %lld\n"", a_1->v_MarshalledCallAsyncHandle.v);
{'\t'}std::printf(""\tTargetInstanceId: %lld\n"", a_1->v_TargetInstanceId.v);*/
{'\t'}f__store(*a_0, nullptr);
{'\t'}void* p;
{'\t'}if (emscripten_is_main_runtime_thread()) {{
{'\t'}{'\t'}p = f_epoch_region([&]
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return mono_wasm_invoke_js_blazor(a_0, a_1, {marshal(types[0], "a_2")}, {marshal(types[1], "a_3")}, {marshal(types[2], "a_4")});
{'\t'}{'\t'}}});
{'\t'}}} else {{
{'\t'}{'\t'}t_root<t_slot_of<t_System_2eString>> r0(a_1->v_FunctionIdentifier.v);
{'\t'}{'\t'}t_root<t_slot_of<t_System_2eString>> r1(a_1->v_MarshalledCallArgsJson.v);
{root(types[0], "a_2")}{root(types[1], "a_3")}{root(types[2], "a_4")
}{'\t'}{'\t'}p = f_epoch_region([&]
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}std::promise<void*> promise;
{'\t'}{'\t'}{'\t'}void* ps[] = {{a_0, a_1, {marshal(types[0], "a_2")}, {marshal(types[1], "a_3")}, {marshal(types[2], "a_4")}, &promise}};
{'\t'}{'\t'}{'\t'}emscripten_async_run_in_main_runtime_thread(EM_FUNC_SIG_VI, +[](void* p)
{'\t'}{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}{'\t'}emscripten_async_call([](void* p)
{'\t'}{'\t'}{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}{'\t'}{'\t'}auto ps = static_cast<void**>(p);
{'\t'}{'\t'}{'\t'}{'\t'}{'\t'}static_cast<std::promise<void*>*>(ps[5])->set_value(mono_wasm_invoke_js_blazor(static_cast<t_System_2eString**>(ps[0]), ps[1], ps[2], ps[3], ps[4]));
{'\t'}{'\t'}{'\t'}{'\t'}}}, p, 0);
{'\t'}{'\t'}{'\t'}}}, ps);
{'\t'}{'\t'}{'\t'}return promise.get_future().get();
{'\t'}{'\t'}}});
{'\t'}}}
{'\t'}if (*a_0) std::fprintf(stderr, ""InvokeJS<{string.Join(", ", types.Select(x => x.ToString()))}>: %s\n"", f__string(*a_0).c_str());
{'\t'}//std::printf(""done: %p, %p\n"", p, *a_0);
{'\t'}return reinterpret_cast<{transpiler.EscapeForStacked(types[3])}>(p);
", 0);
                        }
                    ))
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
                catch (FileNotFoundException) { }
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
                        if (type2path.TryGetValue(type, out var path)) return definition = new StreamWriter(path, true);
                        var escaped = transpiler.EscapeType(type);
                        if (escaped.Length > 240) escaped = escaped.Substring(0, 240);
                        var name = $"{escaped}.cc";
                        for (var i = 0; !names.Add(name); ++i) name = $"{escaped}__{i}.cc";
                        path = Path.Combine(options.Out, name);
                        type2path.Add(type, path);
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
            foreach (var path in type2path.Values) File.AppendAllText(path, "\n}\n");
            void copy(string path)
            {
                var destination = Path.Combine(options.Out, path);
                Directory.CreateDirectory(destination);
                var source = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                foreach (var x in Directory.EnumerateDirectories(source)) copy(Path.Combine(path, Path.GetFileName(x)));
                foreach (var x in Directory.EnumerateFiles(source)) File.Copy(x, Path.Combine(destination, Path.GetFileName(x)));
            }
            copy("src");
            copy("wasm");
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
target_precompile_headers({name} PRIVATE declarations.h)
if(EMSCRIPTEN)
{'\t'}set_target_properties({name} PROPERTIES OUTPUT_NAME dotnet)
{'\t'}target_sources({name} PRIVATE wasm/driver.cc wasm/pinvoke.cc)
{'\t'}target_include_directories({name} PRIVATE wasm src .)
{'\t'}target_link_libraries({name} recyclone dl
{'\t'}{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/libzlib.a
{'\t'}{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/libSystem.Native.a
{'\t'}{'\t'}${{PROJECT_SOURCE_DIR}}/wasm/libSystem.IO.Compression.Native.a
{'\t'}{'\t'}#${{PROJECT_SOURCE_DIR}}/wasm/libicuuc.a
{'\t'}{'\t'}#${{PROJECT_SOURCE_DIR}}/wasm/libicui18n.a
{'\t'}{'\t'}#${{PROJECT_SOURCE_DIR}}/wasm/libSystem.Globalization.Native.a
{'\t'}{'\t'}""-s FORCE_FILESYSTEM;-s EXPORTED_RUNTIME_METHODS=\""['cwrap', 'setValue', 'UTF8ArrayToString']\"";-s EXPORTED_FUNCTIONS=\""['_free', '_malloc']\"";-s STRICT_JS;--extern-pre-js ${{PROJECT_SOURCE_DIR}}/wasm/runtime.iffe.js;--js-library ${{PROJECT_SOURCE_DIR}}/wasm/library-dotnet.js;--js-library ${{PROJECT_SOURCE_DIR}}/wasm/pal_random.js""
{'\t'}{'\t'})
else()
{'\t'}target_include_directories({name} PRIVATE src .)
{'\t'}target_link_libraries({name} recyclone dl)
endif()
");
            return 0;
        }, _ => 1);
    }
}
